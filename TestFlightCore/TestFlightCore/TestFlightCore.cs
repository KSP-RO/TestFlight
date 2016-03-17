using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlightCore
{
    /// <summary>
    /// This is the core PartModule of the TestFlight system, and is the module that everything else plugins into.
    /// All relevant data for working in the system, as well as all usable API methods live here.
    /// </summary>
    public class TestFlightCore : PartModuleExtended, ITestFlightCore
    {
        // New API

        // New v1.3
        [KSPField(isPersistant=true)]
        public float currentFlightData;
        [KSPField(isPersistant=true)]
        public float initialFlightData;
        [KSPField]
        public float startFlightData;

        [KSPField]
        public string configuration = "";
        [KSPField]
        public string title = "";
        [KSPField]
        public string techTransfer = "";
        [KSPField]
        public float techTransferMax = 1000;
        [KSPField]
        public float techTransferGenerationPenalty = 0.05f;
        [KSPField(isPersistant=true)]
        public float operatingTime;
        [KSPField(isPersistant=true)]
        public float lastMET;
        [KSPField(isPersistant=true)]
        public bool initialized = false;
        [KSPField]
        public float maxData = 0f;
        [KSPField]
        public float failureRateModifier = 1f;
        [KSPField]
        public int scienceDataValue = 0;
        // RnD system properties
        [KSPField]
        public float rndMaxData = 0f;
        [KSPField]
        public float rndRate = 1f;
        [KSPField]
        public float rndCost = 1f;

        private double baseFailureRate;
        // We store the base, or initial, flight data for calculation of Base Failure Rate
        // Momentary Failure Rates are calculated based on modifiers.  Those modifiers
        // are stored per SCOPE and per TRIGGER
        private List<MomentaryFailureRate> momentaryFailureRates;
        private List<MomentaryFailureModifier> momentaryFailureModifiers;
        private List<String> disabledFailures;
        private float missionStartTime;
        private bool firstStaged;

        // These were created for KCT integration but might have other uses
        private float dataRateLimiter = 1.0f;
        private float dataCap = 1.0f;

        public bool TestFlightEnabled
        {
            get
            {
                if (TestFlightManagerScenario.Instance != null)
                {
                    if (!TestFlightManagerScenario.Instance.SettingsEnabled)
                        return false;
                }
                if (string.IsNullOrEmpty(Configuration))
                    return true;
                
                string[] ops = { "=", "!=", "<", "<=", ">", ">=", "<>", "<=>" };
                bool opFound = false;
                foreach (string op in ops)
                {
                    if (Configuration.Contains(op))
                        opFound = true;
                }
                if (!opFound)
                {
                    // If this configuration defines an alias, just trim it off
                    string test = Configuration;
                    if (Configuration.Contains(":"))
                    {
                        test = test.Split(new char[1] { ':' })[0];
                    }
                    if (TestFlightUtil.GetPartName(part).ToLower() == test.ToLower())
                        return true;
                    return false;
                }
                return TestFlightUtil.EvaluateQuery(Configuration, this.part);
            }
        }
        public string Configuration
        {
            get 
            { 
                if (configuration.Equals(string.Empty))
                {
                    configuration = "kspPartName = " + TestFlightUtil.GetPartName(this.part);
                    configuration = configuration + ":" + TestFlightUtil.GetPartName(this.part);
                }

                return configuration; 
            }
            set 
            { 
                configuration = value; 
            }
        }
        public string Title
        {
            get 
            { 
                if (String.IsNullOrEmpty(title))
                    return part.partInfo.title;
                
                return title; 
            }
        }
        public bool DebugEnabled
        {
            get 
            { 
                if (TestFlightManagerScenario.Instance != null)
                    return TestFlightManagerScenario.Instance.userSettings.debugLog;
                else
                    return false;
            }
        }

        public System.Random RandomGenerator
        {
            get
            {
                return TestFlightManagerScenario.Instance.RandomGenerator;
            }
        }

        internal void Log(string message)
        {
            if (TestFlightManagerScenario.Instance == null)
                return;

            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = String.Format("TestFlightCore({0}[{1}]): {2}", TestFlightUtil.GetFullPartName(this.part), Configuration, message);
            TestFlightUtil.Log(message, debug);
        }
        private void CalculateMaximumData()
        {
            if (maxData > 0f)
                return;
            
            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part);
            if (reliabilityModules == null)
                return;
            
            if (reliabilityModules.Count < 1)
                return;
            
            foreach (ITestFlightReliability rm in reliabilityModules)
            {
                FloatCurve curve = rm.GetReliabilityCurve();
                if (curve != null)
                {
                    if (curve.maxTime > maxData)
                        maxData = curve.maxTime;
                }
            }
        }
        // Retrieves the maximum amount of data a part can gain
        public float GetMaximumData()
        {

            if (maxData > 0f)
                return maxData;

            CalculateMaximumData();
            return maxData;
        }

        // Get the base or static failure rate
        public double GetBaseFailureRate()
        {
            if (baseFailureRate != 0)
                return baseFailureRate;

            double totalBFR = 0f;
            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part);
            if (reliabilityModules == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            foreach (ITestFlightReliability rm in reliabilityModules)
            {
                totalBFR += rm.GetBaseFailureRate(initialFlightData);
            }
            totalBFR = totalBFR * failureRateModifier;
            totalBFR = Math.Max(totalBFR, TestFlightUtil.MIN_FAILURE_RATE);
            baseFailureRate = totalBFR;
            return baseFailureRate;
        }
        // Get the Reliability Curve for the part
        public FloatCurve GetBaseReliabilityCurve()
        {
            FloatCurve curve;

            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part);
            if (reliabilityModules == null)
                return null;

            foreach (ITestFlightReliability rm in reliabilityModules)
            {
                curve = rm.GetReliabilityCurve();
                if (curve != null)
                    return curve;
            }

            return null;
        }
        // Get the momentary (IE current dynamic) failure rates (Can vary per reliability/failure modules)
        // These  methods will let you get a list of all momentary rates or you can get the best (lowest chance of failure)/worst (highest chance of failure) rates
        public MomentaryFailureRate GetWorstMomentaryFailureRate()
        {
            MomentaryFailureRate worstMFR;

            worstMFR = new MomentaryFailureRate();
            worstMFR.valid = false;
            worstMFR.failureRate = TestFlightUtil.MIN_FAILURE_RATE;

            foreach (MomentaryFailureRate mfr in momentaryFailureRates)
            {
                if (mfr.failureRate > worstMFR.failureRate)
                {
                    worstMFR = mfr;
                }
            }

            return worstMFR;
        }
        public MomentaryFailureRate GetBestMomentaryFailureRate()
        {
            MomentaryFailureRate bestMFR;

            bestMFR = new MomentaryFailureRate();
            bestMFR.valid = false;
            bestMFR.failureRate = double.MaxValue;

            foreach (MomentaryFailureRate mfr in momentaryFailureRates)
            {
                if (mfr.failureRate < bestMFR.failureRate)
                {
                    bestMFR = mfr;
                }
            }

            return bestMFR;
        }
        public List<MomentaryFailureRate> GetAllMomentaryFailureRates()
        {
            return momentaryFailureRates;
        }
        public double GetMomentaryFailureRateForTrigger(String trigger)
        {
            return GetMomentaryFailureRate(trigger).failureRate;
        }
        // The momentary failure rate is tracked per named "trigger" which allows multiple Reliability or FailureTrigger modules to cooperate
        // Returns the total modified failure rate back to the caller for convenience
        // IMPORTANT: For performance reasons a module should only set its Momentary Modifier WHEN IT CHANGES.  The core will cache the value.
        // Setting the same value multiple times will only force the core to recalculate the Momentary Rate over and over
        internal MomentaryFailureModifier GetMomentaryFailureModifier(String trigger, String owner)
        {
            trigger = trigger.ToLower().Trim();
            String ownerName = owner.ToLower().Trim();

            foreach (MomentaryFailureModifier mfMod in momentaryFailureModifiers)
            {
                if (mfMod.owner == ownerName && mfMod.triggerName == trigger)
                {
                    return mfMod;
                }
            }

            return new MomentaryFailureModifier();
        }
        internal MomentaryFailureRate GetMomentaryFailureRate(String trigger)
        {
            trigger = trigger.ToLower().Trim();

            foreach (MomentaryFailureRate mfRate in momentaryFailureRates)
            {
                if (mfRate.triggerName == trigger)
                {
                    return mfRate;
                }
            }

            return new MomentaryFailureRate();
        }

        public double SetTriggerMomentaryFailureModifier(String trigger, double multiplier, PartModule owner)
        {
            // store the trigger, recalculate the final rate, and cache that as well
            trigger = trigger.ToLower().Trim();
            MomentaryFailureModifier mfm;
            String ownerName = owner.moduleName.ToLower();

            mfm = GetMomentaryFailureModifier(trigger, ownerName);
            if (mfm.valid)
            {
                // recalculate new rate and cache everything
                momentaryFailureModifiers.Remove(mfm);
                mfm.modifier = multiplier;
                momentaryFailureModifiers.Add(mfm);
                return CalculateMomentaryFailureRate(trigger);
            }
            else
            {
                // If didn't find a proper match in our existing list, add a new one
                mfm.valid = true;
                mfm.owner = ownerName;
                mfm.modifier = multiplier;
                mfm.triggerName = trigger;
                momentaryFailureModifiers.Add(mfm);
                // recalculate new rate
                return CalculateMomentaryFailureRate(trigger);
            }
        }
        internal double CalculateMomentaryFailureRate(String trigger)
        {
            trigger = trigger.ToLower().Trim();
            double baseFailureRate = GetBaseFailureRate();
            double totalModifiers = 1;

            foreach (MomentaryFailureModifier mfm in momentaryFailureModifiers)
            {
                if (mfm.triggerName == trigger)
                {
                    totalModifiers *= mfm.modifier;
                }
            }
            double momentaryRate = baseFailureRate * totalModifiers;
            // Cache this value internally
            MomentaryFailureRate mfr = GetMomentaryFailureRate(trigger);
            if (mfr.valid)
            {
                momentaryFailureRates.Remove(mfr);
                mfr.failureRate = momentaryRate;
                momentaryFailureRates.Add(mfr);
            }
            else
            {
                mfr.valid = true;
                mfr.triggerName = trigger;
                mfr.failureRate = momentaryRate;
                momentaryFailureRates.Add(mfr);
            }
            return momentaryRate;
        }
        // simply converts the failure rate into a MTBF string.  Convenience method
        // Returned string will be of the format "123 units"
        // units should be one of:
        //  seconds, hours, days, years, 
        public String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units)
        {
            return FailureRateToMTBFString(failureRate, units, false, int.MaxValue);
        }
        public String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, int maximum)
        {
            return FailureRateToMTBFString(failureRate, units, false, maximum);
        }
        // Short version of MTBFString uses a single letter to denote (s)econds, (m)inutes, (h)ours, (d)ays, (y)ears
        // The returned string will be of the format "12.00s" or "0.20d"
        public String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm)
        {
            return FailureRateToMTBFString(failureRate, units, shortForm, int.MaxValue);
        }
        public String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm, int maximum)
        {
            int currentUnit = (int)TestFlightUtil.MTBFUnits.SECONDS;
            double mtbf = FailureRateToMTBF(failureRate, (TestFlightUtil.MTBFUnits)currentUnit);
            while (mtbf > maximum)
            {
                currentUnit++;
                mtbf = FailureRateToMTBF(failureRate, (TestFlightUtil.MTBFUnits)currentUnit);
                if ((TestFlightUtil.MTBFUnits)currentUnit == TestFlightUtil.MTBFUnits.INVALID)
                    break;
            }

            if (shortForm)
            {
                return String.Format("{0:F2}{1}", mtbf, UnitStringForMTBFUnit((TestFlightUtil.MTBFUnits)currentUnit)[0]);
            }
            else
            {
                return String.Format("{0:F2} {1}", mtbf, UnitStringForMTBFUnit((TestFlightUtil.MTBFUnits)currentUnit));
            }
        }
        internal String UnitStringForMTBFUnit(TestFlightUtil.MTBFUnits units)
        {
            switch (units)
            {
                case TestFlightUtil.MTBFUnits.SECONDS:
                    return "seconds";
                case TestFlightUtil.MTBFUnits.MINUTES:
                    return "minutes";
                case TestFlightUtil.MTBFUnits.HOURS:
                    return "hours";
                case TestFlightUtil.MTBFUnits.DAYS:
                    return "days";
                case TestFlightUtil.MTBFUnits.YEARS:
                    return "years";
                default:
                    return "invalid";
            }
        }
        // Simply converts the failure rate to a MTBF number, without any string formatting
        public double FailureRateToMTBF(double failureRate, TestFlightUtil.MTBFUnits units)
        {
            failureRate = Math.Max(failureRate, TestFlightUtil.MIN_FAILURE_RATE);
            double mtbfSeconds = 1.0f / failureRate;

            switch (units)
            {
                case TestFlightUtil.MTBFUnits.SECONDS:
                    return mtbfSeconds;
                case TestFlightUtil.MTBFUnits.MINUTES:
                    return mtbfSeconds / 60;
                case TestFlightUtil.MTBFUnits.HOURS:
                    return mtbfSeconds / 60 / 60;
                case TestFlightUtil.MTBFUnits.DAYS:
                    return mtbfSeconds / 60 / 60 / 24;
                case TestFlightUtil.MTBFUnits.YEARS:
                    return mtbfSeconds / 60 / 60 / 24 / 365;
                default:
                    return mtbfSeconds;
            }
        }
        // Get the FlightData or FlightTime for the part

        // New v1.3 noscope
        public float GetFlightData()
        {
            if (currentFlightData > maxData)
                currentFlightData = maxData;
            
            return currentFlightData;
        }
        public float GetInitialFlightData()
        {
            if (initialFlightData > maxData)
                initialFlightData = maxData;
            
            return initialFlightData;
        }
        public float GetFlightTime()
        {
            if (TestFlightManagerScenario.Instance != null)
            {
                TestFlightPartData partData = TestFlightManagerScenario.Instance.GetPartDataForPart(TestFlightUtil.GetFullPartName(this.part));
                return partData.GetFloat("flightTime");
            }
            else
                return 0f;
        }
        public float GetMaximumFlightData()
        {
            return maxData;
        }

        // Methods to restrict the amount of data accumulated.  Useful for KCT or other "Simulation" mods to use
        public float SetDataRateLimit(float limit)
        {
            float oldRate = dataRateLimiter;
            dataRateLimiter = limit;
            return oldRate;
        }
        public float SetDataCap(float cap)
        {
            float oldCap = dataCap;
            dataCap = cap;
            return oldCap;
        }
        // Set the FlightData for FlightTime or the part - this is an absolute set and replaces the previous FlightData.
        // This will NOT apply any global TestFlight modifiers!
        // Be sure these are the methods you want to use.  99% of the time you want to use ModifyFlightData instead
        public void SetFlightData(float data)
        {
            if (data > maxData)
                data = maxData;
            
            currentFlightData = data;
        }
        public void SetFlightTime(float flightTime)
        {
            if (TestFlightManagerScenario.Instance != null)
            {
                TestFlightManagerScenario.Instance.GetPartDataForPart(TestFlightUtil.GetFullPartName(this.part)).SetValue("flightTime", flightTime);
            }
        }

        // v1.3 noscope
        public float ModifyFlightData(float modifier)
        {
            return ModifyFlightData(modifier, false);
        }

        public float ModifyFlightData(float modifier, bool additive)
        {
            // Will hold the new flight data after calculation
            float newFlightData;
            // The flight data as it stands before modification
            float existingData = currentFlightData;
            // Amount of data stored in the scenario store
            float existingStoredFlightData = TestFlightManagerScenario.Instance.GetFlightDataForPartName(TestFlightUtil.GetFullPartName(this.part));

            // Calculate the new flight data
            if (additive)
            {
                modifier = ApplyFlightDataMultiplier(modifier);
                newFlightData = existingData + modifier;
            }
            else
            {
                newFlightData = existingData * modifier;
            }
            // Adjust new flight data if neccesary to stay under the cap
            if (newFlightData > (maxData * dataCap))
                newFlightData = maxData * dataCap;
            if (newFlightData > maxData)
                newFlightData = maxData;
            // update the scenario store to add (or subtract) the difference between the flight data before calculation and the flight data after (IE the relative change)
            TestFlightManagerScenario.Instance.SetFlightDataForPartName(TestFlightUtil.GetFullPartName(this.part), existingStoredFlightData + (newFlightData - existingData));
            // and update our part's saved data on the vessel
            currentFlightData = newFlightData;

            return currentFlightData;
        }

        public float ModifyFlightTime(float flightTime)
        {
            return ModifyFlightTime(flightTime, false);
        }

        public float ModifyFlightTime(float flightTime, bool additive)
        {
            float newFlightTime = -1f;
            if (TestFlightManagerScenario.Instance != null)
            {
                newFlightTime = TestFlightManagerScenario.Instance.GetPartDataForPart(TestFlightUtil.GetFullPartName(this.part)).GetFloat("flightTime");
                if (additive)
                    newFlightTime += flightTime;
                else
                    newFlightTime *= flightTime;
                TestFlightManagerScenario.Instance.GetPartDataForPart(TestFlightUtil.GetFullPartName(this.part)).SetValue("flightTime", newFlightTime);
            }

            return newFlightTime;
        }

        public float GetEngineerDataBonus(float partEngineerBonus)
        {
            if (TestFlightManagerScenario.Instance == null)
                return 1;
            float globalFlightDataEngineerMultiplier = TestFlightManagerScenario.Instance.userSettings.flightDataEngineerMultiplier;

            List<ProtoCrewMember> crew = this.part.vessel.GetVesselCrew().Where(c => c.experienceTrait.Title == "Engineer").ToList();
            float totalEngineerBonus = 0;
            foreach (ProtoCrewMember crewMember in crew)
            {
                int engineerLevel = crewMember.experienceLevel;
                totalEngineerBonus = totalEngineerBonus + (partEngineerBonus * engineerLevel * globalFlightDataEngineerMultiplier);
            }
            float engineerModifier = 1.0f + totalEngineerBonus;

            return engineerModifier;
        }
        // TODO
        // apply bodyconfig multiplier here
        internal float ApplyFlightDataMultiplier(float baseData)
        {
            baseData *= dataRateLimiter;

            if (TestFlightManagerScenario.Instance == null)
                return baseData;

            return baseData * TestFlightManagerScenario.Instance.userSettings.flightDataMultiplier;
        }
        // Cause a failure to occur, either a random failure or a specific one
        // If fallbackToRandom is true, then if the specified failure can't be found or can't be triggered, a random failure will be triggered instead
        // Returns the triggered failure module, or null if none
        public ITestFlightFailure TriggerFailure()
        {
            // We won't trigger a failure if we are already failed
            if (activeFailure != null)
                return null;

            // Failure occurs.  Determine which failure module to trigger
            int totalWeight = 0;
            int currentWeight = 0;
            int chosenWeight = 0;
            List<ITestFlightFailure> failureModules = null;

            // Get all failure modules on the part
            // Then filter only the ones that are not disabled
            List<ITestFlightFailure> allFailureModules = TestFlightUtil.GetFailureModules(this.part);
            foreach (ITestFlightFailure fm in allFailureModules)
            {
                PartModule pm = fm as PartModule;
                if (!disabledFailures.Contains(pm.moduleName.Trim().ToLowerInvariant()))
                {
                    if (failureModules == null)
                        failureModules = new List<ITestFlightFailure>();
                    failureModules.Add(fm);
                }
            }

            if (failureModules == null || failureModules.Count == 0)
                return null;
            
            foreach(ITestFlightFailure fm in failureModules)
            {
                totalWeight += fm.GetFailureDetails().weight;
            }
            chosenWeight = RandomGenerator.Next(1,totalWeight);
            foreach(ITestFlightFailure fm in failureModules)
            {
                currentWeight += fm.GetFailureDetails().weight;
                if (currentWeight >= chosenWeight)
                {
                    // Trigger this module's failure
                    PartModule pm = fm as PartModule;
                    if (pm != null)
                        return TriggerNamedFailure(pm.moduleName, false);
                }
            }

            return null;
        }
        public ITestFlightFailure TriggerNamedFailure(String failureModuleName)
        {
            return TriggerNamedFailure(failureModuleName, false);
        }
        public ITestFlightFailure TriggerNamedFailure(String failureModuleName, bool fallbackToRandom)
        {
            // We won't trigger a failure if we are already failed
            if (activeFailure != null)
                return null;

            failureModuleName = failureModuleName.ToLower().Trim();

            List<ITestFlightFailure> failureModules;

            failureModules = TestFlightUtil.GetFailureModules(this.part);

            foreach(ITestFlightFailure fm in failureModules)
            {
                PartModule pm = fm as PartModule;
                if (pm.moduleName.ToLower().Trim() == failureModuleName)
                {
                    if (fm == null && fallbackToRandom)
                        return TriggerFailure();
                    else if (fm == null & !fallbackToRandom)
                        return null;
                    else
                    {
                        Log("Triggering Failure: " + pm.moduleName);
                        if (!fm.OneShot)
                        {
                            activeFailure = fm;
                            failureAcknowledged = false;
                            operatingTime = -1;
                        }
                        fm.DoFailure();
                        return fm;
                    }
                }
            }

            if (fallbackToRandom)
                return TriggerFailure();

            return null;
        }
        // Returns a list of all available failures on the part
        public List<String> GetAvailableFailures()
        {
            List<String> failureModulesString = new List<string>();
            List<ITestFlightFailure> failureModules;
            failureModules = TestFlightUtil.GetFailureModules(this.part);

            foreach (ITestFlightFailure fm in failureModules)
            {
                PartModule pm = fm as PartModule;
                failureModulesString.Add(pm.moduleName);
            }

            return failureModulesString;
        }
        // Enable a failure so it can be triggered (this is the default state)
        public void EnableFailure(String failureModuleName)
        {
            failureModuleName = failureModuleName.ToLowerInvariant().Trim();
            disabledFailures.Remove(failureModuleName);
        }
        // Disable a failure so it can not be triggered
        public void DisableFailure(String failureModuleName)
        {
            failureModuleName = failureModuleName.ToLowerInvariant().Trim();
            if (!disabledFailures.Contains(failureModuleName))
                disabledFailures.Add(failureModuleName);
        }
        // Returns the Operational Time or the time, in MET, since the last time the part was fully functional. 
        // If a part is currently in a failure state, return will be -1 and the part should not fail again
        // This counts from mission start time until a failure, at which point it is reset to the time the
        // failure is repaired.  It is important to understand this is NOT the total flight time of the part.
        public float GetOperatingTime()
        {
            return operatingTime;
        }


        public void OnStageActivate(int stage)
        {
            GameEvents.onStageActivate.Remove(OnStageActivate);
            firstStaged = true;
            missionStartTime = (float)Planetarium.GetUniversalTime();
            Log("First stage activated");
        }
        /// <summary>
        /// Determines whether the part is considered operating or not.
        /// </summary>
        public bool IsPartOperating()
        {
            if (activeFailure != null)
                return false;

            IFlightDataRecorder dr = TestFlightUtil.GetDataRecorder(this.part);
            if (dr == null)
                return false;

            return dr.IsPartOperating();
        }

        // PARTMODULE functions
        public override void Update()
        {
            base.Update();

            if (!firstStaged)
                return;

            if (!TestFlightEnabled)
                return;

            if (HighLogic.LoadedSceneIsFlight)
            {

                if (TestFlightManagerScenario.Instance == null)
                    return;

                if (activeFailure != null)
                {
                    float repairStatus = activeFailure.GetSecondsUntilRepair();
                    if (repairStatus == 0)
                    {
                        Log("Part has been repaired");
                        activeFailure = null;
                        failureAcknowledged = false;
                        operatingTime = 0;
                    }
                }


                float currentMET = (float)Planetarium.GetUniversalTime() - (float)missionStartTime;
                Log("Operating Time: " + operatingTime);
                Log("Current MET: " + currentMET + ", Last MET: " + lastMET);
                if (operatingTime != -1 && IsPartOperating())
                {
                    Log("Adding " + (currentMET - lastMET) + " seconds to operatingTime");
                    operatingTime += currentMET - lastMET;
                }

                lastMET = currentMET;
            }
        }

        public override void Start()
        {
            if (!TestFlightEnabled)
                return;

            CalculateMaximumData();

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (this.vessel.situation == Vessel.Situations.PRELAUNCH)
                {
                    GameEvents.onStageActivate.Add(OnStageActivate);
                    firstStaged = false;
                }
                else
                {
                    firstStaged = true;
                    missionStartTime = (float)Planetarium.GetUniversalTime();
                }
            }
        }

        public override void OnAwake()
        {
            String partName;
            if (this.part != null)
                partName = this.part.name;
            else
                partName = "unknown";
            
            if (momentaryFailureRates == null)
                momentaryFailureRates = new List<MomentaryFailureRate>();

            if (momentaryFailureModifiers == null)
                momentaryFailureModifiers = new List<MomentaryFailureModifier>();

            if (disabledFailures == null)
                disabledFailures = new List<string>();
        }

        public void InitializeFlightData(float flightData)
        {
            if (initialized)
                return;
            
            if (flightData == 0f)
                flightData = AttemptTechTransfer();
            
            if (startFlightData > flightData)
            {
                TestFlightManagerScenario.Instance.AddFlightDataForPartName(TestFlightUtil.GetFullPartName(this.part), startFlightData);
                flightData = startFlightData;
            }

            currentFlightData = flightData;
            initialFlightData = flightData;

            missionStartTime = (float)Planetarium.GetUniversalTime();

            if (HighLogic.LoadedSceneIsFlight)
                initialized = true;
        }

        internal float AttemptTechTransfer()
        {
            // attempts to transfer data from a predecessor part
            // parts can be referenced either by part name, full name, or configuration name
            // multiple branches can be specified with the & character
            // multiple parts in a branch can be specified by separating them with a comma
            // for each branch the first part listed is considered the closest part, and each part after is considered to be one generation removed.  An optional generation penalty is added for each level
            //  for each branch, the flight data from each part is added together including any generation penalties, to create a total for that branch, modifed by the transfer amount for that branch
            // if multiple branches are specified, each branch is then added together
            // an optional maximum data can be enforced for each scope (global setting but applied to each scope, not total)
            // Example
            // techTransfer = rs-27a,rs-27,h1-b,h1:10&lr-89-na-7,lr-89-na-6,lr-89-na-5:25
            // defines two branches, one from the RS-27 branch and one from the LR-89 branch.  

            if (techTransfer.Trim() == "")
                return 0f;

            float dataToTransfer = 0f;
            string[] branches;
            string[] modifiers;
            int generation = 0;

            branches = techTransfer.Split(new char[1]{ '&' });

            foreach (string branch in branches)
            {
                modifiers = branch.Split(new char[1]{ ':' });
                if (modifiers.Length < 2)
                    continue;
                string[] partsInBranch = modifiers[0].Split(new char[1]{ ',' });
                float branchModifier = float.Parse(modifiers[1]);
                branchModifier /= 100f;
                foreach (string partName in partsInBranch)
                {
                    float partFlightData = TestFlightManagerScenario.Instance.GetFlightDataForPartName(partName);
                    if (partFlightData == 0f)
                        continue;
                    
                    dataToTransfer = dataToTransfer + ((partFlightData - (partFlightData * generation * techTransferGenerationPenalty)) * branchModifier);
//                    dataToTransfer.Add(scopeData.scope, ((scopeData.flightData - (scopeData.flightData * generation * techTransferGenerationPenalty))) * branchModifier);
                    generation++;
                }
            }

            return dataToTransfer;
        }


        private ITestFlightFailure activeFailure = null;
        private bool failureAcknowledged = false;

        public float GetRepairTime()
        {
            if (activeFailure == null)
                return 0;
            else
                return activeFailure.GetSecondsUntilRepair();
        }

        public float AttemptRepair()
        {
            if (activeFailure == null)
                return 0;

            if (activeFailure.CanAttemptRepair())
            {
                float repairStatus = activeFailure.AttemptRepair();
                if (repairStatus == 0)
                {
                    Log("Part has been repaired");
                    activeFailure = null;
                    failureAcknowledged = false;
                    operatingTime = 0;
                    return 0;
                }
                else
                    return repairStatus;
            }
            return -1;
        }
        public float ForceRepair()
        {
            if (activeFailure == null)
                return 0;

            float repairStatus = activeFailure.ForceRepair();
            if (repairStatus == 0)
            {
                activeFailure = null;
                failureAcknowledged = false;
                operatingTime = 0;
                return 0;
            }
            else
                return repairStatus;
        }

        public void AcknowledgeFailure()
        {
            failureAcknowledged = true;
        }

        public void HighlightPart(bool doHighlight)
        {
            Color highlightColor;
            if (activeFailure == null)
                highlightColor = XKCDColors.HighlighterGreen;
            else
            {
                if (activeFailure.GetFailureDetails().severity == "major")
                    highlightColor = XKCDColors.FireEngineRed;
                else
                    highlightColor = XKCDColors.OrangeYellow;
            }

            if (doHighlight)
            {
                this.part.SetHighlightDefault();
                this.part.SetHighlightType(Part.HighlightType.AlwaysOn);
                this.part.SetHighlight(true, false);
                this.part.SetHighlightColor(highlightColor);
                HighlightingSystem.Highlighter highlighter;
                highlighter = this.part.FindModelTransform("model").gameObject.AddComponent<HighlightingSystem.Highlighter>();
                highlighter.ConstantOn(highlightColor);
                highlighter.SeeThroughOn();
            }
            else
            {
                this.part.SetHighlightDefault();
                this.part.SetHighlightType(Part.HighlightType.AlwaysOn);
                this.part.SetHighlight(false, false);
                this.part.SetHighlightColor(XKCDColors.HighlighterGreen);
                Destroy(this.part.FindModelTransform("model").gameObject.GetComponent(typeof(HighlightingSystem.Highlighter)));
            }
        }

        public virtual int GetPartStatus()
        {
            if (activeFailure == null)
                return 0;

            if (activeFailure.GetFailureDetails().severity == "minor")
                return 1;
            if (activeFailure.GetFailureDetails().severity == "failure")
                return 1;
            if (activeFailure.GetFailureDetails().severity == "major")
                return 1;

            return -1;
        }

        public virtual ITestFlightFailure GetFailureModule()
        {
            return activeFailure;
        }

        public bool IsFailureAcknowledged()
        {
            return failureAcknowledged;
        }

        public string GetRequirementsTooltip()
        {
            if (activeFailure == null)
                return "No Repair Neccesary";

            List<RepairRequirements> requirements = activeFailure.GetRepairRequirements();

            if (requirements == null)
                return "This repair has no requirements or can not be repaired.";

            string tooltip = "";

            foreach (RepairRequirements requirement in requirements)
            {
                if (requirement.requirementMet)
                {
                    tooltip = String.Format("{0}<color=#859900ff>{1}</color>\n", tooltip, requirement.requirementMessage);
                }
                else if (!requirement.requirementMet && !requirement.optionalRequirement)
                {
                    tooltip = String.Format("{0}<color=#dc322fff>{1}</color>\n", tooltip, requirement.requirementMessage);
                }
                else if (!requirement.requirementMet && requirement.optionalRequirement)
                {
                    tooltip = String.Format("{0}(OPTIONAL +{1:f2}%) <color=#b58900ff>{2}</color>\n", tooltip, requirement.repairBonus * 100.0f, requirement.requirementMessage);
                }
            }

            return tooltip;
        }
        public string GetTestFlightInfo()
        {
            string info = "";
//            List<ITestFlightFailure> failureModules = TestFlightUtil.GetFailureModules(this.part);
//            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part);
//            IFlightDataRecorder dataRecorder = TestFlightUtil.GetDataRecorder(this.part);
//
//            foreach (ITestFlightFailure fm in failureModules)
//            {
//                info += fm.GetTestFlightInfo();
//            }
//            foreach (ITestFlightReliability rm in reliabilityModules)
//            {
//                info += rm.GetTestFlightInfo();
//            }
//            info += dataRecorder.GetTestFlightInfo();
            return info;
        }
        public float GetMaximumRnDData()
        {
            if (rndMaxData == 0)
                return GetMaximumData() * 0.75f;
            else
                return rndMaxData;
        }

        public float GetRnDCost()
        {
            return rndCost;
        }

        public float GetRnDRate()
        {
            return rndRate;
        }
    }
}


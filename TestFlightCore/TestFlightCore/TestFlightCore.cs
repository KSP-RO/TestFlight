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
        [KSPField(isPersistant = true)]
        public FlightDataConfig flightData;
        [KSPField(isPersistant = true)]
        public double deepSpaceThreshold = 10000000;
        [KSPField(isPersistant=true)]
        public string configuration = "";
        [KSPField(isPersistant=true)]
        public string techTransfer = "";
        [KSPField(isPersistant=true)]
        public float techTransferMax = 1000;
        [KSPField(isPersistant=true)]
        public float techTransferGenerationPenalty = 0.05f;




        // Base Failure Rate is stored per Scope internally
        private Dictionary<String, double> baseFailureRate;
        // We store the base, or initial, flight data for calculation of Base Failure Rate
        private FlightDataConfig baseFlightData;
        // Momentary Failure Rates are calculated based on modifiers.  Those modifiers
        // are stored per SCOPE and per TRIGGER
        private List<MomentaryFailureRate> momentaryFailureRates;
        private List<MomentaryFailureModifier> momentaryFailureModifiers;
        private List<String> disabledFailures;
        private double operatingTime;
        private double lastMET;
        private double missionStartTime;
        private bool firstStaged;

        // These were created for KCT integration but might have other uses
        private double dataRateLimiter = 1.0;
        private double dataCap = double.MaxValue;

        public bool TestFlightEnabled
        {
            get
            {
                bool enabled = true;
                // If this part has a ModuleEngineConfig then we need to verify we are assigned to the active configuration
                if (this.part.Modules.Contains("ModuleEngineConfigs"))
                {
                    string currentConfig = (string)(part.Modules["ModuleEngineConfigs"].GetType().GetField("configuration").GetValue(part.Modules["ModuleEngineConfigs"]));
                    if (currentConfig != configuration)
                        enabled = false;
                }
                return enabled;
            }
        }
        public string Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }

        // Get a proper scope string for use in other parts of the API
        public String GetScope()
        {
            return GetScopeForSituationAndBody(this.vessel.situation, this.vessel.mainBody);
        }
        public String GetScopeForSituation(String situation)
        {
            return GetScopeForSituationAndBody(situation, this.vessel.mainBody);
        }
        public String GetScopeForSituation(Vessel.Situations situation)
        {
            return GetScopeForSituationAndBody(situation.ToString(), this.vessel.mainBody);
        }
        public String GetScopeForSituationAndBody(String situation, CelestialBody body)
        {
            return GetScopeForSituationAndBody(situation, body.GetName());
        }
        public String GetScopeForSituationAndBody(Vessel.Situations situation, String body)
        {
            return GetScopeForSituationAndBody(situation.ToString(), body);
        }
        public String GetScopeForSituationAndBody(Vessel.Situations situation, CelestialBody body)
        {
            return GetScopeForSituationAndBody(situation.ToString(), body.GetName());
        }
        public String GetScopeForSituationAndBody(String situation, String body)
        {
            // Determine if we are recording data in SPACE or ATMOSHPHERE
            situation = situation.ToLower().Trim();
            body = body.ToLower().Trim();
            if (situation == "sub_orbital" || situation == "orbiting" || situation == "escaping" || situation == "docked")
            {
                if (this.vessel.altitude > deepSpaceThreshold)
                {
                    situation = "deep-space";
                    body = "none";
                }
                else
                {
                    situation = "space";
                }
            }
            else if (situation == "flying" || situation == "landed" || situation == "splashed" || situation == "prelaunch")
            {
                situation = "atmosphere";
            }
            else
            {
                situation = "default";
            }

            return String.Format("{0}_{1}", body.ToLower(), situation.ToLower());
        }
        public String PrettyStringForScope(String scope)
        {
            string body;
            string situation;
            string[] split = scope.Split(new char[] { '_' });

            // fall out if we have unexpected input
            if (split.Length != 2)
                return scope;

            body = split[0].Substring(0,1).ToUpper() + split[0].Substring(1).ToLower();
            situation = split[1].Substring(0,1).ToUpper() + split[1].Substring(1).ToLower();

            // Try to get the alias for this body if at all possible
            if (TestFlightManagerScenario.Instance != null)
            {
                if (TestFlightManagerScenario.Instance.bodySettings.bodyAliases.ContainsKey(body.ToLower()))
                    body = TestFlightManagerScenario.Instance.bodySettings.bodyAliases[body.ToLower()];
            }

            return body + " " + situation;
        }

        // Get the base or static failure rate
        public double GetBaseFailureRate()
        {
            return GetBaseFailureRateForScope(GetScope());
        }
        public double GetBaseFailureRateForScope(String scope)
        {
            // Since the Base Failure Rate does not change during the lifetime of a part
            // we cache that data internally so as to not have to call the Reliability module
            // constantly.  Therefore here we are returning what we have if we have it,
            // or else getting it from the Reliability module and caching that for next time.
            scope = scope.ToLower().Trim();
//            LogFormatted_DebugOnly(String.Format("TestFlightCore: GetBaseFailureRateForScope({0})",scope));
            if (baseFailureRate == null)
            {
//                LogFormatted_DebugOnly("BaseFailureRate data is invalid");
                return TestFlightUtil.MIN_FAILURE_RATE;
            }
            if (baseFlightData == null)
            {
//                LogFormatted_DebugOnly("baseFlightData is invalid");
                return TestFlightUtil.MIN_FAILURE_RATE;
            }
            if (baseFailureRate.ContainsKey(scope))
            {
//                LogFormatted_DebugOnly("TestFlightCore: Returning cached Base Failure Rate");
                return baseFailureRate[scope];
            }
            else
            {
//                LogFormatted_DebugOnly("TestFlightCore: Calculating Base Failure Rate from Reliability modules");
                double totalBFR = 0;
                double data = 0;
                FlightDataBody body = baseFlightData.GetFlightData(scope);
                if (body != null)
                    data = body.flightData;

                List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part);
                if (reliabilityModules == null)
                    return TestFlightUtil.MIN_FAILURE_RATE;

                foreach (ITestFlightReliability rm in reliabilityModules)
                {
                    totalBFR += rm.GetBaseFailureRateForScope(data, scope);
                }
                totalBFR = Mathf.Max((float)totalBFR, (float)TestFlightUtil.MIN_FAILURE_RATE);
                baseFailureRate.Add(scope, totalBFR);
                return totalBFR;
            }

        }
        // Get the Reliability Curve for the part
        public FloatCurve GetBaseReliabilityCurve()
        {
            return GetBaseReliabilityCurveForScope(GetScope());
        }
        public FloatCurve GetBaseReliabilityCurveForScope(String scope)
        {
            scope = scope.ToLower().Trim();
            FloatCurve curve;

            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part);
            if (reliabilityModules == null)
                return null;

            foreach (ITestFlightReliability rm in reliabilityModules)
            {
                curve = rm.GetReliabilityCurveForScope(scope);
                if (curve != null)
                    return curve;
            }

            return null;
        }
        // Get the momentary (IE current dynamic) failure rates (Can vary per reliability/failure modules)
        // These  methods will let you get a list of all momentary rates or you can get the best (lowest chance of failure)/worst (highest chance of failure) rates
        public MomentaryFailureRate GetWorstMomentaryFailureRate()
        {
            return GetWorstMomentaryFailureRateForScope(GetScope());
        }
        public MomentaryFailureRate GetBestMomentaryFailureRate()
        {
            return GetBestMomentaryFailureRateForScope(GetScope());
        }
        public List<MomentaryFailureRate> GetAllMomentaryFailureRates()
        {
            return GetAllMomentaryFailureRatesForScope(GetScope());
        }
        public MomentaryFailureRate GetWorstMomentaryFailureRateForScope(String scope)
        {
            scope = scope.ToLower().Trim();
            MomentaryFailureRate worstMFR;

            worstMFR = new MomentaryFailureRate();
            worstMFR.valid = false;
            worstMFR.failureRate = TestFlightUtil.MIN_FAILURE_RATE;

            foreach (MomentaryFailureRate mfr in momentaryFailureRates)
            {
                if (mfr.scope == scope && mfr.failureRate > worstMFR.failureRate)
                {
                    worstMFR = mfr;
                }
            }

            return worstMFR;
        }
        public MomentaryFailureRate GetBestMomentaryFailureRateForScope(String scope)
        {
            scope = scope.ToLower().Trim();
            MomentaryFailureRate bestMFR;

            bestMFR = new MomentaryFailureRate();
            bestMFR.valid = false;
            bestMFR.failureRate = Double.MaxValue;

            foreach (MomentaryFailureRate mfr in momentaryFailureRates)
            {
                if (mfr.scope == scope && mfr.failureRate < bestMFR.failureRate)
                {
                    bestMFR = mfr;
                }
            }

            return bestMFR;
        }
        public List<MomentaryFailureRate> GetAllMomentaryFailureRatesForScope(String scope)
        {
            scope = scope.ToLower().Trim();

            List<MomentaryFailureRate> mfrList = new List<MomentaryFailureRate>();

            foreach (MomentaryFailureRate mfr in momentaryFailureRates)
            {
                if (mfr.scope == scope)
                {
                    mfrList.Add(mfr);
                }
            }

            return mfrList;
        }
        public double GetMomentaryFailureRateForTrigger(String trigger)
        {
            return GetMomentaryFailureRateForTriggerForScope(trigger, GetScope());
        }
        public double GetMomentaryFailureRateForTriggerForScope(String trigger, String scope)
        {
            return GetMomentaryFailureRate(trigger, scope).failureRate;
        }
        // The momentary failure rate is tracked per named "trigger" which allows multiple Reliability or FailureTrigger modules to cooperate
        // Returns the total modified failure rate back to the caller for convenience
        // IMPORTANT: For performance reasons a module should only set its Momentary Modifier WHEN IT CHANGES.  The core will cache the value.
        // Setting the same value multiple times will only force the core to recalculate the Momentary Rate over and over
        internal MomentaryFailureModifier GetMomentaryFailureModifier(String trigger, String owner, String scope)
        {
            scope = scope.ToLower().Trim();
            trigger = trigger.ToLower().Trim();
            String ownerName = owner.ToLower().Trim();

            foreach (MomentaryFailureModifier mfMod in momentaryFailureModifiers)
            {
                if (mfMod.scope == scope && mfMod.owner == ownerName && mfMod.triggerName == trigger)
                {
                    return mfMod;
                }
            }

            return new MomentaryFailureModifier();
        }
        internal MomentaryFailureRate GetMomentaryFailureRate(String trigger, String scope)
        {
            scope = scope.ToLower().Trim();
            trigger = trigger.ToLower().Trim();

            foreach (MomentaryFailureRate mfRate in momentaryFailureRates)
            {
                if (mfRate.scope == scope && mfRate.triggerName == trigger)
                {
                    return mfRate;
                }
            }

            return new MomentaryFailureRate();
        }

        public double SetTriggerMomentaryFailureModifier(String trigger, double multiplier, PartModule owner)
        {
            return SetTriggerMomentaryFailureModifierForScope(trigger, multiplier, owner, GetScope());
        }
        public double SetTriggerMomentaryFailureModifierForScope(String trigger, double multiplier, PartModule owner, String scope)
        {
            // store the trigger, recalculate the final rate, and cache that as well
            scope = scope.ToLower().Trim();
            trigger = trigger.ToLower().Trim();
            MomentaryFailureModifier mfm;
            String ownerName = owner.moduleName.ToLower();

            mfm = GetMomentaryFailureModifier(trigger, ownerName, scope);
            if (mfm.valid)
            {
                // recalculate new rate and cache everything
                momentaryFailureModifiers.Remove(mfm);
                mfm.modifier = multiplier;
                momentaryFailureModifiers.Add(mfm);
                return CalculateMomentaryFailureRate(trigger, scope);
            }
            else
            {
                // If didn't find a proper match in our existing list, add a new one
                mfm.valid = true;
                mfm.scope = scope;
                mfm.owner = ownerName;
                mfm.modifier = multiplier;
                mfm.triggerName = trigger;
                momentaryFailureModifiers.Add(mfm);
                // recalculate new rate
                return CalculateMomentaryFailureRate(trigger, scope);
            }
        }
        internal double CalculateMomentaryFailureRate(String trigger, String scope)
        {
            scope = scope.ToLower().Trim();
            trigger = trigger.ToLower().Trim();
            double baseFailureRate = GetBaseFailureRateForScope(scope);
            double totalModifiers = 1;

            foreach (MomentaryFailureModifier mfm in momentaryFailureModifiers)
            {
                if (mfm.scope == scope && mfm.triggerName == trigger)
                {
                    totalModifiers *= mfm.modifier;
                }
            }
            double momentaryRate = baseFailureRate * totalModifiers;
            // Cache this value internally
            MomentaryFailureRate mfr = GetMomentaryFailureRate(trigger, scope);
            if (mfr.valid)
            {
                momentaryFailureRates.Remove(mfr);
                mfr.failureRate = momentaryRate;
                momentaryFailureRates.Add(mfr);
            }
            else
            {
                mfr.valid = true;
                mfr.scope = scope;
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
            failureRate = Mathf.Max((float)failureRate, (float)TestFlightUtil.MIN_FAILURE_RATE);
            double mtbfSeconds = 1.0 / failureRate;

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
        public double GetFlightData()
        {
            return GetFlightDataForScope(GetScope());
        }
        public double GetFlightDataForScope(String scope)
        {
            if (flightData == null)
            {
                LogFormatted_DebugOnly("FlightData is invalid");
                return 0;
            }
            FlightDataBody dataBody = flightData.GetFlightData(scope);
            if (dataBody == null)
            {
                return 0;
            }
            else
                return dataBody.flightData;
        }
        public double GetInitialFlightData()
        {
            return GetInitialFlightDataforScope(GetScope());
        }
        public double GetInitialFlightDataforScope(String scope)
        {
            if (baseFlightData == null)
                return 0;

            FlightDataBody dataBody = baseFlightData.GetFlightData(scope);
            if (dataBody == null)
                return 0;
            else
                return dataBody.flightData;
        }
        public double GetFlightTime()
        {
            return GetFlightTimeForScope(GetScope());
        }
        public double GetFlightTimeForScope(String scope)
        {
            FlightDataBody dataBody = flightData.GetFlightData(scope);
            if (dataBody == null)
            {
                return 0;
            }
            else
            {
                return dataBody.flightTime;
            }
        }
        // Methods to restrict the amount of data accumulated.  Useful for KCT or other "Simulation" mods to use
        public double SetDataRateLimit(double limit)
        {
            double oldRate = dataRateLimiter;
            dataRateLimiter = limit;
            return oldRate;
        }
        public double SetDataCap(double cap)
        {
            double oldCap = dataCap;
            dataCap = cap;
            return oldCap;
        }
        // Set the FlightData for FlightTime or the part - this is an absolute set and replaces the previous FlightData.
        // This will NOT apply any global TestFlight modifiers!
        // Be sure these are the methods you want to use.  99% of the time you want to use ModifyFlightData instead
        public void SetFlightData(double data)
        {
            SetFlightDataForScope(data, GetScope());
        }
        public void SetFlightTime(double seconds)
        {
            SetFlightTimeForScope(seconds, GetScope());
        }
        // TODO
        public void SetFlightDataForScope(double data, String scope)
        {
        }
        // TODO
        public void SetFlightTimeForScope(double seconds, String scope)
        {
        }
        // Modify the FlightData or FlightTime for the part
        // The given modifier is multiplied against the current FlightData unless additive is true
        // Global TestFlight modifiers are applied here
        public double ModifyFlightData(double modifier)
        {
            return ModifyFlightDataForScope(modifier, GetScope(), false);
        }
        public double ModifyFlightTime(double modifier)
        {
            return ModifyFlightTimeForScope(modifier, GetScope(), false);
        }
        public double ModifyFlightData(double modifier, bool additive)
        {
            return ModifyFlightDataForScope(modifier, GetScope(), additive);
        }
        public double ModifyFlightTime(double modifier, bool additive)
        {
            return ModifyFlightTimeForScope(modifier, GetScope(), additive);
        }
        public double ModifyFlightDataForScope(double modifier, String scope)
        {
            return ModifyFlightDataForScope(modifier, scope, false);
        }
        public double ModifyFlightTimeForScope(double modifier, String scope)
        {
            return ModifyFlightTimeForScope(modifier, scope, false);
        }
        public double ModifyFlightDataForScope(double modifier, String scope, bool additive)
        {
            if (flightData == null)
            {
                return 0;
            }

            FlightDataBody bodyData = flightData.GetFlightData(scope);
            if (bodyData == null)
            {
                if (!additive)
                    return 0;
                modifier = ApplyFlightDataMultiplier(modifier);

                if (modifier >= dataCap)
                    modifier = dataCap;

                flightData.AddFlightData(scope, modifier, 0);
                return modifier;
            }

            if (additive)
            {
                modifier = ApplyFlightDataMultiplier(modifier);
                bodyData.flightData += modifier;
            }
            else
            {
                bodyData.flightData *= modifier;
            }

            if (bodyData.flightData >= dataCap)
                bodyData.flightData = dataCap;

            flightData.AddFlightData(scope, bodyData.flightData, bodyData.flightTime);
            return bodyData.flightData;
        }
        public double GetEngineerDataBonus(double partEngineerBonus)
        {
            if (TestFlightManagerScenario.Instance == null)
                return 1;
            double globalFlightDataEngineerMultiplier = TestFlightManagerScenario.Instance.userSettings.flightDataEngineerMultiplier;

            List<ProtoCrewMember> crew = this.part.vessel.GetVesselCrew().Where(c => c.experienceTrait.Title == "Engineer").ToList();
            double totalEngineerBonus = 0;
            foreach (ProtoCrewMember crewMember in crew)
            {
                int engineerLevel = crewMember.experienceLevel;
                totalEngineerBonus = totalEngineerBonus + (partEngineerBonus * engineerLevel * globalFlightDataEngineerMultiplier);
            }
            double engineerModifier = 1.0 + totalEngineerBonus;

            return engineerModifier;
        }
        // TODO
        // apply bodyconfig multiplier here
        internal double ApplyFlightDataMultiplier(double baseData)
        {
            baseData *= dataRateLimiter;

            if (TestFlightManagerScenario.Instance == null)
                return baseData;

            return baseData * TestFlightManagerScenario.Instance.userSettings.flightDataMultiplier;
        }
        // TODO
        public double ModifyFlightTimeForScope(double modifier, String scope, bool additive)
        {
            return 0;
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
            List<ITestFlightFailure> failureModules;

            failureModules = TestFlightUtil.GetFailureModules(this.part);

            foreach(ITestFlightFailure fm in failureModules)
            {
                totalWeight += fm.GetFailureDetails().weight;
            }
            chosenWeight = UnityEngine.Random.Range(1,totalWeight);
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
                        LogFormatted_DebugOnly("TestFlightCore: Triggering Failure: " + pm.moduleName);
                        activeFailure = fm;
                        failureAcknowledged = false;
                        fm.DoFailure();
                        operatingTime = -1;
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
            failureModuleName = failureModuleName.ToLower().Trim();
            disabledFailures.Remove(failureModuleName);
        }
        // Disable a failure so it can not be triggered
        public void DisableFailure(String failureModuleName)
        {
            failureModuleName = failureModuleName.ToLower().Trim();
            if (!disabledFailures.Contains(failureModuleName))
                disabledFailures.Add(failureModuleName);
        }
        // Returns the Operational Time or the time, in MET, since the last time the part was fully functional. 
        // If a part is currently in a failure state, return will be -1 and the part should not fail again
        // This counts from mission start time until a failure, at which point it is reset to the time the
        // failure is repaired.  It is important to understand this is NOT the total flight time of the part.
        public double GetOperatingTime()
        {
            return operatingTime;
        }


        public void OnStageActivate(int stage)
        {
            GameEvents.onStageActivate.Remove(OnStageActivate);
            firstStaged = true;
            missionStartTime = Planetarium.GetUniversalTime();
            LogFormatted_DebugOnly("TestFlightCore: First stage activated");
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

                if (operatingTime < 0)
                    return;

                double currentMET = Planetarium.GetUniversalTime() - missionStartTime;

                operatingTime += currentMET - lastMET;

                lastMET = currentMET;
            }
        }

        public override void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (this.vessel.situation == Vessel.Situations.PRELAUNCH)
                {
                    GameEvents.onStageActivate.Add(OnStageActivate);
                    firstStaged = false;
                }
                else
                    firstStaged = true;
                operatingTime = 0;
                lastMET = 0;
            }
        }

        public override void OnAwake()
        {
            String partName;
            if (this.part != null)
                partName = this.part.name;
            else
                partName = "unknown";
            LogFormatted_DebugOnly("TestFlightCore: OnAwake(" + partName + ")");
            if (baseFlightData == null)
                baseFlightData = new FlightDataConfig();
            if (flightData == null)
                flightData = new FlightDataConfig();

            if (baseFailureRate == null)
                baseFailureRate = new Dictionary<string, double>();

            if (momentaryFailureRates == null)
                momentaryFailureRates = new List<MomentaryFailureRate>();

            if (momentaryFailureModifiers == null)
                momentaryFailureModifiers = new List<MomentaryFailureModifier>();

            if (disabledFailures == null)
                disabledFailures = new List<string>();

            operatingTime = 0;
            LogFormatted_DebugOnly("TestFlightCore: OnWake(" + partName + "):DONE");
        }

        public void InitializeFlightData(List<TestFlightData> allFlightData)
        {
            if (allFlightData == null)
                allFlightData = AttemptTechTransfer();
            if (allFlightData == null)
                return;
            baseFlightData = new FlightDataConfig();
            flightData = new FlightDataConfig();
            foreach (TestFlightData data in allFlightData)
            {
                baseFlightData.AddFlightData(data.scope, data.flightData, data.flightTime);
                flightData.AddFlightData(data.scope, data.flightData, data.flightTime);
            }
            missionStartTime = 0;
            return;
        }


        internal List<TestFlightData> AttemptTechTransfer()
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
                return null;

            List<TestFlightData> transferredFlightData;
            Dictionary<string, double> dataToTransfer = null;
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
                dataToTransfer = new Dictionary<string, double>();
                foreach (string partNameFragment in partsInBranch)
                {
                    PartFlightData partData = TestFlightManagerScenario.Instance.GetFlightDataForPartNameFragment(partNameFragment);
                    if (partData == null)
                        continue;
                    List<TestFlightData> data = partData.GetFlightData();
                    foreach (TestFlightData scopeData in data)
                    {
                        if (dataToTransfer.ContainsKey(scopeData.scope))
                        {
                            dataToTransfer[scopeData.scope] = dataToTransfer[scopeData.scope] + ((scopeData.flightData - (scopeData.flightData * generation * techTransferGenerationPenalty)) * branchModifier);
                        }
                        else
                        {
                            dataToTransfer.Add(scopeData.scope, ((scopeData.flightData - (scopeData.flightData * generation * techTransferGenerationPenalty))) * branchModifier);
                        }
                    }
                    generation++;
                }
            }
            // When that is all done we should have a bunch of data in our dictionary dataToTransfer sorted by scope.  Now we just need to pack it into proper TestFlightData structs
            if (dataToTransfer == null || dataToTransfer.Count <= 0)
                return null;
            transferredFlightData = new List<TestFlightData>();
            foreach (var scope in dataToTransfer)
            {
                TestFlightData data = new TestFlightData();
                data.scope = scope.Key;
                if (techTransferMax > 0 && scope.Value > techTransferMax)
                    data.flightData = techTransferMax;
                else
                    data.flightData = scope.Value;
                transferredFlightData.Add(data);
            }
            return transferredFlightData;
        }


        private ITestFlightFailure activeFailure = null;
        private bool failureAcknowledged = false;

        public double GetRepairTime()
        {
            if (activeFailure == null)
                return 0;
            else
                return activeFailure.GetSecondsUntilRepair();
        }

        public double AttemptRepair()
        {
            if (activeFailure == null)
                return 0;

            if (activeFailure.CanAttemptRepair())
            {
                double repairStatus = activeFailure.AttemptRepair();
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
            return -1;
        }
        public double ForceRepair()
        {
            if (activeFailure == null)
                return 0;

            double repairStatus = activeFailure.ForceRepair();
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

    }
}


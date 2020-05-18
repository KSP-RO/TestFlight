using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using TestFlightCore.KSPPluginFramework;
using TestFlightAPI;

namespace TestFlightCore
{
    /// <summary>
    /// This is the core PartModule of the TestFlight system, and is the module that everything else plugins into.
    /// All relevant data for working in the system, as well as all usable API methods live here.
    /// </summary>
    public class TestFlightCore : PartModuleExtended, ITestFlightCore
    {
        [KSPField]
        public int configVersion = 1;
        
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

        #region Persistant KSP Fields

        [KSPField(isPersistant=true)]
        public float operatingTime;
        [KSPField(isPersistant=true)]
        public double lastMET;
        [KSPField(isPersistant=true)]
        public float currentFlightData;
        [KSPField(isPersistant=true)]
        public float initialFlightData;
        [KSPField(isPersistant=true)]
        public double missionStartTime;
        [KSPField(isPersistant=true)]
        public string activeConfig;

        #endregion

        public List<ConfigNode> configs;
        public ConfigNode currentConfig;
        public string configNodeData;

        bool initialized = false;
        float transferData;
        float researchData;

        private double baseFailureRate;
        // We store the base, or initial, flight data for calculation of Base Failure Rate
        // Momentary Failure Rates are calculated based on modifiers.  Those modifiers
        // are stored per SCOPE and per TRIGGER
        private List<MomentaryFailureRate> momentaryFailureRates;
        private List<MomentaryFailureModifier> momentaryFailureModifiers;
        private List<String> disabledFailures;
        private bool firstStaged;

        // These were created for KCT integration but might have other uses
        private float dataRateLimiter = 1.0f;
        private float dataCap = 1.0f;

        private List<ProtoCrewMember> crew;

        // A part can now have multiple failures, so we simply maintain a list of them
        private List<ITestFlightFailure> failures = null;
        private bool hasMajorFailure = false;

        private bool active;

        private string[] ops = { "=", "!=", "<", "<=", ">", ">=", "<>", "<=>" };

        IFlightDataRecorder m_Recorder;

        public bool ActiveConfiguration
        {
            get
            {
                if (TestFlightManagerScenario.Instance != null)
                {
                    if (!TestFlightManagerScenario.Instance.SettingsEnabled)
                        return false;
                }

                SetActiveConfigFromInterop();
                return true;
            }
        }

        public bool TestFlightEnabled
        {
            get
            {
                if (TestFlightManagerScenario.Instance != null)
                {
                    if (!TestFlightManagerScenario.Instance.SettingsEnabled)
                        return false;
                }
                return active;
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
        public string Alias
        {
            get
            {
                return Configuration.Contains(":") ? Configuration.Split(new char[1] {
                    ':'
                })[1] : "";
            }
        }
        public string Title
        {
            get 
            { 
                return String.IsNullOrEmpty(title) ? part.partInfo.title : title;
            }
        }
        public bool DebugEnabled
        {
            get 
            { 
                return TestFlightManagerScenario.Instance != null ? TestFlightManagerScenario.Instance.userSettings.debugLog : false;
            }
        }

        void SetActiveConfigFromInterop()
        {
            if (configs == null)
                configs = new List<ConfigNode>();
            
            foreach (var configNode in configs)
            {
                if (!configNode.HasValue("configuration")) continue;

                var nodeConfiguration = configNode.GetValue("configuration");
                if (string.IsNullOrEmpty(nodeConfiguration))
                {
                    currentConfig = configNode;
                    break;
                }

                bool opFound = false;
                for (int i = 0; i < 8; i++)
                {
                    opFound |= nodeConfiguration.Contains(ops[i]);
                }
                if (!opFound)
                {
                    // If this configuration defines an alias, just trim it off
                    string test = nodeConfiguration;
                    if (nodeConfiguration.Contains(":"))
                    {
                        test = test.Split(new char[1] { ':' })[0];
                    }

                    if (string.Equals(TestFlightUtil.GetPartName(part).ToLower(), test.ToLower(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        currentConfig = configNode;
                        break;
                    }
                }

                if (TestFlightUtil.EvaluateQuery(nodeConfiguration, this.part))
                {
                    currentConfig = configNode;
                    break;
                }
            }

            if (currentConfig == null) return;

            // update current values with those from the current config node
            currentConfig.TryGetValue("startFlightData", ref startFlightData);
            currentConfig.TryGetValue("configuration", ref configuration);
            currentConfig.TryGetValue("title", ref title);
            currentConfig.TryGetValue("techTransfer", ref techTransfer);
            currentConfig.TryGetValue("techTransferMax", ref techTransferMax);
            currentConfig.TryGetValue("techTransferGenerationPenalty", ref techTransferGenerationPenalty);
            currentConfig.TryGetValue("maxData", ref maxData);
            currentConfig.TryGetValue("failureRateModifier", ref failureRateModifier);
            currentConfig.TryGetValue("scienceDataValue", ref scienceDataValue);
            currentConfig.TryGetValue("rndMaxData", ref rndMaxData);
            currentConfig.TryGetValue("rndRate", ref rndRate);
            currentConfig.TryGetValue("rndCost", ref rndCost);
        }

        [KSPEvent(guiActiveEditor=false, guiName = "R&D Window")]            
        public void ToggleRNDGUI()
        {
            TestFlightEditorWindow.Instance.LockPart(this.part, Alias);
            TestFlightEditorWindow.Instance.ToggleWindow();
        }

        public System.Random RandomGenerator
        {
            get
            {
                return TestFlightManagerScenario.RandomGenerator;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (node.HasNode("MODULE"))
                node = node.GetNode("MODULE");

            if (configs == null)
                configs = new List<ConfigNode>();

            ConfigNode[] cNodes = node.GetNodes("CONFIG");
            if (cNodes != null && cNodes.Length > 0)
            {
                configs.Clear();

                foreach (ConfigNode subNode in cNodes) {
                    var newNode = new ConfigNode("CONFIG");
                    subNode.CopyTo(newNode);
                    configs.Add(newNode);
                }
            }

            configNodeData = node.ToString();
        }

        internal void Log(string message)
        {
            return;
//
//            if (TestFlightManagerScenario.Instance == null)
//                return;
//
//            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
//            message = String.Format("TestFlightCore({0}[{1}]): {2}", Alias, Configuration, message);
//            TestFlightUtil.Log(message, debug);
        }
        private void CalculateMaximumData()
        {
            if (maxData > 0f)
                return;
            
            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, Alias);
            if (reliabilityModules == null)
                return;
            
            if (reliabilityModules.Count < 1)
                return;
            
            for (int i = 0, reliabilityModulesCount = reliabilityModules.Count; i < reliabilityModulesCount; i++)
            {
                ITestFlightReliability rm = reliabilityModules[i];
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
            if (baseFailureRate > 0)
            {
                Log("Returning cached failure rate");
                return baseFailureRate;
            }

            double totalBFR = 0f;
            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, Alias);
            if (reliabilityModules == null)
            {
                Log("Unable to locate any reliability modules.  Using min failure rate");
                return TestFlightUtil.MIN_FAILURE_RATE;
            }

            for (int i = 0, reliabilityModulesCount = reliabilityModules.Count; i < reliabilityModulesCount; i++)
            {
                ITestFlightReliability rm = reliabilityModules[i];
                totalBFR += rm.GetBaseFailureRate(initialFlightData);
            }
            Log(String.Format("BFR: {0:F7}, Modifier: {1:F7}", totalBFR, failureRateModifier));
            totalBFR = totalBFR * failureRateModifier;
            totalBFR = Math.Max(totalBFR, TestFlightUtil.MIN_FAILURE_RATE);
            baseFailureRate = totalBFR;
            return baseFailureRate;
        }
        // Get the Reliability Curve for the part
        public FloatCurve GetBaseReliabilityCurve()
        {
            FloatCurve curve;

            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, Alias);
            if (reliabilityModules == null)
                return null;

            for (int i = 0, reliabilityModulesCount = reliabilityModules.Count; i < reliabilityModulesCount; i++)
            {
                ITestFlightReliability rm = reliabilityModules[i];
                curve = rm.GetReliabilityCurve();
                if (curve == null)
                    continue;
                return curve;
            }

            return null;
        }
        public float GetBurnTime()
        {
            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, Alias);
            if (reliabilityModules == null)
                return 0f;

            float burnTime = 0f;

            for (int i = 0, reliabilityModulesCount = reliabilityModules.Count; i < reliabilityModulesCount; i++)
            {
                ITestFlightReliability rm = reliabilityModules[i];
                burnTime = Mathf.Max(rm.GetCurrentBurnTime(), burnTime);
            }

            return burnTime;
        }
        // Get the momentary (IE current dynamic) failure rates (Can vary per reliability/failure modules)
        // These  methods will let you get a list of all momentary rates or you can get the best (lowest chance of failure)/worst (highest chance of failure) rates
        public MomentaryFailureRate GetWorstMomentaryFailureRate()
        {
            //MomentaryFailureRate worstMFR;

            //worstMFR = new MomentaryFailureRate();
            //worstMFR.valid = false;
            //worstMFR.failureRate = TestFlightUtil.MIN_FAILURE_RATE;

            //foreach (MomentaryFailureRate mfr in momentaryFailureRates)
            //{
            //    if (mfr.failureRate > worstMFR.failureRate)
            //    {
            //        worstMFR = mfr;
            //    }
            //}

            double failureRate = TestFlightUtil.MIN_FAILURE_RATE;
            int mfrIndex = -1;
            for (int i = 0; i < momentaryFailureRates.Count; i++)
            {
                if (momentaryFailureRates[i].failureRate <= failureRate)
                    continue;
                mfrIndex = i;
            }

            return mfrIndex > -1 ? momentaryFailureRates[mfrIndex] : new MomentaryFailureRate();

        }
        public MomentaryFailureRate GetBestMomentaryFailureRate()
        {
            MomentaryFailureRate bestMFR;

            bestMFR = new MomentaryFailureRate();
            bestMFR.valid = false;
            bestMFR.failureRate = double.MaxValue;

            for (int i = 0, momentaryFailureRatesCount = momentaryFailureRates.Count; i < momentaryFailureRatesCount; i++)
            {
                MomentaryFailureRate mfr = momentaryFailureRates[i];
                if (mfr.failureRate >= bestMFR.failureRate)
                    continue;
                bestMFR = mfr;
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

            for (int i = 0, momentaryFailureModifiersCount = momentaryFailureModifiers.Count; i < momentaryFailureModifiersCount; i++)
            {
                MomentaryFailureModifier mfMod = momentaryFailureModifiers[i];
                if (mfMod.owner != ownerName || mfMod.triggerName != trigger)
                    continue;
                return mfMod;
            }

            return new MomentaryFailureModifier();
        }
        internal MomentaryFailureRate GetMomentaryFailureRate(String trigger)
        {
            trigger = trigger.ToLower().Trim();

            for (int i = 0, momentaryFailureRatesCount = momentaryFailureRates.Count; i < momentaryFailureRatesCount; i++)
            {
                MomentaryFailureRate mfRate = momentaryFailureRates[i];
                if (mfRate.triggerName != trigger)
                    continue;
                return mfRate;
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

            for (int i = 0, momentaryFailureModifiersCount = momentaryFailureModifiers.Count; i < momentaryFailureModifiersCount; i++)
            {
                MomentaryFailureModifier mfm = momentaryFailureModifiers[i];
                if (mfm.triggerName != trigger)
                    continue;
                totalModifiers *= mfm.modifier;
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

        public string FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm, int maximum)
        {
            return TestFlightUtil.FailureRateToMTBFString(failureRate, units, shortForm, maximum);
        }

        public void FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm,
            int maximum, out string output)
        {
            TestFlightUtil.FailureRateToMTBFString(failureRate, units, shortForm, maximum, out output);
        }

        // Simply converts the failure rate to a MTBF number, without any string formatting
        public double FailureRateToMTBF(double failureRate, TestFlightUtil.MTBFUnits units)
        {
            return TestFlightUtil.FailureRateToMTBF(failureRate, units);
        }
        // Get the FlightData or FlightTime for the part

        // New v1.3 noscope
        public float GetFlightData()
        {
            var data = currentFlightData + researchData + transferData;
            data = Mathf.Min(data, maxData);
            return data;
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
                TestFlightPartData partData = TestFlightManagerScenario.Instance.GetPartDataForPart(Alias);
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
                TestFlightManagerScenario.Instance.GetPartDataForPart(Alias).SetValue("flightTime", flightTime);
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
            float existingStoredFlightData = Mathf.Max(0,TestFlightManagerScenario.Instance.GetFlightDataForPartName(Alias));

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
            TestFlightManagerScenario.Instance.SetFlightDataForPartName(Alias, existingStoredFlightData + (newFlightData - existingData));
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
                newFlightTime = TestFlightManagerScenario.Instance.GetPartDataForPart(Alias).GetFloat("flightTime");
                if (additive)
                    newFlightTime += flightTime;
                else
                    newFlightTime *= flightTime;
                TestFlightManagerScenario.Instance.GetPartDataForPart(Alias).SetValue("flightTime", newFlightTime);
            }

            return newFlightTime;
        }

        public float GetEngineerDataBonus(float partEngineerBonus)
        {
            if (TestFlightManagerScenario.Instance == null)
                return 1;
            
            float globalFlightDataEngineerMultiplier = TestFlightManagerScenario.Instance.userSettings.flightDataEngineerMultiplier;

            float totalEngineerBonus = 0;
            for (int i = 0, crewCount = crew.Count; i < crewCount; i++)
            {
                ProtoCrewMember crewMember = crew[i];
                int engineerLevel = crewMember.experienceLevel;
                totalEngineerBonus = totalEngineerBonus + (partEngineerBonus * engineerLevel * globalFlightDataEngineerMultiplier);
            }
            float engineerModifier = 1.0f + totalEngineerBonus;

            return engineerModifier;
        }

        internal float ApplyFlightDataMultiplier(float baseData)
        {
            baseData *= dataRateLimiter;

            if (TestFlightManagerScenario.Instance == null)
                return baseData;

            return baseData * TestFlightManagerScenario.Instance.userSettings.flightDataMultiplier;
        }
        public ITestFlightFailure TriggerFailure()
        {
            return TriggerFailure("any");
        }
        // Cause a failure to occur, either a random failure or a specific one
        // If fallbackToRandom is true, then if the specified failure can't be found or can't be triggered, a random failure will be triggered instead
        // Returns the triggered failure module, or null if none
        public ITestFlightFailure TriggerFailure(string severity)
        {
            severity = severity.ToLowerInvariant();
            Log(string.Format("Triggering random {0} failure", severity));

            // Failure occurs.  Determine which failure module to trigger
            int totalWeight = 0;
            int currentWeight = 0;
            int chosenWeight = 0;
            List<ITestFlightFailure> failureModules = null;

            // Get all failure modules on the part
            // Then filter only the ones that are not disabled and are of the desired severity
            List<ITestFlightFailure> allFailureModules = TestFlightUtil.GetFailureModules(this.part, Alias);
            for (int i = 0, allFailureModulesCount = allFailureModules.Count; i < allFailureModulesCount; i++)
            {
                ITestFlightFailure fm = allFailureModules[i];
                PartModule pm = fm as PartModule;
                if (fm.GetFailureDetails().severity.ToLowerInvariant() == severity || severity == "any")
                {
                    if (fm.Failed)
                    {
                        Log(string.Format("Skipping {0} because it is already active", pm.moduleName));
                        continue;
                    }
                    if (!disabledFailures.Contains(pm.moduleName.Trim().ToLowerInvariant()))
                    {
                        if (failureModules == null)
                            failureModules = new List<ITestFlightFailure>();
                        failureModules.Add(fm);
                    }
                }
                else
                {
                    Log(string.Format("Skipping {0} because it doesn't have a matching severity ({1} != {2}", pm.moduleName, fm.GetFailureDetails().severity, severity));
                }
            }

            if (failureModules == null || failureModules.Count == 0)
            {
                Log("No failure modules to trigger");
                return null;
            }
            
            for (int i = 0, failureModulesCount = failureModules.Count; i < failureModulesCount; i++)
            {
                ITestFlightFailure fm = failureModules[i];
                totalWeight += fm.GetFailureDetails().weight;
            }
            chosenWeight = RandomGenerator.Next(1,totalWeight);
            for (int i = 0, failureModulesCount = failureModules.Count; i < failureModulesCount; i++)
            {
                ITestFlightFailure fm = failureModules[i];
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
            failureModuleName = failureModuleName.ToLower().Trim();

            List<ITestFlightFailure> failureModules;

            failureModules = TestFlightUtil.GetFailureModules(this.part, Alias);

            for (int i = 0, failureModulesCount = failureModules.Count; i < failureModulesCount; i++)
            {
                ITestFlightFailure fm = failureModules[i];
                PartModule pm = fm as PartModule;
                if (pm.moduleName.ToLower().Trim() == failureModuleName)
                {
                    if ((fm == null || fm.Failed) && fallbackToRandom)
                        return TriggerFailure();
                    else if (fm == null & !fallbackToRandom)
                        return null;
                    else
                    {
                        Log("Triggering Failure: " + pm.moduleName);
                        if (failures == null)
                            failures = new List<ITestFlightFailure>();
                        failures.Add(fm);
                        fm.DoFailure();
                        hasMajorFailure |= fm.GetFailureDetails().severity.ToLowerInvariant() == "major";
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
            failureModules = TestFlightUtil.GetFailureModules(this.part, Alias);

            for (int i = 0, failureModulesCount = failureModules.Count; i < failureModulesCount; i++)
            {
                ITestFlightFailure fm = failureModules[i];
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

        public List<ITestFlightFailure> GetActiveFailures()
        {
            return failures;
        }

        public void OnStageActivate(int stage)
        {
            GameEvents.onStageActivate.Remove(OnStageActivate);
            firstStaged = true;
            missionStartTime = Planetarium.GetUniversalTime();
            Log("First stage activated");
        }

        /// <summary>
        /// Determines whether the part is considered operating or not.
        /// </summary>
        public bool IsPartOperating()
        {
            Profiler.BeginSample("IsPartOperating");
            if (m_Recorder == null)
            {
                m_Recorder = GetComponent(typeof(IFlightDataRecorder)) as IFlightDataRecorder;
            }

            Profiler.EndSample();
            return m_Recorder.IsPartOperating();
        }

        // PARTMODULE functions
        public override void Update()
        {
            base.Update();

            if (!firstStaged)
                return;

            Profiler.BeginSample("Enabled check");
            if (!TestFlightEnabled)
                return;
            Profiler.EndSample();

            Profiler.BeginSample("Flight check");
            if (HighLogic.LoadedSceneIsFlight)
            {

                Profiler.BeginSample("Scenario check");
                if (TestFlightManagerScenario.Instance == null)
                    return;
                if (!TestFlightManagerScenario.Instance.SettingsEnabled)
                    return;
                Profiler.EndSample();

                Profiler.BeginSample("Icon check");
                if (this.part.stackIcon != null)
                {
                    Profiler.BeginSample("Failure check");
                    if (failures != null && failures.Count > 0)
                    {
                        Profiler.BeginSample("Icon fail coloring");
                        part.stackIcon.SetBackgroundColor(hasMajorFailure
                            ? XKCDColors.Red
                            : XKCDColors.KSPNotSoGoodOrange);
                        Profiler.EndSample();
                    }
                    else
                    {
                        part.stackIcon.SetBackgroundColor(XKCDColors.White);
                    }
                    Profiler.EndSample();
                }
                Profiler.EndSample();


                Profiler.BeginSample("Time logging");
                double currentMET = Planetarium.GetUniversalTime() - missionStartTime;
                Profiler.EndSample();
                Profiler.BeginSample("Operating check");
                if (IsPartOperating())
                {
                    operatingTime += (float)(currentMET - lastMET);
                }
                Profiler.EndSample();
                lastMET = currentMET;
            }
            Profiler.EndSample();
        }

        public override void Start()
        {
            if (!TestFlightEnabled)
                return;

            CalculateMaximumData();

            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onCrewTransferred.Add(OnCrewChange);
                if (crew == null)
                    crew = new List<ProtoCrewMember>();

                crew.Clear();
                List<ProtoCrewMember> allCrew = part.vessel.GetVesselCrew();
                for (int i = 0, crewCount = allCrew.Count; i < crewCount; i++)
                {
                    if (allCrew[i].experienceTrait.Title == "Engineer")
                    {
                        crew.Add(allCrew[i]);
                    }
                }
                if (vessel.situation == Vessel.Situations.PRELAUNCH)
                {
                    GameEvents.onStageActivate.Add(OnStageActivate);
                    firstStaged = false;
                }
                else
                {
                    firstStaged = true;
                    missionStartTime = Planetarium.GetUniversalTime();
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onCrewTransferred.Remove(OnCrewChange);
            }
        }

        public override void OnAwake()
        {
            initialized = false;
            
            if (!string.IsNullOrEmpty(configNodeData))
            {
                var node = ConfigNode.Parse(configNodeData);
                OnLoad(node);
            }
            
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

            // poll failure modules for any existing failures
            if (failures == null)
                failures = new List<ITestFlightFailure>();
            List<ITestFlightFailure> failureModules = TestFlightUtil.GetFailureModules(this.part, Alias);
            for (int i = 0, failureModulesCount = failureModules.Count; i < failureModulesCount; i++)
            {
                ITestFlightFailure failure = failureModules[i];
                if (failure.Failed)
                {
                    failures.Add(failure);
                    hasMajorFailure |= failure.GetFailureDetails().severity.ToLowerInvariant() == "major";
                }
            }
        }

        public void OnCrewChange(GameEvents.HostedFromToAction<ProtoCrewMember, Part> e)
        {
            crew.Clear();
            List<ProtoCrewMember> allCrew = part.vessel.GetVesselCrew();
            for (int i = 0, crewCount = allCrew.Count; i < crewCount; i++)
            {
                if (allCrew[i].experienceTrait.Title == "Engineer")
                {
                    crew.Add(allCrew[i]);
                }
            }
        }

        public void InitializeFlightData(float flightData)
        {
            if (initialized)
                return;

            researchData = Mathf.Min(TestFlightManagerScenario.Instance.GetResearchDataForPartName(Alias), rndMaxData);
            if (researchData < 0f) researchData = 0f;
            transferData = AttemptTechTransfer();
            if (transferData < 0f) transferData = 0f;
            TestFlightManagerScenario.Instance.SetTransferDataForPartName(Alias, transferData);

            flightData = Mathf.Max(startFlightData, flightData);
                
            currentFlightData = Mathf.Min(maxData, flightData);
            initialFlightData = Mathf.Min(maxData, flightData + researchData + transferData);

            TestFlightManagerScenario.Instance.SetFlightDataForPartName(Alias, flightData);
            missionStartTime = Planetarium.GetUniversalTime();

            initialized |= HighLogic.LoadedSceneIsFlight;
        }

        public float GetTechTransfer()
        {
            return AttemptTechTransfer();
        }

        internal float AttemptTechTransfer()
        {
            // attempts to transfer data from a predecessor part
            // parts can be referenced either by part name, full name, or configuration name
            // multiple branches can be specified with the & character
            // multiple parts in a branch can be specified by separating them with a comma
            // for each branch the first part listed is considered the closest part, and each part after is considered to be one generation removed.  An optional generation penalty is added for each level
            //  for each branch, the flight data from each part is added together including any generation penalties, to create a total for that branch, modified by the transfer amount for that branch
            // if multiple branches are specified, each branch is then added together
            // an optional maximum data can be enforced for each scope (global setting but applied to each scope, not total)
            // Example
            // techTransfer = rs-27a,rs-27,h1-b,h1:10&lr-89-na-7,lr-89-na-6,lr-89-na-5:25
            // defines two branches, one from the RS-27 branch and one from the LR-89 branch.  

            Debug.Log($"[TestFlight] Attempting TechTransfer for part {Configuration}");
            Debug.Log($"[TestFlight] techTransfer: {techTransfer.Trim()}");
            
            if (techTransfer.Trim() == "")
                return 0f;

            float dataToTransfer = 0f;
            string[] branches;
            string[] modifiers;

            branches = techTransfer.Split(new char[1]{ '&' });

            for (int i = 0, branchesLength = branches.Length; i < branchesLength; i++)
            {
                string branch = branches[i];
                Debug.Log($"[TestFlight] Processing Branch {branch}");
                modifiers = branch.Split(new char[1] {
                    ':'
                });
                if (modifiers.Length < 2)
                    continue;
                string[] partsInBranch = modifiers[0].Split(new char[1] {
                    ','
                });
                float branchModifier = float.Parse(modifiers[1]);
                branchModifier /= 100f;
                int generation = 0;
                for (int j = 0, partsInBranchLength = partsInBranch.Length; j < partsInBranchLength; j++)
                {
                    string partName = partsInBranch[j];
                    float partFlightData = TestFlightManagerScenario.Instance.GetFlightDataForPartName(partName);
                    Debug.Log($"[TestFlight] Existing data for {partName}: {partFlightData}");
                    if (partFlightData == 0f)
                        continue;
                    dataToTransfer = dataToTransfer + ((partFlightData - (partFlightData * generation * techTransferGenerationPenalty)) * branchModifier);
                    generation++;
                }
            }

            Debug.Log($"[TestFlight] Data to Transfer: {dataToTransfer}");
            return Mathf.Min(dataToTransfer, techTransferMax);
        }

        public float ForceRepair(ITestFlightFailure failure)
        {
            if (failure == null)
                return 0;

            failure.ForceRepair();
            // update our major failure flag in case this repair changes things
            hasMajorFailure = HasMajorFailure();

            return 0;
        }

        private bool HasMajorFailure()
        {
            if (failures == null)
                return false;
            if (failures.Count <= 0)
                return false;
            
            for (int i = 0; i < failures.Count; i++)
            {
                if (failures[i].GetFailureDetails().severity.ToLowerInvariant() == "major")
                    return true;
            }

            return false;
        }
            
        public void HighlightPart(bool doHighlight)
        {
//            Color highlightColor;
//            if (activeFailure == null)
//                highlightColor = XKCDColors.HighlighterGreen;
//            else
//            {
//                if (activeFailure.GetFailureDetails().severity == "major")
//                    highlightColor = XKCDColors.Red;
//                else
//                    highlightColor = XKCDColors.OrangeYellow;
//            }
//
//            if (doHighlight)
//            {
//                this.part.SetHighlightDefault();
//                this.part.SetHighlightType(Part.HighlightType.AlwaysOn);
//                this.part.SetHighlight(true, false);
//                this.part.SetHighlightColor(highlightColor);
//                HighlightingSystem.Highlighter highlighter;
//                highlighter = this.part.FindModelTransform("model").gameObject.AddComponent<HighlightingSystem.Highlighter>();
//                highlighter.ConstantOn(highlightColor);
//                highlighter.SeeThroughOn();
//            }
//            else
//            {
//                this.part.SetHighlightDefault();
//                this.part.SetHighlightType(Part.HighlightType.AlwaysOn);
//                this.part.SetHighlight(false, false);
//                this.part.SetHighlightColor(XKCDColors.HighlighterGreen);
//                Destroy(this.part.FindModelTransform("model").gameObject.GetComponent(typeof(HighlightingSystem.Highlighter)));
//            }
        }

        public virtual int GetPartStatus()
        {
            if (failures == null || failures.Count <= 0)
                return 0;
            else
                return 1;
        }

        public override string GetModuleDisplayName()
        {
            return string.Format("Test Flight reliability for {0}", Title);
        }

        public override string GetInfo()
        {
            // This methods collects data from all the TestFlight configs and combines it into one string
            List<string> configStrings = new List<string>();

            foreach (var infoConfig in configs)
            {
                if (!infoConfig.HasValue("configuration"))
                    continue;

                var nodeConfiguration = infoConfig.GetValue("configuration");
                if (string.IsNullOrEmpty(nodeConfiguration))
                    continue;

                string nodeAlias = nodeConfiguration.Contains(":") ? nodeConfiguration.Split(new char[1] { ':' })[1] : nodeConfiguration;
                
                string nodeTitle = null;
                infoConfig.TryGetValue("title", ref nodeTitle);
                if (string.IsNullOrEmpty(nodeTitle))
                    nodeTitle = nodeAlias;

                // This methods collects data from all the TestFlight modules and combines it into one string
                List<string> infoStrings = new List<string>();

                List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, nodeAlias, false);
                if (reliabilityModules != null)
                {
                    float ratedBurnTime = 0f;
                    for (int i = 0, reliabilityModulesCount = reliabilityModules.Count; i < reliabilityModulesCount; i++)
                    {
                        ITestFlightReliability reliabilityModule = reliabilityModules[i];
                        ratedBurnTime = Mathf.Max(reliabilityModule.GetRatedBurnTime(nodeAlias), ratedBurnTime);
                    }

                    for (int i = 0, reliabilityModulesCount = reliabilityModules.Count; i < reliabilityModulesCount; i++)
                    {
                        ITestFlightReliability reliabilityModule = reliabilityModules[i];
                        var s = reliabilityModule.GetModuleInfo(nodeAlias, ratedBurnTime);
                        if (!string.IsNullOrEmpty(s))
                            infoStrings.Add(s);
                    }
                }

                List<ITestFlightFailure> failureModules = TestFlightUtil.GetFailureModules(this.part, nodeAlias, false);
                if (failureModules != null)
                {
                    for (int i = 0, failureModulesCount = failureModules.Count; i < failureModulesCount; i++)
                    {
                        ITestFlightFailure failureModule = failureModules[i];
                        var s = failureModule.GetModuleInfo(nodeAlias);
                        if (!string.IsNullOrEmpty(s))
                            infoStrings.Add(s);
                    }
                }

                if (infoStrings.Count > 0)
                {
                    infoStrings.Insert(0, $"<b><color=#ffb400ff>{nodeTitle}</color></b>");
                    configStrings.Add(string.Join("\n", infoStrings.ToArray()));
                }
            }

            if (configStrings.Count > 0)
            {
                return string.Join("\n\n", configStrings.ToArray());
            }

            return base.GetInfo();
        }

        public List<string> GetTestFlightInfo()
        {
            string indent = "    ";

            List<string> infoStrings = new List<string>();
            string partName = Alias;
            infoStrings.Add("<b>Core</b>");
            infoStrings.Add(indent + "  <b>Active Part</b>: " + partName);
            // float flightData = TestFlightManagerScenario.Instance.GetFlightDataForPartName(partName);
            var flightData = GetFlightData();
            if (flightData < 0f)
                flightData = 0f;
            infoStrings.Add(indent + String.Format("  <b>Flight Data</b>: {0:f1}/{1:f1}", flightData, maxData));
            infoStrings.Add("");

            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, Alias);
            if (reliabilityModules != null)
            {
                float ratedBurnTime = 0f;
                for (int i = 0, reliabilityModulesCount = reliabilityModules.Count; i < reliabilityModulesCount; i++)
                {
                    ITestFlightReliability reliabilityModule = reliabilityModules[i];
                    ratedBurnTime = Mathf.Max(reliabilityModule.GetRatedBurnTime(), ratedBurnTime);
                }

                for (int i = 0, reliabilityModulesCount = reliabilityModules.Count; i < reliabilityModulesCount; i++)
                {
                    ITestFlightReliability reliabilityModule = reliabilityModules[i];
                    List<string> infoColl = reliabilityModule.GetTestFlightInfo(ratedBurnTime);
                    if (infoColl != null)
                    {
                        // Don't indent header string
                        infoStrings.Add(infoColl[0]);
                        infoColl.RemoveAt(0);
                        foreach (string s in infoColl)
                            infoStrings.Add(indent + s);
                        infoStrings.Add("");
                    }
                }
            }

            List<ITestFlightFailure> failureModules = TestFlightUtil.GetFailureModules(this.part, Alias);
            if (failureModules != null)
            {
                for (int i = 0, failureModulesCount = failureModules.Count; i < failureModulesCount; i++)
                {
                    ITestFlightFailure failureModule = failureModules[i];
                    List<string> infoColl = failureModule.GetTestFlightInfo();
                    if (infoColl != null)
                    {
                        // Don't indent header string
                        infoStrings.Add(infoColl[0]);
                        infoColl.RemoveAt(0);
                        foreach (string s in infoColl)
                            infoStrings.Add(indent + s);
                        infoStrings.Add("");
                    }
                }
            }

            infoStrings.RemoveAt(infoStrings.Count - 1);

            return infoStrings;
        }

        public void UpdatePartConfig()
        {
            Log("Updating part config");

            SetActiveConfigFromInterop();
            
            // enabled = ActiveConfiguration;
            // active = enabled;

            enabled = true;
            active = true;

            List<PartModule> tfPartModules = TestFlightAPI.TestFlightUtil.GetAllTestFlightModulesForPart(part);
            foreach (var partModule in tfPartModules)
            {
                // FlightDataRecorder
                IFlightDataRecorder recorder = partModule as IFlightDataRecorder;
                if (recorder != null)
                {
                    recorder.SetActiveConfig(Alias);
                }

                // TestFlightReliability
                ITestFlightReliability reliability = partModule as ITestFlightReliability;
                if (reliability != null)
                    reliability.SetActiveConfig(Alias);
                
                // TestFlightFailure
                ITestFlightFailure failure = partModule as ITestFlightFailure;
                if (failure != null)
                {
                    failure.SetActiveConfig(Alias);
                }
            }
            

            List<PartModule> testFlightModules = TestFlightUtil.GetAllTestFlightModulesForAlias(this.part, Alias);
            for (int i = 0; i < testFlightModules.Count; i++)
            {
                testFlightModules[i].enabled = enabled;
            }


            if (TestFlightManagerScenario.Instance.SettingsAlwaysMaxData)
                InitializeFlightData(maxData);
            else
            {
                InitializeFlightData(Mathf.Max(0, TestFlightManagerScenario.Instance.GetFlightDataForPartName(Alias)));
            }
            
            if (Events == null)
                return;
            
            // BaseEvent toggleRNDGUIEvent = Events["ToggleRNDGUI"];
            // if (toggleRNDGUIEvent != null)
            // {
            //     toggleRNDGUIEvent.guiActiveEditor = enabled;
            //     toggleRNDGUIEvent.guiName = string.Format("R&D {0}", Alias);
            // }
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


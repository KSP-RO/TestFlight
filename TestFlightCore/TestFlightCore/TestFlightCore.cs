using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using TestFlightCore.KSPPluginFramework;
using TestFlightAPI;
using System.Linq;

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

        public List<ConfigNode> configs = new List<ConfigNode>(8);
        public ConfigNode currentConfig;
        public string configNodeData;

        bool initialized = false;
        float transferData;
        float researchData;

        private double baseFailureRate;
        // We store the base, or initial, flight data for calculation of Base Failure Rate
        // Momentary Failure Rates are calculated based on modifiers.  Those modifiers
        // are stored per SCOPE and per TRIGGER
        private readonly List<MomentaryFailureRate> momentaryFailureRates = new List<MomentaryFailureRate>();
        private readonly List<MomentaryFailureModifier> momentaryFailureModifiers = new List<MomentaryFailureModifier>();
        private readonly List<ITestFlightFailure> failures = new List<ITestFlightFailure>();
        private readonly List<string> disabledFailures = new List<string>();
        private bool hasMajorFailure = false;
        private bool firstStaged;

        // These were created for KCT integration but might have other uses
        private float dataRateLimiter = 1.0f;
        private float dataCap = 1.0f;

        private readonly List<ProtoCrewMember> crew = new List<ProtoCrewMember>();

        private bool active;
        private bool TestFlightScenarioReady => TestFlightManagerScenario.Instance != null && TestFlightManagerScenario.Instance.isReady;

        private readonly string[] ops = { "=", "!=", "<", "<=", ">", ">=", "<>", "<=>" };
        private readonly char[] colonSeparator = new char[1] { ':' };

        IFlightDataRecorder m_Recorder;

        public bool ActiveConfiguration
        {
            get
            {
                if (!TestFlightEnabled) return false;
                SetActiveConfigFromInterop();
                return true;
            }
        }

        public bool TestFlightEnabled => TestFlightManagerScenario.Instance != null && TestFlightManagerScenario.Instance.SettingsEnabled && active;
        public string Configuration
        {
            get 
            { 
                if (configuration.Equals(string.Empty))
                {
                    string s = TestFlightUtil.GetPartName(this.part);
                    configuration = $"kspPartName = {s}:{s}";
                }

                return configuration; 
            }
            set 
            { 
                configuration = value;
                _alias = configuration.Contains(":") ? configuration.Split(colonSeparator)[1] : string.Empty;
            }
        }
        private string _alias = string.Empty;
        public string Alias => _alias;
        public string Title => string.IsNullOrEmpty(title) ? part.partInfo.title : title;
        public bool DebugEnabled => TestFlightManagerScenario.Instance != null && TestFlightManagerScenario.Instance.userSettings.debugLog;

        bool SetActiveConfigFromInterop()
        {
            ConfigNode prevConfig = currentConfig;
            
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
                        test = test.Split(colonSeparator)[0];
                    }

                    if (test.Equals(TestFlightUtil.GetPartName(part), StringComparison.OrdinalIgnoreCase))
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

            if (currentConfig == null) return prevConfig == null;

            // update current values with those from the current config node
            currentConfig.TryGetValue("startFlightData", ref startFlightData);
            currentConfig.TryGetValue("configuration", ref configuration);
            // Apply side-effect of updating alias
            Configuration = configuration;
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

            // Invalidate cached value.
            if (prevConfig != currentConfig)
            {
                baseFailureRate = 0f;
            }

            return prevConfig == currentConfig;
        }

        [KSPEvent(guiActiveEditor=false, guiName = "R&D Window")]            
        public void ToggleRNDGUI()
        {
            TestFlightEditorWindow.Instance.LockPart(this.part, Alias);
            TestFlightEditorWindow.Instance.ToggleWindow();
        }

        public System.Random RandomGenerator => TestFlightManagerScenario.RandomGenerator;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (node.HasNode("MODULE"))
                node = node.GetNode("MODULE");

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
            TestFlightUtil.Log($"TestFlightCore({Alias}[{Configuration}]): {message}", true);
        }
        private void CalculateMaximumData()
        {
            if (maxData > 0f)
                return;
            
            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, Alias);
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
            if (maxData <= 0)
                CalculateMaximumData();
            return maxData;
        }

        // Get the base or static failure rate
        public double GetBaseFailureRate()
        {
            if (baseFailureRate > 0)
                return baseFailureRate;

            double totalBFR = 0f;
            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, Alias);
            foreach (ITestFlightReliability rm in reliabilityModules)
            {
                totalBFR += rm.GetBaseFailureRate(initialFlightData);
            }
            Log($"BFR: {totalBFR:F7}, Modifier: {failureRateModifier:F7}");
            totalBFR *= failureRateModifier;
            baseFailureRate = Math.Max(totalBFR, TestFlightUtil.MIN_FAILURE_RATE);
            return baseFailureRate;
        }
        // Get the Reliability Curve for the part
        public FloatCurve GetBaseReliabilityCurve()
        {
            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, Alias);
            foreach (ITestFlightReliability rm in reliabilityModules)
            {
                FloatCurve curve = rm.GetReliabilityCurve();
                if (curve != null)
                    return curve;
            }

            return null;
        }
        public float GetRunTime(RatingScope ratingScope)
        {
            float burnTime = 0f;
            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, Alias);

            foreach (ITestFlightReliability rm in reliabilityModules)
                burnTime = Mathf.Max(rm.GetScopedRunTime(ratingScope), burnTime);

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

            double worstRate = TestFlightUtil.MIN_FAILURE_RATE;
            int mfrIndex = -1;
            for (int i = 0; i < momentaryFailureRates.Count; i++)
            {
                if (momentaryFailureRates[i].failureRate > worstRate)
                {
                    worstRate = momentaryFailureRates[i].failureRate;
                    mfrIndex = i;
                }
            }

            return mfrIndex > -1 ? momentaryFailureRates[mfrIndex] : new MomentaryFailureRate();

        }
        public MomentaryFailureRate GetBestMomentaryFailureRate()
        {
            MomentaryFailureRate bestMFR = new MomentaryFailureRate
            {
                valid = false,
                failureRate = double.MaxValue
            };
            foreach (var mfr in momentaryFailureRates)
            {
                if (mfr.failureRate < bestMFR.failureRate)
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
        internal MomentaryFailureModifier GetMomentaryFailureModifier(string trigger, string owner)
        {
            trigger = trigger.Trim();
            string ownerName = owner.Trim();

            foreach (MomentaryFailureModifier mfMod in momentaryFailureModifiers)
            {
                if (mfMod.owner.Equals(ownerName, StringComparison.OrdinalIgnoreCase) &&
                    mfMod.triggerName.Equals(trigger, StringComparison.OrdinalIgnoreCase))
                    return mfMod;
            }

            return new MomentaryFailureModifier();
        }
        internal MomentaryFailureRate GetMomentaryFailureRate(string trigger)
        {
            trigger = trigger.Trim();

            foreach (MomentaryFailureRate mfRate in momentaryFailureRates)
                if (mfRate.triggerName.Equals(trigger, StringComparison.OrdinalIgnoreCase))
                    return mfRate;

            return new MomentaryFailureRate();
        }

        public double SetTriggerMomentaryFailureModifier(string trigger, double multiplier, PartModule owner)
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
            trigger = trigger.Trim();
            double baseFailureRate = GetBaseFailureRate();
            double totalModifiers = 1;

            foreach (MomentaryFailureModifier mfm in momentaryFailureModifiers)
            {
                if (mfm.triggerName.Equals(trigger, StringComparison.OrdinalIgnoreCase))
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
            return Mathf.Min(maxData, currentFlightData + researchData + transferData);
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
                return partData.flightTime;
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
                TestFlightManagerScenario.Instance.GetPartDataForPart(Alias).flightTime = flightTime;
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
                newFlightTime = TestFlightManagerScenario.Instance.GetPartDataForPart(Alias).flightTime;
                if (additive)
                    newFlightTime += flightTime;
                else
                    newFlightTime *= flightTime;
                TestFlightManagerScenario.Instance.GetPartDataForPart(Alias).flightTime = newFlightTime;
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
                totalEngineerBonus += partEngineerBonus * engineerLevel * globalFlightDataEngineerMultiplier;
            }
            float engineerModifier = 1.0f + totalEngineerBonus;

            return engineerModifier;
        }

        internal float ApplyFlightDataMultiplier(float baseData)
        {
            baseData *= dataRateLimiter;
            float mult = TestFlightManagerScenario.Instance == null ? 1 : TestFlightManagerScenario.Instance.userSettings.flightDataMultiplier;
            return baseData * mult;
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
            // Failure occurs.  Determine which failure module to trigger
            int totalWeight = 0;
            int currentWeight = 0;
            var failureModules = new List<ITestFlightFailure>();

            // Get all failure modules on the part
            // Then filter only the ones that are not disabled and are of the desired severity
            foreach (ITestFlightFailure fm in TestFlightUtil.GetFailureModules(this.part, Alias))
            {
                PartModule pm = fm as PartModule;
                var details = fm.GetFailureDetails();
                if (severity.Equals(details.severity, StringComparison.OrdinalIgnoreCase) || severity == "any")
                    if (!fm.Failed && pm != null && !disabledFailures.Contains(pm.moduleName.Trim().ToLowerInvariant()))
                    {
                        failureModules.Add(fm);
                        totalWeight += details.weight;
                    }
            }

            if (failureModules.Count == 0)
            {
                Log($"No failure modules to trigger for severity {severity}");
                return null;
            }
            int chosenWeight = RandomGenerator.Next(1, Math.Max(totalWeight, 1));
            foreach (ITestFlightFailure fm in failureModules)
            {
                currentWeight += fm.GetFailureDetails().weight;
                if (currentWeight >= chosenWeight)
                {
                    // Trigger this module's failure
                    return TriggerNamedFailure((fm as PartModule).moduleName, false);
                }
            }
            Debug.LogError($"Failed to fail! {failureModules.Count} options for severity: {severity}, chose {chosenWeight} from {totalWeight}?");
            return null;
        }
        public ITestFlightFailure TriggerNamedFailure(String failureModuleName)
        {
            return TriggerNamedFailure(failureModuleName, false);
        }
        public ITestFlightFailure TriggerNamedFailure(String failureModuleName, bool fallbackToRandom)
        {
            failureModuleName = failureModuleName.ToLower().Trim();

            ITestFlightFailure fm = TestFlightUtil.GetFailureModules(this.part, Alias).FirstOrDefault(x => (x as PartModule).moduleName.Equals(failureModuleName, StringComparison.OrdinalIgnoreCase) && !x.Failed);
            if (fm != null)
            {
                Log($"Triggering Failure: {(fm as PartModule).moduleName}");
                failures.Add(fm);
                fm.DoFailure();
                hasMajorFailure |= fm.GetFailureDetails().severity.ToLowerInvariant() == "major";
            } else if (fallbackToRandom)
                fm = TriggerFailure();
            return fm;
        }
        // Returns a list of all available failures on the part
        public List<string> GetAvailableFailures()
        {
            List<string> failureModulesString = new List<string>();
            foreach (var fm in TestFlightUtil.GetFailureModules(this.part, Alias))
                failureModulesString.Add((fm as PartModule).moduleName);
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
        }

        /// <summary>
        /// Determines whether the part is considered operating or not.
        /// </summary>
        public bool IsPartOperating()
        {
            if (m_Recorder == null)
                m_Recorder = GetComponent<IFlightDataRecorder>();
            return m_Recorder != null && m_Recorder.IsPartOperating();
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
                if (this.part.stackIcon != null)
                {
                    var color = failures.Count == 0 ? XKCDColors.White :
                                hasMajorFailure ? XKCDColors.Red : XKCDColors.KSPNotSoGoodOrange;
                    part.stackIcon.SetBackgroundColor(color);
                }

                double currentMET = Planetarium.GetUniversalTime() - missionStartTime;
                if (IsPartOperating())
                    operatingTime += (float)(currentMET - lastMET);
                lastMET = currentMET;
            }
        }

        public IEnumerator InitializeData(float du=0)
        {
            yield return new WaitUntil(() => TestFlightManagerScenario.Instance != null);
            yield return new WaitUntil(() => TestFlightManagerScenario.Instance.isReady);
            
            if (TestFlightManagerScenario.Instance.SettingsAlwaysMaxData)
                InitializeFlightData(maxData);
            else
            {
                var data = Mathf.Max(0, TestFlightManagerScenario.Instance.GetFlightDataForPartName(Alias));
                InitializeFlightData(data);
            }
        }
        public override void Start()
        {
            if (!TestFlightEnabled)
                return;

            CalculateMaximumData();
            if (!TestFlightScenarioReady)
                StartCoroutine(InitializeData());
            else
            {
                float data;
                if (TestFlightManagerScenario.Instance.SettingsAlwaysMaxData)
                    data = maxData;
                else if (HighLogic.LoadedSceneIsFlight && vessel.situation != Vessel.Situations.PRELAUNCH)
                    data = currentFlightData;
                else
                    data = Mathf.Max(0, TestFlightManagerScenario.Instance.GetFlightDataForPartName(Alias));
                InitializeFlightData(data);
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onCrewTransferred.Add(OnCrewChange);
                _OnCrewChange();
                firstStaged = vessel.situation != Vessel.Situations.PRELAUNCH;
                if (vessel.situation == Vessel.Situations.PRELAUNCH)
                    GameEvents.onStageActivate.Add(OnStageActivate);
                else
                    missionStartTime = Planetarium.GetUniversalTime();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onCrewTransferred.Remove(OnCrewChange);
                GameEvents.onStageActivate.Remove(OnStageActivate);
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
            
            // poll failure modules for any existing failures
            foreach (ITestFlightFailure failure in TestFlightUtil.GetFailureModules(this.part, Alias))
            {
                if (failure.Failed)
                {
                    failures.Add(failure);
                    hasMajorFailure |= failure.GetFailureDetails().severity.ToLowerInvariant() == "major";
                }
            }
        }

        public void OnCrewChange(GameEvents.HostedFromToAction<ProtoCrewMember, Part> e) => _OnCrewChange();
        private void _OnCrewChange()
        {
            crew.Clear();
            foreach (ProtoCrewMember c in part.vessel.GetVesselCrew())
                if (c.experienceTrait.Title == "Engineer")
                    crew.Add(c);
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

            if (techTransfer.Trim() == "")
                return 0f;

            float dataToTransfer = 0f;
            string[] branches;
            string[] modifiers;

            branches = techTransfer.Split(new char[1]{ '&' });

            for (int i = 0, branchesLength = branches.Length; i < branchesLength; i++)
            {
                string branch = branches[i];
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
                    if (partFlightData == 0f)
                        continue;
                    dataToTransfer += ((partFlightData - (partFlightData * generation * techTransferGenerationPenalty)) * branchModifier);
                    generation++;
                }
            }

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
            foreach (var failure in failures)
                if (failure.GetFailureDetails().severity.Equals("major", StringComparison.OrdinalIgnoreCase))
                    return true;
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
            return failures.Count == 0 ? 0 : 1;
        }

        public override string GetModuleDisplayName() => $"Test Flight reliability for {Title}";

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

                float ratedBurnTime = 0f;
                List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, nodeAlias, false);
                foreach (var reliabilityModule in reliabilityModules)
                {
                    ratedBurnTime = Mathf.Max(reliabilityModule.GetRatedTime(nodeAlias, RatingScope.Cumulative), ratedBurnTime);
                }
                foreach (var reliabilityModule in reliabilityModules)
                {
                    string s = reliabilityModule.GetModuleInfo(nodeAlias, ratedBurnTime);
                    if (!string.IsNullOrEmpty(s))
                        infoStrings.Add(s);
                }

                foreach (var failureModule in TestFlightUtil.GetFailureModules(this.part, nodeAlias, false))
                {
                    string s = failureModule.GetModuleInfo(nodeAlias);
                    if (!string.IsNullOrEmpty(s))
                        infoStrings.Add(s);
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

            float ratedBurnTime = 0f;
            List<ITestFlightReliability> reliabilityModules = TestFlightUtil.GetReliabilityModules(this.part, Alias);
            foreach (var reliabilityModule in reliabilityModules)
                ratedBurnTime = Mathf.Max(reliabilityModule.GetRatedTime(RatingScope.Cumulative), ratedBurnTime);

            foreach (var reliabilityModule in reliabilityModules)
            {
                List<string> infoColl = reliabilityModule.GetTestFlightInfo(ratedBurnTime);
                if (infoColl.Count > 0)
                {
                    // Don't indent header string
                    infoStrings.Add(infoColl[0]);
                    infoColl.RemoveAt(0);
                    foreach (string s in infoColl)
                        infoStrings.Add(indent + s);
                    infoStrings.Add("");
                }
            }

            foreach (var failureModule in TestFlightUtil.GetFailureModules(this.part, Alias))
            {
                List<string> infoColl = failureModule.GetTestFlightInfo();
                if (infoColl.Count > 0)
                {
                    // Don't indent header string
                    infoStrings.Add(infoColl[0]);
                    infoColl.RemoveAt(0);
                    foreach (string s in infoColl)
                        infoStrings.Add(indent + s);
                    infoStrings.Add("");
                }
            }

            infoStrings.RemoveAt(infoStrings.Count - 1);

            return infoStrings;
        }

        public void UpdatePartConfig()
        {
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


            if (TestFlightScenarioReady && HighLogic.LoadedSceneIsEditor)
            {
                float data = TestFlightManagerScenario.Instance.SettingsAlwaysMaxData
                            ? maxData : Mathf.Max(0, TestFlightManagerScenario.Instance.GetFlightDataForPartName(Alias));
                InitializeFlightData(data);
            }
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


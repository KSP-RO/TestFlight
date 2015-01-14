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


        private Dictionary<String, double> momentaryFailureRates;
        private double baseFailureRate;
        private FlightDataConfig baseFlightData;

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
        // TODO
        // Implement theses
        // Get the base or static failure rate
        public double GetBaseFailureRate()
        {
            return GetBaseFailureRateForScope(GetScope());
        }
        public double GetBaseFailureRateForScope(String scope)
        {
            return 0;
        }
        // Get the momentary (IE current dynamic) failure rates (Can vary per reliability/failure modules)
        // These  methods will let you get a list of all momentary rates or you can get the best (lowest chance of failure)/worst (highest chance of failure) rates
        public Dictionary<String, double> GetWorstMomentaryFailureRate()
        {
            return GetWorstMomentaryFailureRateForScope(GetScope());
        }
        public Dictionary<String, double> GetBestMomentaryFailureRate()
        {
            return GetBestMomentaryFailureRateForScope(GetScope());
        }
        public Dictionary<String, double> GetAllMomentaryFailureRates()
        {
            return GetAllMomentaryFailureRatesForScope(GetScope());
        }
        public Dictionary<String, double> GetWorstMomentaryFailureRateForScope(String scope)
        {
            Dictionary<String, double> failureRate = new Dictionary<string, double>();
            failureRate.Add("TODO", 0);
            return failureRate;
        }
        public Dictionary<String, double> GetBestMomentaryFailureRateForScope(String scope)
        {
            Dictionary<String, double> failureRate = new Dictionary<string, double>();
            failureRate.Add("TODO", 0);
            return failureRate;
        }
        public Dictionary<String, double> GetAllMomentaryFailureRatesForScope(String scope)
        {
            Dictionary<String, double> failureRate = new Dictionary<string, double>();
            failureRate.Add("TODO", 0);
            return failureRate;
        }
        public double GetMomentaryFailureRateForTrigger(String trigger)
        {
            return 0;
        }
        public double GetMomentaryFailureRateForTriggerForScope(String trigger, String scope)
        {
            return 0;
        }
        // The base failure rate can be modified with a multipler that is applied during flight only
        // Returns the total modified failure rate back to the caller for convenience
        public double ModifyBaseFailureRate(double multiplier)
        {
            return ModifyBaseFailureRateForScope(GetScope(), multiplier);
        }
        public double ModifyBaseFailureRateForScope(String scope, double multiplier)
        {
            return 0;
        }
        // The momentary failure rate is tracked per named "trigger" which allows multiple Reliability or FailureTrigger modules to cooperate
        // Returns the total modified failure rate back to the caller for convenience
        public double ModifyTriggerMomentaryFailureRate(String module, double multiplier)
        {
            return ModifyTriggerMomentaryFailureRateForScope(module, GetScope(), multiplier);
        }
        public double ModifyTriggerMomentaryFailureRateForScope(String module, String scope, double multiplier)
        {
            return 0;
        }
        // simply converts the failure rate into a MTBF string.  Convenience method
        // Returned string will be of the format "123 units"
        // units should be one of:
        //  seconds, hours, days, months, years, flights, missions
        public String FailureRateToMTBFString(double failureRate, String units)
        {
            return FailureRateToMTBFString(failureRate, units, false);
        }
        // Short version of MTBFString uses a single letter to denote (s)econds, (m)inutes, (h)ours, (d)ays, (y)ears
        // The returned string will be of the format "12s" or ".2d"
        public String FailureRateToMTBFString(double failureRate, String units, bool shortForm)
        {
            if (shortForm)
            {
                return String.Format("{0:F2}{1}", FailureRateToMTBF(failureRate, units), units.ToLower()[0]);
            }
            else
            {
                return String.Format("{0:F2} {1}", FailureRateToMTBF(failureRate, units), units.ToLower());
            }
        }
        // Simply converts the failure rate to a MTBF number, without any string formatting
        public double FailureRateToMTBF(double failureRate, String units)
        {
            double mtbfSeconds = 1.0 / failureRate;

            units = units.ToLower().Trim();
            switch (units)
            {
                case "seconds":
                    return mtbfSeconds;
                    break;
                case "minutes":
                    return mtbfSeconds / 60;
                    break;
                case "hours":
                    return mtbfSeconds / 60 / 60;
                    break;
                case "days":
                    return mtbfSeconds / 60 / 60 / 24;
                    break;
                case "months":
                    return mtbfSeconds / 60 / 60 / 24 / 30;
                    break;
                case "years":
                    return mtbfSeconds / 60 / 60 / 24 / 365;
                    break;
                default:
                    return mtbfSeconds;
                    break;
            }

            return 1.0 / failureRate;
        }
        // Get the FlightData or FlightTime for the part
        public double GetFlightData()
        {
            return GetFlightDataForScope(GetScope());
        }
        public double GetFlightDataForScope(String scope)
        {
            LogFormatted_DebugOnly("GetFlightDataForScope: scope=" + scope);
            FlightDataBody dataBody = flightData.GetFlightData(scope);
            if (dataBody == null)
            {
                LogFormatted_DebugOnly("GetFlightDataForScope: Could not find data or this scope.  Returning 0");
                return 0;
            }
            else
            {
                LogFormatted_DebugOnly("GetFlightDataForScope: Returning " + dataBody.flightData);
                return dataBody.flightData;
            }
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
        public void SetFlightDataForScope(double data, String scope)
        {
        }
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
                LogFormatted_DebugOnly("ModifyFlightData: Skipping update - flightData is null");
                return 0;
            }

            FlightDataBody bodyData = flightData.GetFlightData(scope);
            if (bodyData == null)
            {
                if (!additive)
                    return 0;
                LogFormatted_DebugOnly("ModifyFlightData: Adding new entry");
                modifier = ApplyFlightDataMultiplier(modifier);
                flightData.AddFlightData(scope, modifier, 0);
                return modifier;
            }

            if (additive)
            {
                bodyData.flightData += modifier;
            }
            else
            {
                bodyData.flightData *= modifier;
            }
            bodyData.flightData = ApplyFlightDataMultiplier(bodyData.flightData);
            LogFormatted_DebugOnly("ModifyFlightData: Updating entry for scope " + scope + " with data " + bodyData.flightData + " and time " + bodyData.flightTime);
            flightData.AddFlightData(scope, bodyData.flightData, bodyData.flightTime);
            return bodyData.flightData;
        }
        public double GetEngineerDataBonus(double partEngineerBonus)
        {
            if (TestFlightManagerScenario.Instance == null)
                return 1;
            double globalFlightDataEngineerMultiplier = TestFlightManagerScenario.Instance.settings.flightDataEngineerMultiplier;

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
        internal double ApplyFlightDataMultiplier(double baseData)
        {
            if (TestFlightManagerScenario.Instance == null)
                return baseData;

            return baseData * TestFlightManagerScenario.Instance.settings.flightDataMultiplier;
        }
        public double ModifyFlightTimeForScope(double modifier, String scope, bool additive)
        {
            return 0;
        }
        // Cause a failure to occur, either a random failure or a specific one
        // If fallbackToRandom is true, then if the specified failure can't be found or can't be triggered, a random failure will be triggered instead
        // Returns the triggered failure module, or null if none
        public ITestFlightFailure TriggerFailure()
        {
            // Failure occurs.  Determine which failure module to trigger
            int totalWeight = 0;
            int currentWeight = 0;
            int chosenWeight = 0;
            List<ITestFlightFailure> failureModules;

            failureModules = new List<ITestFlightFailure>();
            foreach (PartModule pm in this.part.Modules)
            {
                ITestFlightFailure fm = pm as ITestFlightFailure;
                if (fm != null)
                    failureModules.Add(fm);
            }

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
            failureModuleName = failureModuleName.ToLower().Trim();

            foreach(PartModule pm in this.part.Modules)
            {
                if (pm.moduleName.ToLower().Trim() == failureModuleName)
                {
                    ITestFlightFailure fm = pm as ITestFlightFailure;
                    if (pm == null && fallbackToRandom)
                        return TriggerFailure();
                    else if (pm == null & !fallbackToRandom)
                        return null;
                    else
                    {
                        LogFormatted_DebugOnly("TestFlightCore: Triggering failure on " + pm.moduleName);
                        activeFailure = fm;
                        failureAcknowledged = false;
                        fm.DoFailure();
                        return fm;
                    }
                }
            }

            if (fallbackToRandom)
                return TriggerFailure();

            return null;
        }


        public override void Update()
        {
            base.Update();

            if (TestFlightManagerScenario.Instance == null)
                return;
        }



        private float lastFailureCheck = 0f;
        private float lastPolling = 0.0f;
        private TestFlightData currentFlightData;
        private List<TestFlightData> initialFlightData;
        private double currentReliability = 0.0f;
        private List<ITestFlightFailure> failureModules = null;
        private ITestFlightFailure activeFailure = null;
        private bool failureAcknowledged = false;

        [KSPField(isPersistant = true)]
        public float failureCheckFrequency = 0f;
        [KSPField(isPersistant = true)]
        public float pollingInterval = 0f;

        public bool AttemptRepair()
        {
            if (activeFailure == null)
                return true;

            if (activeFailure.CanAttemptRepair())
            {
                bool isRepaired = activeFailure.AttemptRepair();
                if (isRepaired)
                {
                    activeFailure = null;
                    failureAcknowledged = false;
                    return true;
                }
            }
            return false;
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

        public override void OnAwake()
        {
            base.OnAwake();
        }

        public override void OnStart(StartState state)
        {
            if (baseFlightData == null)
                baseFlightData = new FlightDataConfig();
            if (flightData == null)
                flightData = new FlightDataConfig();

            if (failureModules == null)
            {
                failureModules = new List<ITestFlightFailure>();
            }
            failureModules.Clear();
            foreach(PartModule pm in this.part.Modules)
            {
                ITestFlightFailure failureModule = pm as ITestFlightFailure;
                if (failureModule != null)
                {
                    failureModules.Add(failureModule);
                }
            }
            base.OnStart(state);
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
                return "This repair has no requirements";

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

//        public virtual double GetCurrentReliability(double globalReliabilityModifier)
//        {
//            // Calculate reliability based on initial flight data, not current
//            double totalReliability = 0.0;
//            string scope;
//            IFlightDataRecorder dataRecorder = null;
//            foreach (PartModule pm in this.part.Modules)
//            {
//                IFlightDataRecorder fdr = pm as IFlightDataRecorder;
//                if (fdr != null)
//                {
//                    dataRecorder = fdr;
//                }
//            }
//            foreach (PartModule pm in this.part.Modules)
//            {
//                ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
//                if (reliabilityModule != null)
//                {
//                    scope = String.Format("{0}_{1}", dataRecorder.GetDataBody(), dataRecorder.GetDataSituation());
//                    TestFlightData flightData;
//                    if (initialFlightData == null)
//                    {
//                        flightData = new TestFlightData();
//                        flightData.scope = scope;
//                        flightData.flightData = 0.0f;
//                    }
//                    else
//                    {
//                        flightData = initialFlightData.Find(fd => fd.scope == scope);
//                    }
//                    totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(flightData);
//                }
//            }
//            currentReliability = totalReliability * globalReliabilityModifier;
//            return currentReliability;
//        }

        public void InitializeFlightData(List<TestFlightData> allFlightData, double globalReliabilityModifier)
        {
            baseFlightData = new FlightDataConfig();
            flightData = new FlightDataConfig();
            foreach (TestFlightData data in allFlightData)
            {
                baseFlightData.AddFlightData(data.scope, data.flightData, data.flightTime);
                flightData.AddFlightData(data.scope, data.flightData, data.flightTime);
            }
            return;
        }

//            initialFlightData = new List<TestFlightData>(allFlightData);
//            double totalReliability = 0.0;
//            string scope;
//            IFlightDataRecorder dataRecorder = null;
//            foreach (PartModule pm in this.part.Modules)
//            {
//                IFlightDataRecorder fdr = pm as IFlightDataRecorder;
//                if (fdr != null)
//                {
//                    dataRecorder = fdr;
//                    fdr.InitializeFlightData(allFlightData);
//                    currentFlightData = fdr.GetCurrentFlightData();
//                }
//            }
//            // Calculate reliability based on initial flight data, not current
//            foreach (PartModule pm in this.part.Modules)
//            {
//                ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
//                if (reliabilityModule != null)
//                {
//                    scope = String.Format("{0}_{1}", dataRecorder.GetDataBody(), dataRecorder.GetDataSituation());
//                    TestFlightData tfd;
//                    if (initialFlightData == null)
//                    {
//                        tfd = new TestFlightData();
//                        tfd.scope = scope;
//                        tfd.flightData = 0.0f;
//                    }
//                    else
//                    {
//                        tfd = initialFlightData.Find(fd => fd.scope == scope);
//                    }
//                    totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(tfd);
//                }
//            }
//            currentReliability = totalReliability * globalReliabilityModifier;
//        }


//        public virtual void DoFlightUpdate(double missionStartTime, double flightDataMultiplier, double flightDataEngineerMultiplier, double globalReliabilityModifier)
//        {
//            // Check to see if its time to poll
//            IFlightDataRecorder dataRecorder = null;
//            string scope;
//            float currentMet = (float)(Planetarium.GetUniversalTime() - missionStartTime);
//            if (currentMet > (lastPolling + pollingInterval) && currentMet > 10)
//            {
//                // Poll all compatible modules in this order:
//                // 1) FlightDataRecorder
//                // 2) TestFlightReliability
//                // 2A) Determine final computed reliability
//                // 3) Determine if failure poll should be done
//                // 3A) If yes, roll for failure check
//                // 3B) If failure occurs, roll to determine which failure module
//                foreach (PartModule pm in this.part.Modules)
//                {
//                    IFlightDataRecorder fdr = pm as IFlightDataRecorder;
//                    if (fdr != null)
//                    {
//                        dataRecorder = fdr;
//                        fdr.DoFlightUpdate(missionStartTime, flightDataMultiplier, flightDataEngineerMultiplier);
//                        currentFlightData = fdr.GetCurrentFlightData();
//                        break;
//                    }
//                }
//                // Calculate reliability based on initial flight data, not current
//                double totalReliability = 0.0;
//                foreach (PartModule pm in this.part.Modules)
//                {
//                    ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
//                    if (reliabilityModule != null)
//                    {
//                        scope = String.Format("{0}_{1}", dataRecorder.GetDataBody(), dataRecorder.GetDataSituation());
//                        TestFlightData flightData;
//                        if (initialFlightData == null)
//                        {
//                            flightData = new TestFlightData();
//                            flightData.scope = scope;
//                            flightData.flightData = 0.0f;
//                        }
//                        else
//                        {
//                            flightData = initialFlightData.Find(fd => fd.scope == scope);
//                        }
//                        totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(flightData);
//                    }
//                }
//                currentReliability = totalReliability * globalReliabilityModifier;
//                lastPolling = currentMet;
//            }
//        }
//
//        public virtual bool DoFailureCheck(double missionStartTime, double globalReliabilityModifier)
//        {
//            string scope;
//            IFlightDataRecorder dataRecorder = null;
//            float currentMet = (float)(Planetarium.GetUniversalTime() - missionStartTime);
//            if ( currentMet > (lastFailureCheck + failureCheckFrequency) && activeFailure == null && currentMet > 10)
//            {
//                lastFailureCheck = currentMet;
//                // Calculate reliability based on initial flight data, not current
//                double totalReliability = 0.0;
//                foreach (PartModule pm in this.part.Modules)
//                {
//                    IFlightDataRecorder fdr = pm as IFlightDataRecorder;
//                    if (fdr != null)
//                    {
//                        dataRecorder = fdr;
//                        break;
//                    }
//                }
//                foreach (PartModule pm in this.part.Modules)
//                {
//                    ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
//                    if (reliabilityModule != null)
//                    {
//                        scope = String.Format("{0}_{1}", dataRecorder.GetDataBody(), dataRecorder.GetDataSituation());
//                        TestFlightData flightData;
//                        if (initialFlightData == null)
//                        {
//                            flightData = new TestFlightData();
//                            flightData.scope = scope;
//                            flightData.flightData = 0.0f;
//                        }
//                        else
//                            flightData = initialFlightData.Find(fd => fd.scope == scope);
//                        totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(flightData);
//                    }
//                }
//                currentReliability = totalReliability * globalReliabilityModifier;
//                // Roll for failure
//                float roll = UnityEngine.Random.Range(0.0f,100.0f);
//                if (roll > currentReliability)
//                {
//                    // Failure occurs.  Determine which failure module to trigger
//                    int totalWeight = 0;
//                    int currentWeight = 0;
//                    int chosenWeight = 0;
//                    foreach(ITestFlightFailure fm in failureModules)
//                    {
//                        totalWeight += fm.GetFailureDetails().weight;
//                    }
//                    chosenWeight = UnityEngine.Random.Range(1,totalWeight);
//                    foreach(ITestFlightFailure fm in failureModules)
//                    {
//                        currentWeight += fm.GetFailureDetails().weight;
//                        if (currentWeight >= chosenWeight)
//                        {
//                            // Trigger this module's failure
//                            LogFormatted_DebugOnly("TestFlightCore: Triggering failure on " + fm);
//                            activeFailure = fm;
//                            failureAcknowledged = false;
//                            fm.DoFailure();
//                            return true;
//                        }
//                    }
//                }
//            }
//            return false;
//        }
    }
}


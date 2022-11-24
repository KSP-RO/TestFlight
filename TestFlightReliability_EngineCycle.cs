using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using TestFlightAPI;
using TestFlightCore;

namespace TestFlight
{
    public class TestFlightReliability_EngineCycle : TestFlightReliabilityBase
    {
        private EngineModuleWrapper engine;

        /// <summary>
        /// failure rate modifier applied to cumulative lifecycle
        /// </summary>
        [KSPField]
        public FloatCurve cycle;

        /// <summary>
        /// failure rate modified applied to current continuous burn
        /// </summary>
        [KSPField]
        public FloatCurve continuousCycle;

        /// <summary>
        /// maximum rated cumulative run time of the engine over the entire lifecycle
        /// </summary>
        [KSPField(guiName = "Rated Burn Time", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActiveEditor = true, guiUnits = "s")]
        public float ratedBurnTime = 0f;
        
        /// <summary>
        /// Multiplier to how much time is added to the engine's runTime based on set throttle position
        /// </summary>
        [KSPField]
        public FloatCurve thrustModifier = null;

        /// <summary>
        /// maximum rated continuous burn time between restarts
        /// </summary>
        [KSPField(guiName = "Rated Continuous Burn Time", groupName = "TestFlight", groupDisplayName = "TestFlight", guiUnits = "s")]
        public float ratedContinuousBurnTime;
        
        [KSPField]
        public string engineID = "";
        [KSPField]
        public string engineConfig = "";


        // Used for tracking burn states over time
        /// <summary>
        /// amount of seconds engine has been running over the entire lifecycle (cumulative)
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Total Run Time", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true, guiFormat = "N0")]
        public double engineOperatingTime = 0d;

        /// <summary>
        /// amount of second engine has been running since last start/restart
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Current Run Time", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true, guiFormat = "N0")]
        public double currentRunTime;

        [KSPField(isPersistant = true)]
        public double previousOperatingTime = 0d;

        /// <summary>
        /// Current MTBF
        /// </summary>
        [KSPField(guiName = "Mean Time Between Failures", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true)]
        public string currentMTBF;

        /// <summary>
        /// Current flight data
        /// </summary>
        [KSPField(guiName = "Flight Data", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true, guiActiveEditor = true, guiFormat = "F0")]
        public double flightData = 0d;

        private double maxFlightData = 0d;

        [KSPField(guiName = "Current Reliability", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActiveEditor = true, guiFormat = "P2")]
        public double currentReliability = 0f;

        [KSPField(guiName = "Reliability at Max Data", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActiveEditor = true, guiFormat = "P2")]
        public double maxReliability = 0f;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            engine = new EngineModuleWrapper();
            engine.InitWithEngine(this.part, engineID);

            Fields[nameof(currentRunTime)].guiUnits = $"s / {ratedContinuousBurnTime}s";
            Fields[nameof(engineOperatingTime)].guiUnits = $"s / {ratedBurnTime}s";

            // update engine PAW with flight data
            GetFlightData(ref flightData, ref maxFlightData);
            GetMTBF();            
        }

        public override double GetBaseFailureRate(float flightData)
        {
            Log("MFR based reliability module.  Returning 0 failure rate");
            return 0;
        }

        protected void UpdateCycle()
        {
            double currentTime = Planetarium.GetUniversalTime();
            double deltaTime = (currentTime - previousOperatingTime) / 1d;

            // update run times every second
            if (deltaTime >= 1d)
            {
                previousOperatingTime = currentTime;

                // Is our engine running?
                if (!core.IsPartOperating())
                {
                    // engine is not running
                    
                    // reset continuous run time
                    currentRunTime = 0;
                }
                else
                {
                    // engine is running
                    
                    // increase both continuous and cumulative run times
                    // add burn time based on time passed, optionally modified by the thrust
                    float actualThrustModifier = thrustModifier.Evaluate(engine.commandedThrust);
                    currentRunTime += deltaTime * actualThrustModifier; // continuous
                    engineOperatingTime += deltaTime * actualThrustModifier; // cumulative
                    

                    // calculate total failure rate modifier
                    float cumulativeModifier = cycle.Evaluate((float)engineOperatingTime);
                    float continuousModifier = continuousCycle.Evaluate((float)currentRunTime);
                    
                    core.SetTriggerMomentaryFailureModifier("EngineCycle", cumulativeModifier * continuousModifier, this);
                }
            }

            GetFlightData(ref flightData);
            GetMTBF();
        }

        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            if (core == null)
                return;

            UpdateCycle();

            // We intentionally do NOT call our base class OnUpdate() because that would kick off a second round of 
            // failure checks which is already handled by the main Reliability module that should 
            // already be on the part (This PartModule should be added in addition to the normal reliability module)
            // base.OnUpdate();
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }
        
        public override void SetActiveConfig(string alias)
        {
            base.SetActiveConfig(alias);
            
            if (currentConfig == null) return;

            // update current values with those from the current config node
            cycle = new FloatCurve();
            if (currentConfig.HasNode("cycle"))
            {
                cycle.Load(currentConfig.GetNode("cycle"));
            }
            else
            {
                cycle.Add(0f,1f);
            }

            continuousCycle = new FloatCurve();
            if (currentConfig.HasNode("continuousCycle"))
            {
                continuousCycle.Load(currentConfig.GetNode("continuousCycle"));
            }
            else
            {
                continuousCycle.Add(0f, 1f);
            }
            currentConfig.TryGetValue("ratedBurnTime", ref ratedBurnTime);
            currentConfig.TryGetValue("engineID", ref engineID);
            currentConfig.TryGetValue("engineConfig", ref engineConfig);
            if (currentConfig.HasValue("ratedContinuousBurnTime"))
            {
                currentConfig.TryGetValue("ratedContinuousBurnTime", ref ratedContinuousBurnTime);
            }
            else
            {
                ratedContinuousBurnTime = ratedBurnTime;
            }

            GetFlightData(ref flightData, ref maxFlightData);
            GetReliability(ratedBurnTime, ref currentReliability, ref maxReliability);

            // add display in minutes and seconds for longer burn times
            if (ratedBurnTime >= 60)
                Fields[nameof(ratedBurnTime)].guiUnits = $"s ({TestFlightUtil.FormatTime(ratedBurnTime, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false)})";
            else
                Fields[nameof(ratedBurnTime)].guiUnits = $"s";
        }

        public override string GetModuleInfo(string configuration, float reliabilityAtTime)
        {
            foreach (var configNode in configs)
            {
                if (!configNode.HasValue("configuration"))
                    continue;

                var nodeConfiguration = configNode.GetValue("configuration");

                if (string.Equals(nodeConfiguration, configuration, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (configNode.HasNode("cycle"))
                    {
                        var nodeCycle = new FloatCurve();
                        nodeCycle.Load(configNode.GetNode("cycle"));

                        float nodeBurnTime = 0f;
                        configNode.TryGetValue("ratedBurnTime", ref nodeBurnTime);

                        float burnThrough = nodeCycle.Evaluate(nodeCycle.maxTime);
                        return String.Format("  Rated Burn Time: <color=#b1cc00ff>{0:F0} s</color>\n  <color=#ec423fff>{1:F0}%</color> failure at <color=#b1cc00ff>{2:F0} s</color>", nodeBurnTime, burnThrough, nodeCycle.maxTime);
                    }
                }
            }

            return base.GetModuleInfo(configuration, reliabilityAtTime);
        }

        public override float GetRatedTime(string configuration, RatingScope ratingScope)
        {
            foreach (var configNode in configs)
            {
                if (!configNode.HasValue("configuration"))
                    continue;

                var nodeConfiguration = configNode.GetValue("configuration");

                if (string.Equals(nodeConfiguration, configuration, StringComparison.InvariantCultureIgnoreCase))
                {
                    switch (ratingScope)
                    {
                        case RatingScope.Cumulative:
                            if (configNode.HasValue("ratedBurnTime"))
                            {
                                float nodeBurnTime = 0f;
                                configNode.TryGetValue("ratedBurnTime", ref nodeBurnTime);
                                return nodeBurnTime;
                            }
                            break;
                        
                        case RatingScope.Continuous:
                            if (configNode.HasValue("ratedContinuousBurnTime"))
                            {
                                float nodeBurnTime = 0f;
                                configNode.TryGetValue("ratedContinuousBurnTime", ref nodeBurnTime);
                                return nodeBurnTime;
                            }
                            break;
                    }
                }
            }

            return base.GetRatedTime(configuration, ratingScope);
        }
        
        

        public override float GetRatedTime(RatingScope ratingScope)
        {
            switch (ratingScope)
            {
                case RatingScope.Cumulative:
                    return ratedBurnTime;
                case RatingScope.Continuous:
                    return ratedContinuousBurnTime;
                default:
                    return 0f;
            }
        }

        public override float GetScopedRunTime(RatingScope ratingScope)
        {
            switch (ratingScope)
            {
                case RatingScope.Cumulative:
                    return (float)engineOperatingTime;
                case RatingScope.Continuous:
                    return (float)currentRunTime;
                default:
                    return 0f;
            }
        }

        public override void OnAwake()
        {
            base.OnAwake();
            if (!string.IsNullOrEmpty(configNodeData))
            {
                var node = ConfigNode.Parse(configNodeData);
                OnLoad(node);
            }
            if (cycle == null)
            {
                cycle = new FloatCurve();
                cycle.Add(0f, 1f);
            }
            if (continuousCycle == null)
            {
                continuousCycle = new FloatCurve();
                continuousCycle.Add(0f, 1f);
            }

            if (thrustModifier == null)
            {
                thrustModifier = new FloatCurve();
                thrustModifier.Add(0f, 1f);                
            }

        }

        public override List<string> GetTestFlightInfo(float reliabilityAtTime)
        {
            List<string> infoStrings = new List<string>();

            infoStrings.Add("<b>Engine Cycle</b>");
            if (ratedContinuousBurnTime < ratedBurnTime)
            {
                infoStrings.Add($"<b>Continuous Run Time</b>: {TestFlightUtil.FormatTime(ratedContinuousBurnTime, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, true)}");
                infoStrings.Add($"<b>Cumulative Run Time</b>: {TestFlightUtil.FormatTime(ratedBurnTime, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, true)}");
            }
            else if (ratedContinuousBurnTime > ratedBurnTime)
            {
                infoStrings.Add($"<b>Rated Run Time</b>: {TestFlightUtil.FormatTime(ratedBurnTime, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, true)}");
                infoStrings.Add($"<b>Safe Run Time</b>: {TestFlightUtil.FormatTime(ratedContinuousBurnTime, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, true)}");
            }
            else
            {
                infoStrings.Add($"<b>Rated Run Time</b>: {TestFlightUtil.FormatTime(ratedBurnTime, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, true)}");
            }
            return infoStrings;
        }

        /// <summary>
        /// Gets MTBF for use in engine PAW.
        /// </summary>
        private void GetMTBF()
        {
            if (core == null)
            {
                Log("Core is null");
                return;
            }

            FloatCurve curve = core.GetBaseReliabilityCurve();

            if (curve == null)
            {
                Log("Curve is null");
                return;
            }
            
            MomentaryFailureRate momentaryFailureRate = core.GetWorstMomentaryFailureRate();
            if (!momentaryFailureRate.valid) return;

            currentMTBF = core.FailureRateToMTBFString(momentaryFailureRate.failureRate, TestFlightUtil.MTBFUnits.SECONDS, true, 60);
        }


        /// <summary>
        /// Gets flight data for use in engine PAW. Does NOT modify flight data.
        /// </summary>
        /// <param name="flightData"></param>
        /// <param name="maxFlightData"></param>
        private void GetFlightData(ref double flightData, ref double maxFlightData)
        {
            if (core == null)
            {
                Log("Core is null");
                return;
            }

            maxFlightData = core.GetMaximumData();
            Fields[nameof(flightData)].guiUnits = $" du / {maxFlightData:F0} du";

            flightData = Mathf.Max(0f, core.GetFlightData());
            float fd_test1 = TestFlightManagerScenario.Instance.GetFlightDataForPartName(core.Alias);

            if (flightData > maxFlightData)
                flightData = maxFlightData;
        }

        /// <summary>
        /// Gets flight data for use in engine PAW. Does NOT modify flight data.
        /// </summary>
        /// <param name="flightData"></param>
        private void GetFlightData(ref double flightData)
        {
            if (core == null)
            {
                Log("Core is null");
                return;
            }

            flightData = Mathf.Max(0f, core.GetFlightData());

            if (flightData > maxFlightData)
                flightData = maxFlightData;
        }

        /// <summary>
        /// Gets current and max data realibility for use in engine PAW
        /// </summary>
        /// <param name="reliabilityAtTime"></param>
        private void GetReliability(float reliabilityAtTime, ref double currentReliability, ref double maxReliability)
        {
            if (core == null)
            {
                Log("Core is null");
                return;
            }

            FloatCurve curve = core.GetBaseReliabilityCurve();

            if (curve == null)
            {
                Log("Curve is null");
                return;
            }

            float flightData = Mathf.Max(0f, core.GetFlightData());
            double currentFailRate = curve.Evaluate(flightData);
            double maxFailRate = curve.Evaluate(curve.maxTime);

            currentReliability = TestFlightUtil.FailureRateToReliability(currentFailRate, reliabilityAtTime);
            maxReliability = TestFlightUtil.FailureRateToReliability(maxFailRate, reliabilityAtTime);

            // this also updates MTBF during initial flight scene load
            currentMTBF = core.FailureRateToMTBFString(currentFailRate, TestFlightUtil.MTBFUnits.SECONDS, true, 60);            
            
            string maxMTBF = core.FailureRateToMTBFString(maxFailRate, TestFlightUtil.MTBFUnits.SECONDS, true, 60);

            Fields[nameof(currentReliability)].guiUnits = $", MTBF: {currentMTBF}";
            Fields[nameof(maxReliability)].guiUnits = $", MTBF: {maxMTBF}";
        }
    }
}


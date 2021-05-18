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
        [KSPField]
        public float ratedBurnTime = 0f;

        /// <summary>
        /// maximum rated continuous burn time between restarts
        /// </summary>
        [KSPField]
        public float ratedContinuousBurnTime;
        
        [KSPField]
        public string engineID = "";
        [KSPField]
        public string engineConfig = "";


        // Used for tracking burn states over time
        /// <summary>
        /// amount of seconds engine has been running over the entire lifecycle (cumulative)
        /// </summary>
        [KSPField(isPersistant = true)]
        public double engineOperatingTime = 0d;

        /// <summary>
        /// amount of second engine has been running since last start/restart
        /// </summary>
        [KSPField(isPersistant = true)]
        public double currentRunTime;

        [KSPField(isPersistant = true)]
        public double previousOperatingTime = 0d;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            engine = new EngineModuleWrapper();
            engine.InitWithEngine(this.part, engineID);
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
            if (deltaTime >= 1d)
            {
                previousOperatingTime = currentTime;
                // Is our engine running?
                if (!core.IsPartOperating())
                {
                    // engine is not running
                    
                    // reset continuous run time
                    currentTime = 0;
                }
                else
                {
                    // engine is running
                    
                    // increase both continuous and cumulative run times
                    currentTime += deltaTime; // continuous
                    engineOperatingTime += deltaTime; // cumulative
                    

                    // calculate totaly failure rate modifier
                    float cumulativeModifier = cycle.Evaluate((float)engineOperatingTime);
                    float continuousModifier = continuousCycle.Evaluate((float)currentTime);
                    
                    core.SetTriggerMomentaryFailureModifier("EngineCycle", cumulativeModifier * continuousModifier, this);
                }
            }
        }

        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            if (core == null)
                return;

            UpdateCycle();

            // We intentionally do NOT call our base class OnUpdate() because that would kick off a second round of 
            // failure checks which is already handled by the main Reliabilty module that should 
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

        }

        public override List<string> GetTestFlightInfo(float reliabilityAtTime)
        {
            List<string> infoStrings = new List<string>();

            infoStrings.Add("<b>Engine Cycle</b>");
            infoStrings.Add($"<b>Maximum Continuous Run Time</b>: {TestFlightUtil.FormatTime(ratedContinuousBurnTime, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, true)}");
            infoStrings.Add($"<b>Maximum Cumulative Run Time</b>: {TestFlightUtil.FormatTime(ratedBurnTime, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, true)}");
            return infoStrings;
        }
    }
}


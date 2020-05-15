﻿using System;
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

        [KSPField]
        public FloatCurve cycle;
        [KSPField]
        public FloatCurve thrustModifier = null;
        [KSPField]
        public float idleDecayRate = 0f;
        [KSPField]
        public float ratedBurnTime = 0f;
        [KSPField]
        public string engineID = "";
        [KSPField]
        public string engineConfig = "";


        [KSPField(isPersistant = true)]
        public double engineOperatingTime = 0d;
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
//            Log(String.Format("TestFlightReliability_EngineCycle: previous time: {0:F4}, current time: {1:F4}, delta time: {2:F4}", previousOperatingTime, currentTime, deltaTime));
            if (deltaTime >= 1d)
            {
                previousOperatingTime = currentTime;
                // Is our engine running?
                if (!core.IsPartOperating())
                {
                    // If not then we optionally cool down the engine, which decresses the burn time used
                    engineOperatingTime = Math.Max(engineOperatingTime - (idleDecayRate * deltaTime), 0d);
                }
                else
                {
                    // If so then we add burn time based on time passed, optionally modified by the thrust
                    float actualThrustModifier = thrustModifier.Evaluate(engine.finalThrust);
                    if (verboseDebugging)
                    {
                        Log(String.Format("TestFlightReliability_EngineCycle: Engine Thrust {0:F4}", engine.finalThrust));
                        Log(String.Format("TestFlightReliability_EngineCycle: delta time: {0:F4}, operating time :{1:F4}, thrustModifier: {2:F4}", deltaTime, engineOperatingTime, actualThrustModifier));
                    }                    engineOperatingTime = engineOperatingTime + (deltaTime * actualThrustModifier);

                    // Check for failure
                    float minValue, maxValue = -1f;
                    cycle.FindMinMaxValue(out minValue, out maxValue);
                    float penalty = cycle.Evaluate((float)engineOperatingTime);
                    if (verboseDebugging)
                    {
                        Log(String.Format("TestFlightReliability_EngineCycle: Cycle Curve, Min Value {0:F2}:{1:F6}, Max Value {2:F2}:{3:F6}", cycle.minTime, minValue, cycle.maxTime, maxValue));
                        Log(String.Format("TestFlightReliability_EngineCycle: Applying modifier {0:F4} at cycle time {1:F4}", penalty, engineOperatingTime));
                    }   
                    core.SetTriggerMomentaryFailureModifier("EngineCycle", penalty, this);
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
            thrustModifier = new FloatCurve();
            if (currentConfig.HasNode("thrustModifier"))
            {
                thrustModifier.Load(currentConfig.GetNode("thrustModifier"));
            }
            else
            {
                thrustModifier.Add(0f,1f);
            }
            currentConfig.TryGetValue("idleDecayRate", ref idleDecayRate);
            currentConfig.TryGetValue("ratedBurnTime", ref ratedBurnTime);
            currentConfig.TryGetValue("engineID", ref engineID);
            currentConfig.TryGetValue("engineConfig", ref engineConfig);
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

        public override float GetRatedBurnTime(string configuration)
        {
            foreach (var configNode in configs)
            {
                if (!configNode.HasValue("configuration"))
                    continue;

                var nodeConfiguration = configNode.GetValue("configuration");

                if (string.Equals(nodeConfiguration, configuration, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (configNode.HasValue("ratedBurnTime"))
                    {
                        float nodeBurnTime = 0f;
                        configNode.TryGetValue("ratedBurnTime", ref nodeBurnTime);
                        return nodeBurnTime;
                    }
                }
            }

            return base.GetRatedBurnTime(configuration);
        }

        public override float GetRatedBurnTime()
        {
            return ratedBurnTime;
        }

        public override float GetCurrentBurnTime()
        {
            return (float)engineOperatingTime;
        }

        public override void OnAwake()
        {
            base.OnAwake();
            if (!string.IsNullOrEmpty(configNodeData))
            {
                var node = ConfigNode.Parse(configNodeData);
                OnLoad(node);
            }
            if (thrustModifier == null)
            {
                thrustModifier = new FloatCurve();
                thrustModifier.Add(0f, 1f);
            }
            if (cycle == null)
            {
                cycle = new FloatCurve();
                cycle.Add(0f, 1f);
            }
        }

        public override List<string> GetTestFlightInfo(float reliabilityAtTime)
        {
            List<string> infoStrings = new List<string>();

            infoStrings.Add("<b>Engine Cycle</b>");
            infoStrings.Add(String.Format("<b>Rated Burn Time</b>: {0}", TestFlightUtil.FormatTime(ratedBurnTime, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, true)));
            if (idleDecayRate > 0)
                infoStrings.Add(String.Format("Cooling. Burn time decays {0:F2} per sec second engine is off", idleDecayRate));
            float minThrust, maxThrust;
            thrustModifier.FindMinMaxValue(out minThrust, out maxThrust);
            if (minThrust != maxThrust)
            {
                infoStrings.Add(String.Format("Engine thrust affects burn time"));
                infoStrings.Add(String.Format("<b>Min Thrust/b> {0:F2}kn, {1:F2}x", thrustModifier.minTime, minThrust));
                infoStrings.Add(String.Format("<b>Max Thrust/b> {0:F2}kn, {1:F2}x", thrustModifier.maxTime, maxThrust));
            }
            return infoStrings;
        }
    }
}


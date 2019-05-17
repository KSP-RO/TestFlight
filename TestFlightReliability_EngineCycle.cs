using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using TestFlightAPI;

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
                    Log(String.Format("TestFlightReliability_EngineCycle: Engine Thrust {0:F4}", engine.finalThrust));
                    float actualThrustModifier = thrustModifier.Evaluate(engine.finalThrust);
                    Log(String.Format("TestFlightReliability_EngineCycle: delta time: {0:F4}, operating time :{1:F4}, thrustModifier: {2:F4}", deltaTime, engineOperatingTime, actualThrustModifier));
                    engineOperatingTime = engineOperatingTime + (deltaTime * actualThrustModifier);

                    // Check for failure
                    float minValue, maxValue = -1f;
                    cycle.FindMinMaxValue(out minValue, out maxValue);
                    Log(String.Format("TestFlightReliability_EngineCycle: Cycle Curve, Min Value {0:F2}:{1:F6}, Max Value {2:F2}:{3:F6}", cycle.minTime, minValue, cycle.maxTime, maxValue));
                    float penalty = cycle.Evaluate((float)engineOperatingTime);
                    Log(String.Format("TestFlightReliability_EngineCycle: Applying modifier {0:F4} at cycle time {1:F4}", penalty, engineOperatingTime));
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

        public override string GetModuleInfo()
        {
            if (cycle != null)
            {
                float burnThrough = cycle.Evaluate(cycle.maxTime);
                return String.Format("Rated Burn Time: <color=#859900ff>{0:F2}</color> seconds\nBurn through penalty is <color=#dc322fff>{1:F2}</color> at {2:F2} seconds", ratedBurnTime, burnThrough, cycle.maxTime);
            }
            return base.GetModuleInfo();
        }

        public override void OnAwake()
        {
            base.OnAwake();
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

        public override List<string> GetTestFlightInfo()
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


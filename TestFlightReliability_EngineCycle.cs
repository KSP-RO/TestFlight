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


        [KSPField(isPersistant = true)]
        public double engineOperatingTime = 0f;
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
            return 0;
        }

        protected void UpdateCycle()
        {
            double currentTime = Planetarium.GetUniversalTime();
            double deltaTime = (currentTime - previousOperatingTime) / 1d;
            if (deltaTime >= 1d)
            {
                // Is our engine running?
                if (!core.IsPartOperating())
                {
                    // If not then we optionally cool down the engine, which decresses the burn time used
                    engineOperatingTime = engineOperatingTime - (idleDecayRate * deltaTime);
                }
                else
                {
                    // If so then we add burn time based on time passed, optionally modified by the thrust
                    float actualThrustModifier = thrustModifier.Evaluate(engine.finalThrust);
                    engineOperatingTime = engineOperatingTime + (deltaTime * actualThrustModifier);

                    // Check for failure
                    float penalty = cycle.Evaluate((float)engineOperatingTime);
                    Log(String.Format("TestFlightFailure_EngineCycle: Applying modifier {0:F4} at cycle time {1:F4}", penalty, engineOperatingTime));
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
        public override string GetInfo()
        {
            if (cycle != null)
            {
                float burnThrough = cycle.Evaluate(cycle.maxTime);
                return String.Format("Engine Cycle Information for: \n{3}: Rated Burn Time: <color=#859900ff>{0:F2}</color> seconds\nBurn through penalty is <color=#dc322fff>{1:F2}</color> at {2:F2} seconds", ratedBurnTime, burnThrough, cycle.maxTime, Configuration);
            }
            return base.GetInfo();
        }

        public override void OnAwake()
        {
            base.OnAwake();
            thrustModifier = new FloatCurve();
            thrustModifier.Add(0f, 1f);
            cycle = new FloatCurve();
            cycle.Add(0f, 1f);
        }
    }
}


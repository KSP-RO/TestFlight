using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightReliability_EngineCycle : TestFlightReliabilityBase
    {
        public FloatCurve cycle;
        private bool engineOperating = false;
        private float engineStartTime = 0;
        [KSPField(isPersistant = true)]
        public float ratedBurnTime = 0f;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Part prefab = this.part.partInfo.partPrefab;
            foreach (PartModule pm in prefab.Modules)
            {
                TestFlightReliability_EngineCycle modulePrefab = pm as TestFlightReliability_EngineCycle;
                if (modulePrefab != null && modulePrefab.Configuration == configuration)
                {
                    cycle = modulePrefab.cycle;
                }
            }
        }
        public override double GetBaseFailureRate(float flightData)
        {
            return 0;
        }
        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            if (core == null)
                return;
            if (cycle == null)
                return;

            if (!engineOperating)
            {
                if (core.IsPartOperating())
                {
                    engineStartTime = (float)Planetarium.GetUniversalTime();
                    engineOperating = true;
                }
            }
            // We apply a momentary modifier based on engine cycle
            // This makes the failure rate for the engine higher in the first few seconds after it starts
            if (engineOperating)
            {
                if (!core.IsPartOperating())
                {
                    engineOperating = false;
                    return;
                }
                float engineOperatingTime = (float)Planetarium.GetUniversalTime() - engineStartTime;
                float penalty = cycle.Evaluate((float)engineOperatingTime);
//                Debug.Log(String.Format("TestFlightFailure_EngineCycle: Applying modifier {0:F4} at cycle time {1:F4}", penalty, engineOperatingTime));
                core.SetTriggerMomentaryFailureModifier("EngineCycle", penalty, this);
            }
            // We intentionally do NOT call our base class OnUpdate() because that would kick off a second round of 
            // failure checks which is already handled by the main Reliabilty module that should 
            // already be on the part (This PartModule should be added in addition to the normal reliability module)
            // base.OnUpdate();
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasNode("cycle"))
            {
                cycle = new FloatCurve();
                cycle.Load(node.GetNode("cycle"));
            }
            else
                cycle = null;
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
    }
}


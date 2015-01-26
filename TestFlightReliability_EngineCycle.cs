using System;
using System.Linq;
using System.Collections.Generic;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightReliability_EngineCycle : TestFlightReliabilityBase
    {
        [KSPField(isPersistant = true)]
        public FloatCurve cycle;

        private bool engineOperating = false;
        private double engineStartTime = 0;
//        private ITestFlightCore core = null;
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
        }
        public override void OnUpdate()
        {
            if (core == null)
                return;
            if (!engineOperating)
            {
                if (core.IsPartOperating())
                {
                    engineStartTime = Planetarium.GetUniversalTime();
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
                double engineOperatingTime = Planetarium.GetUniversalTime() - engineStartTime;
                float penalty = cycle.Evaluate((float)engineOperatingTime);
                core.SetTriggerMomentaryFailureModifier("", penalty, this);
            }
            // We intentionally do NOT call our base class OnUpdate() because that would kick off a second round of 
            // failure checks which is already handled by the main Reliabilty module that should 
            // already be on the part (This PartModule should be added in addition to the normal reliability module)
            // base.OnUpdate();
        }
    }
}


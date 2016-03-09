using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightReliability_DynamicPressure : TestFlightReliabilityBase
    {
        [KSPField]
        public FloatCurve reliabilityAtPressure = new FloatCurve();

        private double oldPenalty = TestFlightUtil.MIN_FAILURE_RATE;

        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            if (core == null)
                return;

            double newPenalty = reliabilityAtPressure.Evaluate((float)base.vessel.dynamicPressurekPa * 1000);
            newPenalty = Math.Min(newPenalty, TestFlightUtil.MIN_FAILURE_RATE);

            if (newPenalty != oldPenalty)
                core.SetTriggerMomentaryFailureModifier("DynamicPressure", newPenalty, this);
            oldPenalty = newPenalty;
        }

        public override double GetBaseFailureRate(float flightData)
        {
            return 0;
        }
    }
}

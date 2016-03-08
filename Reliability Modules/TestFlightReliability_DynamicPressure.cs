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
        [KSPField]
        public double min_failure_modifier = 0.000000000001d;

        private double oldPenalty = 1.0;
        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            if (core == null)
                return;

            double newPenalty = reliabilityAtPressure.Evaluate((float)base.vessel.dynamicPressurekPa * 1000);
            if (newPenalty < this.min_failure_modifier)
                newPenalty = this.min_failure_modifier;
            if (newPenalty != this.oldPenalty)
            {
                this.oldPenalty = newPenalty;
                core.SetTriggerMomentaryFailureModifier("DynamicPressure", newPenalty, this);
            }
        }
        public override double GetBaseFailureRate(float flightData)
        {
            return 0;
        }
    }
}

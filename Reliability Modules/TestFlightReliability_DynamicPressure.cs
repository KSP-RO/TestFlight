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

        private double oldPenalty = 1.0;
        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            if (core == null)
                return;

            double newPenalty = reliabilityAtPressure.Evaluate((float)base.vessel.dynamicPressurekPa * 1000);
            if (newPenalty < 1d) newPenalty = 1d;
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

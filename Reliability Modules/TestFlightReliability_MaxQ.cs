using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightReliability_MaxQ : TestFlightReliabilityBase
    {
        [KSPField]
        public int maxQ = int.MaxValue;
        [KSPField]
        public FloatCurve penalty = new FloatCurve();

        private double oldPenalty = 1.0;
        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            if (core == null)
                return;

            double newPenalty = 1.0;
            if (!base.part.ShieldedFromAirstream)
            {
                int value = (int)(base.vessel.dynamicPressurekPa * 1000) - this.maxQ;
                if (value > 0)
                {
                    newPenalty = (double)this.penalty.Evaluate(value);
                }
            }
            if (newPenalty != this.oldPenalty)
            {
                core.SetTriggerMomentaryFailureModifier("MaxQ", newPenalty, this);
            }
        }
        public override double GetBaseFailureRate(float flightData)
        {
            return 0;
        }
    }
}

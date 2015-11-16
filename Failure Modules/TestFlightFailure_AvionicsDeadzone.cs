using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_AvionicsDeadzone : TestFlightFailureBase_Avionics
    {
        private float deadStart;
        private float deadEnd;
        public override void DoFailure()
        {
            base.DoFailure();
            Random ran = new Random();
            float range = (float)ran.NextDouble() * 0.25f;
            float center = (float)ran.NextDouble() * 2 - 1;
            this.deadStart = Math.Max(center - range, -1f);
            this.deadEnd = Math.Min(center + range, 1f);
        }
        public override float Calculate(float value)
        {
            if (value > this.deadStart && value < this.deadEnd)
            {
                return 0;
            }
            return value;
        }
    }
}
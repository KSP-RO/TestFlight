using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_WheelBrake : TestFlightFailureBase_Wheel
    {
        public override void DoFailure()
        {
            base.DoFailure();
            base.wheelBrakes.Actions["BrakesAction"].active = false;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.wheelBrakes.Actions["BrakesAction"].active = true;
            return 0f;
        }
    }
}

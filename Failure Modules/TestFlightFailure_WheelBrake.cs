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
            base.module.Actions["BrakesAction"].active = false;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.module.Actions["BrakesAction"].active = true;
            return 0f;
        }
    }
}

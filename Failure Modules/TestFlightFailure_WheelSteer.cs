using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_WheelSteer : TestFlightFailureBase_Wheel
    {
        private bool state;
        public override void DoFailure()
        {
            base.DoFailure();
            this.state = base.wheelSteering.steeringEnabled;
            base.wheelSteering.steeringEnabled = false;
            base.wheelSteering.Events["LockSteering"].active = false;
            base.wheelSteering.Events["UnlockSteering"].active = false;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.wheelSteering.steeringEnabled = this.state;
            base.wheelSteering.Events["LockSteering"].active = !this.state;
            base.wheelSteering.Events["UnlockSteering"].active = this.state;
            return 0f;
        }
    }
}
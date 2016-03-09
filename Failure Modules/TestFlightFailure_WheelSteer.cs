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
            this.state = base.module.steeringLocked;
            base.module.steeringLocked = true;
            base.module.Events["LockSteering"].active = false;
            base.module.Events["UnlockSteering"].active = false;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.module.steeringLocked = this.state;
            base.module.Events["LockSteering"].active = !this.state;
            base.module.Events["UnlockSteering"].active = this.state;
            return 0f;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_WheelMotor : TestFlightFailureBase_Wheel
    {
        private bool state;
        public override void DoFailure()
        {
            base.DoFailure();
            this.state = base.module.motorEnabled;
            base.module.motorEnabled = false;
            base.module.Events["EnableMotor"].active = false;
            base.module.Events["DisableMotor"].active = false;

        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.module.motorEnabled = state;
            base.module.Events["EnableMotor"].active = state;
            base.module.Events["DisableMotor"].active = !state;
            return 0f;
        }
    }
}

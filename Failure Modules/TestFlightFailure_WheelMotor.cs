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
            this.state = base.wheelMotor.motorEnabled;
            base.wheelMotor.motorEnabled = false;
            base.wheelMotor.Events["EnableMotor"].active = false;
            base.wheelMotor.Events["DisableMotor"].active = false;

        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.wheelMotor.motorEnabled = state;
            base.wheelMotor.Events["EnableMotor"].active = state;
            base.wheelMotor.Events["DisableMotor"].active = !state;
            return 0f;
        }
    }
}

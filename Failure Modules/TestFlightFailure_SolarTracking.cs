using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_SolarTracking : TestFlightFailureBase_Solar
    {
        private float trackingSpeed = 0f;
        public override void DoFailure()
        {
            base.DoFailure();
            this.trackingSpeed = base.module.trackingSpeed;
            base.module.trackingSpeed = 0;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.module.trackingSpeed = this.trackingSpeed;
            return 0f;
        }
    }
}

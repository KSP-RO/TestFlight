using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_ScienceBroken : TestFlightFailureBase_Science
    {
        //private bool oldState;
        private int inter;
        private int exter;
        private string usage;

        public override void DoFailure()
        {
            base.DoFailure();
            this.inter = this.module.usageReqMaskInternal;
            this.exter = this.module.usageReqMaskExternal;
            this.usage = this.module.usageReqMessage;

            this.module.usageReqMaskInternal = -1;
            this.module.usageReqMaskExternal = -1;
            this.module.usageReqMessage = "Science experiment inoperable: " + base.failureTitle;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            this.module.usageReqMaskInternal = this.inter;
            this.module.usageReqMaskExternal = this.exter;
            this.module.usageReqMessage = this.usage;
            return 0f;
        }
    }
}

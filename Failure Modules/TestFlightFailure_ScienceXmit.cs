using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_ScienceXmit : TestFlightFailureBase_Science
    {
        [KSPField(isPersistant = true)]
        public float xmit = -1f;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (this.xmit == -1f)
            {
                this.xmit = base.module.xmitDataScalar;
            }
        }
        public override void DoFailure()
        {
            base.DoFailure();
            System.Random ran = new System.Random();
            base.module.xmitDataScalar = (float)ran.NextDouble() * this.xmit;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.module.xmitDataScalar = this.xmit;
            return 0;
        }
    }
}

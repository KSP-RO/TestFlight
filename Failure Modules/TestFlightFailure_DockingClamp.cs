using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight
{
    public class TestFlightFailure_DockingClamp : TestFlightFailureBase_Docking
    {
        public override void DoFailure()
        {
            base.DoFailure();
            if (base.module != null)
            {
                if (base.module.Events["Decouple"].active)
                {
                    base.module.Decouple();
                }
                else if (base.module.Events["Undock"].active)
                {
                    base.module.Undock();
                }
            }
        }
        public override bool CanAttemptRepair()
        {
            return false;
        }
        public override float DoRepair()
        {
            return -1f;
        }
    }
}
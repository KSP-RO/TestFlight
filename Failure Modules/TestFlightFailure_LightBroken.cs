using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_LightBroken : TestFlightFailureBase_Light
    {
        public override void DoFailure()
        {
            base.DoFailure();
            if (base.module != null)
            {
                if (this.module.isOn)
                {
                    this.module.LightsOff();
                }
                base.module.Events["LightsOff"].active = false;
                base.module.Events["LightsOn"].active = false;
            }
        }
        public override float DoRepair()
        {
            base.DoRepair();
            if (base.module != null)
            {
                this.module.Events["LightsOn"].active = true;
            }
            return 0f;
        }
    }
}
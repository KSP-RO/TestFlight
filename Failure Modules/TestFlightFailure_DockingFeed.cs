using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight
{
    public class TestFlightFailure_DockingFeed : TestFlightFailureBase_Docking
    {
        private bool state = false;
        public override void DoFailure()
        {
            base.DoFailure();
            this.state = base.module.Events["DisableXFeed"].active;
            base.module.Events["DisableXFeed"].active = false;
            base.module.Events["EnableXFeed"].active = false;
            base.part.fuelCrossFeed = false;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.module.Events["DisableXFeed"].active = this.state;
            base.module.Events["EnableXFeed"].active = !this.state;
            base.part.fuelCrossFeed = this.state;
            return 0f;
        }
    }
}
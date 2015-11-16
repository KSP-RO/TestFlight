using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_ReactionBroken : TestFlightFailureBase_ReactionWheel
    {
        private ModuleReactionWheel.WheelState state;
        public override void DoFailure()
        {
            base.DoFailure();
            this.state = base.module.wheelState;
            base.module.Events["OnToggle"].active = false;
            base.module.wheelState = ModuleReactionWheel.WheelState.Broken;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.module.Events["OnToggle"].active = true;
            base.module.wheelState = this.state;
            return 0f;
        }
    }
}
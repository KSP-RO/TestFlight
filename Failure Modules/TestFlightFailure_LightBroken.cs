using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_LightBroken : TestFlightFailureBase
    {
        private ModuleLight module;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.module = base.part.FindModuleImplementing<ModuleLight>();
        }
        public override void DoFailure()
        {
            base.DoFailure();
            if (this.module.isOn)
            {
                this.module.LightsOff();
            }
            this.module.Events["LightsOff"].active = false;
            this.module.Events["LightsOn"].active = false;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            this.module.Events["LightsOn"].active = true;
            return 0f;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailureBase_Wheel : TestFlightFailureBase
    {
        protected ModuleWheel module;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            this.module = base.part.FindModuleImplementing<ModuleWheel>();
        }
    }
}

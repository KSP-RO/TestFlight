using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailureBase_FARCtrlSrf : TestFlightFailureBase
    {
        protected PartModule module;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.module = base.part.Modules["FARControllableSurface"];
        }
    }
}
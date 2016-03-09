using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailureBase_Light : TestFlightFailureBase
    {
        protected ModuleLight module;

        public override void OnAwake()
        {
            base.OnAwake();
            this.module = base.part.FindModuleImplementing<ModuleLight>();
        }
    }
}

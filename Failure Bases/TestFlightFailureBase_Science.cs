using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailureBase_Science : TestFlightFailureBase
    {
        protected ModuleScienceExperiment module;
        public override void OnAwake()
        {
            base.OnAwake();
            this.module = base.part.FindModuleImplementing<ModuleScienceExperiment>();
        }
    }
}

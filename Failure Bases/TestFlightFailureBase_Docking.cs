using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailureBase_Docking : TestFlightFailureBase
    {
        protected ModuleDockingNode module;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.module = base.part.FindModuleImplementing<ModuleDockingNode>();
        }
    }
}
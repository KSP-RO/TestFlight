using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailureBase_ReactionWheel : TestFlightFailureBase
    {
        protected ModuleReactionWheel module;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.module = base.part.FindModuleImplementing<ModuleReactionWheel>();
        }
    }
}
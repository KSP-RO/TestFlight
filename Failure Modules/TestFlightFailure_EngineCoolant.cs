using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_EngineCoolant : TestFlightFailure_Engine
    {
        [KSPField]
        public float heatMultiplier = 3.0F;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            base.Startup();
        }

        public override void DoFailure()
        {
            base.DoFailure();
            foreach (EngineHandler engine in engines)
            {
                ModuleEngines module = (ModuleEngines)engine.engine.Module;
                if (module != null)
                {
                    module.heatProduction *= heatMultiplier;
                }
            }
        }
        public override float DoRepair()
        {
            base.DoRepair();
            foreach (EngineHandler engine in engines)
            {
                ModuleEngines module = (ModuleEngines)engine.engine.Module;
                if (module != null)
                {
                    module.heatProduction /= heatMultiplier;
                }
            }
            return 0f;
        }
    }
}

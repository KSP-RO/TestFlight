using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_ReducedMaxThrust : TestFlightFailure_Engine
    {
        [KSPField(isPersistant=true)]
        public float thrustReduction = 0.5f;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            base.Startup();
        }
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            base.DoFailure();
            // for each engine change its fuelFlow which will affect thrust
            foreach (EngineHandler engine in engines)
            {
                engine.engine.SetFuelFlowMult(thrustReduction);
            }
        }

        public override float DoRepair()
        {
            base.DoRepair();
            // for each engine restore its fuell flow back to 1.0
            foreach (EngineHandler engine in engines)
            {
                engine.engine.SetFuelFlowMult(1.0f);
            }
            return 0;
        }
    }
}


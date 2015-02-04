using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_ReducedMaxThrust : TestFlightFailureBase
    {
        private float maxThrust;
        private float minThrust;
        EngineModuleWrapper engine;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            engine = new EngineModuleWrapper(this.part);
        }
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            base.DoFailure();
            maxThrust = engine.maxThrust;
            minThrust = engine.minThrust;
            engine.maxThrust = maxThrust * 0.5f;
            if (maxThrust == minThrust || maxThrust * 0.5 < minThrust)
                engine.minThrust = maxThrust * 0.5f;
        }

        public override double DoRepair()
        {
            base.DoRepair();
            engine.maxThrust = maxThrust;
            engine.minThrust = minThrust;
            return 0;
        }
    }
}


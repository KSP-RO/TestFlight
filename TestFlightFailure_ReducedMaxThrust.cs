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
        [KSPField(isPersistant=true)]
        public float thrustReduction = 0.5f;

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
            engine.maxThrust = maxThrust * thrustReduction;
            if (maxThrust == minThrust || maxThrust * thrustReduction < minThrust)
                engine.minThrust = maxThrust * thrustReduction;
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


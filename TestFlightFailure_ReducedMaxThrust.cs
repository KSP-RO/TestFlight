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

        protected struct CachedEngineState
        {
            public float maxThrust;
            public float minThrust;
        }

        Dictionary<int, CachedEngineState> engineStates;

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
            if (engineStates == null)
                engineStates = new Dictionary<int, CachedEngineState>();
            engineStates.Clear();
            // for each engine, store its current min & max thrust then reduce it
            foreach (EngineHandler engine in engines)
            {
                int id = engine.engine.Module.GetInstanceID();
                CachedEngineState engineState = new CachedEngineState();
                engineState.minThrust = engine.engine.minThrust;
                engineState.maxThrust = engine.engine.maxThrust;
                engine.engine.maxThrust = engineState.maxThrust * thrustReduction;
                if (engineState.maxThrust == engineState.minThrust || engineState.maxThrust * thrustReduction < engineState.minThrust)
                    engine.engine.minThrust = engineState.maxThrust * thrustReduction;
                engineStates.Add(id, engineState);
            }
        }

        public override float DoRepair()
        {
            base.DoRepair();
            // for each engine restore its thrust values
            foreach (EngineHandler engine in engines)
            {
                int id = engine.engine.Module.GetInstanceID();
                if (engineStates.ContainsKey(id))
                {
                    engine.engine.minThrust = engineStates[id].minThrust;
                    engine.engine.maxThrust = engineStates[id].maxThrust;
                }
            }
            engineStates.Clear();
            return 0;
        }
    }
}


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
            public float maxFuelFlow;
            public float minFuelFlow;
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
                engineState.minFuelFlow = engine.engine.minFuelFlow;
                engineState.maxFuelFlow = engine.engine.maxFuelFlow;
                engine.engine.maxFuelFlow = engineState.maxFuelFlow * thrustReduction;
                if (engineState.maxFuelFlow == engineState.minFuelFlow || engineState.maxFuelFlow * thrustReduction < engineState.minFuelFlow)
                    engine.engine.minFuelFlow = engineState.maxFuelFlow * thrustReduction;
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
                    engine.engine.minFuelFlow = engineStates[id].minFuelFlow;
                    engine.engine.maxFuelFlow = engineStates[id].maxFuelFlow;
                }
            }
            engineStates.Clear();
            return 0;
        }
    }
}


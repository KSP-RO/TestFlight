using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_ShutdownEngine : TestFlightFailure_Engine
    {
        protected struct CachedEngineState
        {
            public bool allowShutdown;
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
            foreach (EngineHandler engine in engines)
            {
                int id = engine.engine.Module.GetInstanceID();
                CachedEngineState engineState = new CachedEngineState();
                engineState.allowShutdown = engine.engine.allowShutdown;
                engine.engine.Shutdown();
                engineStates.Add(id, engineState);
            }
        }

        public override float DoRepair()
        {
            base.DoRepair();
            // for each engine restore it
            foreach (EngineHandler engine in engines)
            {
                int id = engine.engine.Module.GetInstanceID();
                if (engineStates.ContainsKey(id))
                {
                    engine.engine.enabled = true;
                    engine.engine.allowShutdown = engineStates[id].allowShutdown;
                }
            }
            engineStates.Clear();
            return 0;
        }
    }
}


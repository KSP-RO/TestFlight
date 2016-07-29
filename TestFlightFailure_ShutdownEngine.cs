using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_ShutdownEngine : TestFlightFailure_Engine
    {
        protected struct CachedEngineState
        {
            public bool allowShutdown;
            public int numIgnitions;
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
                engineState.numIgnitions = engine.engine.GetIgnitionCount();
                engine.engine.Shutdown();
                engineStates.Add(id, engineState);
                engine.engine.RemoveIgnitions(-1);
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
                    engine.engine.SetIgnitionCount(engineStates[id].numIgnitions);
                }
            }
            engineStates.Clear();
            return 0;
        }
    }
}


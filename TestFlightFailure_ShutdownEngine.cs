using System.Collections.Generic;

namespace TestFlight
{
    public class TestFlightFailure_ShutdownEngine : TestFlightFailure_Engine
    {
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (Failed && IsMajor)
            {
                foreach (EngineHandler engine in engines)
                {
                    engine.engine.DisableRestart();
                    engine.engine.failed = true;
                    engine.engine.failMessage = failureTitle;
                }
            }
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
                int id = engines.IndexOf(engine);
                var engineState = new CachedEngineState(engine.engine);
                engine.engine.Shutdown();
                var numIgnitionsToRemove = 1;
                if (IsMajor)
                {
                    numIgnitionsToRemove = -1;

                    engine.engine.DisableRestart();
                    engine.engine.failed = true;
                    engine.engine.failMessage = failureTitle;
                }
                engineStates.Add(id, engineState);
                engine.engine.RemoveIgnitions(numIgnitionsToRemove);
            }
        }

        public override float DoRepair()
        {
            // for each engine restore it
            foreach (EngineHandler engine in engines)
            {
                int id = engines.IndexOf(engine);
                CachedEngineState engineState = null;
                if (engineStates?.TryGetValue(id, out engineState) ?? false)
                {
                    engine.engine.enabled = true;
                    engine.engine.Events["Activate"].active = true;
                    engine.engine.Events["Activate"].guiActive = true;
                    engine.engine.Events["Shutdown"].guiActive = true;
                    engine.engine.allowShutdown = engineState.allowShutdown;
                    engine.engine.allowRestart = engineState.allowRestart;
                    engine.engine.SetIgnitionCount(engineState.numIgnitions);
                    engine.engine.failed = false;
                    engine.engine.failMessage = "";
                }
            }
            engineStates.Clear();
            base.DoRepair();
            return 0;
        }
    }
}


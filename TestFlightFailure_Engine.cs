using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_Engine : TestFlightFailureBase
    {
        [KSPField]
        public string engineID = "all";

        protected class EngineHandler
        {
            public EngineModuleWrapper.EngineIgnitionState ignitionState;
            public EngineModuleWrapper engine;
            public bool failEngine;
        }

        protected List<EngineHandler> engines = new List<EngineHandler>();
        protected ITestFlightCore core = null;

        public new bool TestFlightEnabled
        {
            get
            {
                if (core != null && engines.Count > 0) 
                    return core.TestFlightEnabled;

                core = TestFlightUtil.GetCore(part, Configuration);
                if (core == null)
                    Log ("EngineBase: No TestFlight core found");
                else if (engines.Count == 0)
                    Log("EngineBase: No valid engines found");

                return false;
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            core = TestFlightUtil.GetCore(part, Configuration);
            Startup();
        }

        public virtual void Startup()
        {
            engines.Clear();
            if (!String.IsNullOrEmpty(engineID))
            {
                if (engineID.ToLower() == "all")
                {
                    List<ModuleEngines> engineMods = this.part.Modules.GetModules<ModuleEngines>();
                    foreach (ModuleEngines eng in engineMods)
                    {
                        string id = eng.engineID;
                        EngineModuleWrapper engine = new EngineModuleWrapper();
                        engine.InitWithEngine(this.part, id);
                        EngineHandler engineHandler = new EngineHandler();
                        engineHandler.engine = engine;
                        engineHandler.ignitionState = engine.IgnitionState;
                        engines.Add(engineHandler);
                    }
                }
                else if (engineID.Contains(","))
                {
                    string[] sEngineIndices = engineID.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string sEngineIndex in sEngineIndices)
                    {
                        EngineModuleWrapper engine = new EngineModuleWrapper();
                        engine.InitWithEngine(this.part, sEngineIndex);
                        EngineHandler engineHandler = new EngineHandler();
                        engineHandler.engine = engine;
                        engineHandler.ignitionState = engine.IgnitionState;
                        engines.Add(engineHandler);
                    }
                }
                else
                {
                    EngineModuleWrapper engine = new EngineModuleWrapper();
                    engine.InitWithEngine(this.part, engineID);
                    EngineHandler engineHandler = new EngineHandler();
                    engineHandler.engine = engine;
                    engineHandler.ignitionState = engine.IgnitionState;
                    engines.Add(engineHandler);
                }
            }
            else
            {
                EngineModuleWrapper engine = new EngineModuleWrapper();
                engine.Init(this.part);
                EngineHandler engineHandler = new EngineHandler();
                engineHandler.engine = engine;
                engineHandler.ignitionState = engine.IgnitionState;
                engines.Add(engineHandler);
            }
        }

        private void OnEnable()
        {
            StartCoroutine(UpdateEngineStatus());
        }

        private void OnDisable()
        {
            StopCoroutine(UpdateEngineStatus());
        }

        public override void SetActiveConfig(string alias)
        {
            base.SetActiveConfig(alias);
            
            if (currentConfig == null) return;

            // update current values with those from the current config node
            currentConfig.TryGetValue("engineID", ref engineID);
        }

        public IEnumerator UpdateEngineStatus()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();

                if (Failed && engines != null)
                {
                    foreach (var handler in engines)
                    {
                        handler.engine.Status = "Failed";
                        handler.engine.StatusL2 = failureTitle;
                    }
                }
            }
        }
    }
}


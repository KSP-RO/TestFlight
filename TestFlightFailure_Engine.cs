using System;
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
        protected Dictionary<int, CachedEngineState> engineStates;

        public new bool TestFlightEnabled
        {
            get
            {
                if (core != null && engines.Count > 0) 
                    return core.TestFlightEnabled;

                core = TestFlightUtil.GetCore(part, Configuration);
                if (core == null)
                    Log("EngineBase: No TestFlight core found");
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

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            ConfigNode[] cNodes = node.GetNodes("ENGINESTATE");
            if (cNodes.Length > 0 && engineStates == null)
                engineStates = new Dictionary<int, CachedEngineState>();

            foreach (ConfigNode cNode in cNodes)
            {
                int id = 0;
                if (!cNode.TryGetValue("id", ref id))
                {
                    Debug.LogError("[TestFlight] Invalid ENGINESTATE data in OnLoad:" + cNode);
                    continue;
                }
                var state = new CachedEngineState(cNode);
                engineStates.Add(id, state);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (engineStates?.Count > 0)
            {
                foreach (KeyValuePair<int, CachedEngineState> kvp in engineStates)
                {
                    var cn = new ConfigNode("ENGINESTATE");
                    kvp.Value.Save(cn);
                    cn.AddValue("id", kvp.Key);
                    node.AddNode(cn);
                }
            }
        }

        private void AddEngine(EngineModuleWrapper engine)
        {
            var engineHandler = new EngineHandler();
            engineHandler.engine = engine;
            engineHandler.ignitionState = engine.IgnitionState;
            engines.Add(engineHandler);
        }

        public virtual void Startup()
        {
            engines.Clear();
            if (!string.IsNullOrEmpty(engineID))
            {
                if (engineID.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    List<ModuleEngines> engineMods = this.part.Modules.GetModules<ModuleEngines>();
                    foreach (ModuleEngines eng in engineMods)
                    {
                        string id = eng.engineID;
                        EngineModuleWrapper engine = new EngineModuleWrapper();
                        engine.InitWithEngine(this.part, id);
                        AddEngine(engine);
                    }
                }
                else if (engineID.Contains(","))
                {
                    string[] sEngineIndices = engineID.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string sEngineIndex in sEngineIndices)
                    {
                        EngineModuleWrapper engine = new EngineModuleWrapper();
                        engine.InitWithEngine(this.part, sEngineIndex);
                        AddEngine(engine);
                    }
                }
                else
                {
                    EngineModuleWrapper engine = new EngineModuleWrapper();
                    engine.InitWithEngine(this.part, engineID);
                    AddEngine(engine);
                }
            }
            else
            {
                EngineModuleWrapper engine = new EngineModuleWrapper();
                engine.Init(this.part);
                AddEngine(engine);
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

        public override float DoRepair()
        {
            foreach (EngineHandler engine in engines)
            {
                engine.engine.ResetStatus();
            }
            return base.DoRepair();
        }

        private readonly WaitForFixedUpdate _wait = new WaitForFixedUpdate();
        public IEnumerator UpdateEngineStatus()
        {
            while (true)
            {
                yield return _wait;

                if (Failed)
                {
                    foreach (var handler in engines)
                    {
                        handler.engine.Status = "<color=orange>Failed</color>";
                        handler.engine.StatusL2 = $"<color=orange>{failureTitle}</color>";
                    }
                }
            }
        }
    }
}


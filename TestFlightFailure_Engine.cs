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
        [KSPField(isPersistant=true)]
        public string engineID = "";

        protected class EngineHandler
        {
            public EngineModuleWrapper.EngineIgnitionState ignitionState;
            public EngineModuleWrapper engine;
            public bool failEngine;
        }

        protected List<EngineHandler> engines = null;

        public new bool TestFlightEnabled
        {
            get
            {
                // Make sure we have valid engines
                if (engines == null)
                {
                    Log("EngineBase: No valid engines found");
                    return false;
                }
                return TestFlightUtil.EvaluateQuery(Configuration, this.part);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            StartCoroutine("Attach");
        }

        IEnumerator Attach()
        {
            while (this.part == null || this.part.partInfo == null || this.part.partInfo.partPrefab == null || this.part.Modules == null)
                yield return null;

            Startup();
        }

        public virtual void Startup()
        {
            engines = new List<EngineHandler>();
            if (!String.IsNullOrEmpty(engineID))
            {
                if (engineID.ToLower() == "all")
                {
                    List<ModuleEnginesFX> enginesFX = this.part.Modules.OfType<ModuleEnginesFX>().ToList();
                    foreach (ModuleEnginesFX fx in enginesFX)
                    {
                        string id = fx.engineID;
                        EngineModuleWrapper engine = new EngineModuleWrapper(this.part, id);
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
                        EngineModuleWrapper engine = new EngineModuleWrapper(this.part, sEngineIndex);
                        EngineHandler engineHandler = new EngineHandler();
                        engineHandler.engine = engine;
                        engineHandler.ignitionState = engine.IgnitionState;
                        engines.Add(engineHandler);
                    }
                }
                else
                {
                    EngineModuleWrapper engine = new EngineModuleWrapper(this.part, engineID);
                    EngineHandler engineHandler = new EngineHandler();
                    engineHandler.engine = engine;
                    engineHandler.ignitionState = engine.IgnitionState;
                    engines.Add(engineHandler);
                }
            }
            else
            {
                EngineModuleWrapper engine = new EngineModuleWrapper(this.part);
                EngineHandler engineHandler = new EngineHandler();
                engineHandler.engine = engine;
                engineHandler.ignitionState = engine.IgnitionState;
                engines.Add(engineHandler);
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }

        // Failure methods
        public override void DoFailure()
        {
            base.DoFailure();
        }
        public override float DoRepair()
        {
            return base.DoRepair();
        }
    }
}


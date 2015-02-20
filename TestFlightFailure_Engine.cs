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
        public string engineIndex = "";
        [KSPField(isPersistant=true)]
        public string engineID = "";

        protected struct EngineHandler
        {
            public EngineModuleWrapper.EngineIgnitionState ignitionState;
            public EngineModuleWrapper engine;
            public bool failEngine;
        }

        List<EngineHandler> engines = null;

        public new bool TestFlightEnabled
        {
            get
            {
                // Make sure we have valid engines
                if (engines == null)
                    return false;

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

        public void Startup()
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
                        engineHandler.ignitionState = EngineModuleWrapper.EngineIgnitionState.UNKNOWN;
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
                        engineHandler.ignitionState = EngineModuleWrapper.EngineIgnitionState.UNKNOWN;
                        engines.Add(engineHandler);
                    }
                }
                else
                {
                    int index = int.Parse(engineIndex);
                    EngineModuleWrapper engine = new EngineModuleWrapper(this.part, index);
                    EngineHandler engineHandler = new EngineHandler();
                    engineHandler.engine = engine;
                    engineHandler.ignitionState = EngineModuleWrapper.EngineIgnitionState.UNKNOWN;
                    engines.Add(engineHandler);
                }
            }
            else if (!String.IsNullOrEmpty(engineIndex))
            {
                if (engineIndex.ToLower() == "all")
                {
                    int length = this.part.Modules.Count;
                    for (int i = 0; i < length; i++)
                    {
                        PartModule pm = this.part.Modules.GetModule(i);
                        ModuleEngines pmEngine = pm as ModuleEngines;
                        ModuleEnginesFX pmEngineFX = pm as ModuleEnginesFX;
                        EngineModuleWrapper engine = null;
                        if (pmEngine != null || pmEngineFX != null)
                        {
                            engine = new EngineModuleWrapper(this.part, i);
                            EngineHandler engineHandler = new EngineHandler();
                            engineHandler.engine = engine;
                            engineHandler.ignitionState = EngineModuleWrapper.EngineIgnitionState.UNKNOWN;
                            engines.Add(engineHandler);
                        }
                    }
                }
                else if (engineIndex.Contains(","))
                {
                    string[] sEngineIndices = engineIndex.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string sEngineIndex in sEngineIndices)
                    {
                        int index = int.Parse(sEngineIndex);
                        EngineModuleWrapper engine = new EngineModuleWrapper(this.part, index);
                        EngineHandler engineHandler = new EngineHandler();
                        engineHandler.engine = engine;
                        engineHandler.ignitionState = EngineModuleWrapper.EngineIgnitionState.UNKNOWN;
                        engines.Add(engineHandler);
                    }
                }
                else
                {
                    int index = int.Parse(engineIndex);
                    EngineModuleWrapper engine = new EngineModuleWrapper(this.part, index);
                    EngineHandler engineHandler = new EngineHandler();
                    engineHandler.engine = engine;
                    engineHandler.ignitionState = EngineModuleWrapper.EngineIgnitionState.UNKNOWN;
                    engines.Add(engineHandler);
                }
            }
            else
            {
                EngineModuleWrapper engine = new EngineModuleWrapper(this.part);
                EngineHandler engineHandler = new EngineHandler();
                engineHandler.engine = engine;
                engineHandler.ignitionState = EngineModuleWrapper.EngineIgnitionState.UNKNOWN;
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
        public override double DoRepair()
        {
            return base.DoRepair();
        }
    }
}


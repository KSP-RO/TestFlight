﻿using System;
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

        protected List<EngineHandler> engines = null;

        public new bool TestFlightEnabled
        {
            get
            {
                ITestFlightCore core = TestFlightUtil.GetCore (this.part, Configuration);
                if (core == null)
                {
                    Log ("EngineBase: No TestFlight core found");
                    return false;
                }
                // Make sure we have valid engines
                if (engines == null)
                {
                    Log("EngineBase: No valid engines found");
                    return false;
                }
                return core.TestFlightEnabled;
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


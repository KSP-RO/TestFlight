using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_IgnitionFail : TestFlightFailureBase
    {
        [KSPField(isPersistant=true)]
        public bool restoreIgnitionCharge = false;
        [KSPField(isPersistant=true)]
        public string engineIndex = "";
        [KSPField(isPersistant=true)]
        public string engineID = "";

        public FloatCurve ignitionFailureRate;

        private ITestFlightCore core = null;

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
                // verify we have a valid core attached
                if (core == null)
                    return false;
                // We also need a reliability curve
                if (ignitionFailureRate == null)
                    return false;
                // and a valid engine
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

            while (core == null)
            {
                core = TestFlightUtil.GetCore(this.part);
                yield return null;
            }

            Startup();
        }

        public void Startup()
        {
            // We don't want this getting triggered as a random failure
            core.DisableFailure("TestFlightFailure_IgnitionFail");
            Part prefab = this.part.partInfo.partPrefab;
            foreach (PartModule pm in prefab.Modules)
            {
                TestFlightFailure_IgnitionFail modulePrefab = pm as TestFlightFailure_IgnitionFail;
                if (modulePrefab != null && modulePrefab.Configuration == configuration)
                    ignitionFailureRate = modulePrefab.ignitionFailureRate;
            }
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
            if (!TestFlightEnabled)
                return;

            // For each engine we are tracking, compare its current ignition state to our last known ignition state
            for (int i = 0; i < engines.Count; i++)
            {
                EngineHandler engine = engines[i];
                EngineModuleWrapper.EngineIgnitionState currentIgnitionState = engine.engine.IgnitionState;
                // If we are transitioning from not ignited to ignited, we do our check
                // The ignitionFailueRate defines the failure rate per flight data

                if (currentIgnitionState == EngineModuleWrapper.EngineIgnitionState.IGNITED)
                {
                    if (engine.ignitionState == EngineModuleWrapper.EngineIgnitionState.NOT_IGNITED || engine.ignitionState == EngineModuleWrapper.EngineIgnitionState.UNKNOWN)
                    {
                        // We want the initial flight data, not the current here
                        double initialFlightData = core.GetInitialFlightData();
                        double failureRate = ignitionFailureRate.Evaluate((float)initialFlightData);
                        failureRate = Mathf.Max((float)failureRate, (float)TestFlightUtil.MIN_FAILURE_RATE);

                        // r1 = the chance of survival right now at time index 1
                        // r2 = the chance of survival through ignition and into initial run up

                        double r1 = Mathf.Exp((float)-failureRate * 1f);
                        double r2 = Mathf.Exp((float)-failureRate * 3f);
                        double survivalChance = r2 / r1;
                        double failureRoll = core.RandomGenerator.NextDouble();
                        if (failureRoll > survivalChance)
                        {
                            engine.failEngine = true;
                            core.TriggerNamedFailure("TestFlightFailure_IgnitionFail");
                        }
                    }
                }
                engine.ignitionState = currentIgnitionState;
            }
        }

        // Failure methods
        public override void DoFailure()
        {
            base.DoFailure();
            if (!TestFlightEnabled)
                return;
            for (int i = 0; i < engines.Count; i++)
            {
                EngineHandler engine = engines[i];
                if (engine.failEngine)
                {
                    engine.engine.Shutdown();
                    if (OneShot && restoreIgnitionCharge)
                        RestoreIgnitor();
                    engine.failEngine = false;
                }
            }

        }
        public override double DoRepair()
        {
            base.DoRepair();
            for (int i = 0; i < engines.Count; i++)
            {
                EngineHandler engine = engines[i];
                if (engine.failEngine)
                {
                    engine.engine.Shutdown();
                    if (restoreIgnitionCharge)
                        RestoreIgnitor();
                    engine.failEngine = false;
                }
            }
            return 0;
        }
        public void RestoreIgnitor()
        {
            // part.Modules["ModuleEngineIgnitor"].GetType().GetField("ignitionsRemained").GetValue(part.Modules["ModuleEngineIgnitor"]));
            if (this.part.Modules.Contains("ModuleEngineIgnitor"))
            {
                int currentIgnitions = (int)part.Modules["ModuleEngineIgnitor"].GetType().GetField("ignitionsRemained").GetValue(part.Modules["ModuleEngineIgnitor"]);
                part.Modules["ModuleEngineIgnitor"].GetType().GetField("ignitionsRemained").SetValue(part.Modules["ModuleEngineIgnitor"], currentIgnitions + 1);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasNode("ignitionFailureRate"))
            {
                ignitionFailureRate = new FloatCurve();
                ignitionFailureRate.Load(node.GetNode("ignitionFailureRate"));
            }
            else
                ignitionFailureRate = null;
        }
    }
}


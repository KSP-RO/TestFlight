using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_IgnitionFail : TestFlightFailure_Engine
    {
        [KSPField(isPersistant=true)]
        public bool restoreIgnitionCharge = false;
        [KSPField(isPersistant=true)]
        public bool ignorePressureOnPad = true;

        public FloatCurve ignitionFailureRate;
        public FloatCurve pressureMultiplier;

        private ITestFlightCore core = null;


        private bool _FARLoaded = false, check = true;
        /// <summary>
        /// Returns if FAR is currently loaded in the game
        /// </summary>
        public bool FARLoaded
        {
            get 
            {
                if (check) { _FARLoaded = AssemblyLoader.loadedAssemblies.Any(a => a.dllName == "FerramAerospaceResearch"); check = false; }
                return _FARLoaded;
            }
        }
        private MethodInfo _densityMethod = null;
        /// <summary>
        /// A delegate to the FAR GetCurrentDensity method
        /// </summary>
        public MethodInfo densityMethod
        {
            get
            {
                if (_densityMethod == null)
                {
                    _densityMethod = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.dllName == "FerramAerospaceResearch").assembly
                        .GetTypes().Single(t => t.Name == "FARAeroUtil").GetMethods().Where(m => m.IsPublic && m.IsStatic)
                        .Where(m => m.ReturnType == typeof(double) && m.Name == "GetCurrentDensity").ToDictionary(m => m, m => m.GetParameters())
                        .Single(p => p.Value[0].ParameterType == typeof(CelestialBody) && p.Value[1].ParameterType == typeof(double)).Key;
                }
                return _densityMethod;
            }
        }
        public double DynamicPressure
        {
            get
            {
                double dynamicPressure = this.part.dynamicPressureAtm;
                if (FARLoaded)
                {
                    Vector3 velocity = this.part.Rigidbody.velocity + Krakensbane.GetFrameVelocityV3f();
                    float sqrSpeed = velocity.sqrMagnitude;
                    double airDensity = (double)densityMethod.Invoke(null, new object[] { this.vessel.mainBody, this.vessel.altitude, true });
                    dynamicPressure = 0.5 * airDensity * sqrSpeed;
                }

                return dynamicPressure;
            }
        }
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

        public override void Startup()
        {
            base.Startup();
            // We don't want this getting triggered as a random failure
            core.DisableFailure("TestFlightFailure_IgnitionFail");
            Part prefab = this.part.partInfo.partPrefab;
            foreach (PartModule pm in prefab.Modules)
            {
                TestFlightFailure_IgnitionFail modulePrefab = pm as TestFlightFailure_IgnitionFail;
                if (modulePrefab != null && modulePrefab.Configuration == configuration)
                    ignitionFailureRate = modulePrefab.ignitionFailureRate;
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
                        Log(String.Format("IgnitionFail: Engine {0} transitioning to INGITED state", engine.engine.Module.GetInstanceID()));
                        // We want the initial flight data, not the current here
                        double initialFlightData = core.GetInitialFlightData();
                        double failureRate = ignitionFailureRate.Evaluate((float)initialFlightData);
                        if (pressureMultiplier != null)
                        {
                            if (!ignorePressureOnPad)
                            {
                                failureRate = failureRate * pressureMultiplier.Evaluate((float)DynamicPressure);
                            }
                            else
                            {
                                if (this.vessel.situation != Vessel.Situations.PRELAUNCH)
                                {
                                    failureRate = failureRate * pressureMultiplier.Evaluate((float)DynamicPressure);
                                }
                            }
                        }
                        failureRate = Mathf.Max((float)failureRate, (float)TestFlightUtil.MIN_FAILURE_RATE);

                        // r1 = the chance of survival right now at time index 1
                        // r2 = the chance of survival through ignition and into initial run up

                        double r1 = Mathf.Exp((float)-failureRate * 1f);
                        double r2 = Mathf.Exp((float)-failureRate * 3f);
                        double survivalChance = r2 / r1;
                        double failureRoll = core.RandomGenerator.NextDouble();
                        Log(String.Format("IgnitionFail: Engine {0} surivival chance {1:F4}, roll {2:F4}", engine.engine.Module.GetInstanceID(), survivalChance, failureRoll));
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
                    engines[i].failEngine = false;
                }
            }

        }
        public override double DoRepair()
        {
            base.DoRepair();
            for (int i = 0; i < engines.Count; i++)
            {
                EngineHandler engine = engines[i];
                {
                    engine.engine.Shutdown();
                    if (restoreIgnitionCharge)
                        RestoreIgnitor();
                    engines[i].failEngine = false;
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
            if (node.HasNode("pressureMultiplier"))
            {
                pressureMultiplier = new FloatCurve();
                pressureMultiplier.Load(node.GetNode("pressureMultiplier"));
            }
            else
                pressureMultiplier = null;
        }
    }
}


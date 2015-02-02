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
        public FloatCurve ignitionFailureRate;
        public int engineIndex = 0;

        private ITestFlightCore core = null;
        private EngineModuleWrapper.EngineIgnitionState previousIgnitionState = EngineModuleWrapper.EngineIgnitionState.UNKNOWN;
        private EngineModuleWrapper engine = null;

        public new bool TestFlightEnabled
        {
            get
            {
                bool enabled = true;
                // verify we have a valid core attached
                if (core == null)
                    return false;
                // If this part has a ModuleEngineConfig then we need to verify we are assigned to the active configuration
                if (this.part.Modules.Contains("ModuleEngineConfigs"))
                {
                    string currentConfig = (string)(part.Modules["ModuleEngineConfigs"].GetType().GetField("configuration").GetValue(part.Modules["ModuleEngineConfigs"]));
                    if (currentConfig != configuration)
                        enabled = false;
                }
                // We also need a reliability curve
                if (ignitionFailureRate == null)
                    return false;
                // and a valid engine
                if (engine == null)
                    return false;

                return enabled;
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
            if (engineIndex > 0)
                engine = new EngineModuleWrapper(this.part, engineIndex);
            else
                engine = new EngineModuleWrapper(this.part);
        }

        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            EngineModuleWrapper.EngineIgnitionState currentIgnitionState = engine.IgnitionState;
            // If we are transitioning from not ignited to ignited, we do our check
            // The ignitionFailueRate defines the failure rate per flight data

            if (currentIgnitionState == EngineModuleWrapper.EngineIgnitionState.IGNITED)
            {
                if (previousIgnitionState == EngineModuleWrapper.EngineIgnitionState.NOT_IGNITED || previousIgnitionState == EngineModuleWrapper.EngineIgnitionState.UNKNOWN)
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
                    double failureRoll = TestFlightUtil.GetCore(this.part).RandomGenerator.NextDouble();
                    if (failureRoll > survivalChance)
                        core.TriggerNamedFailure("TestFlightFailure_IgnitionFail");
                }
            }

            previousIgnitionState = currentIgnitionState;
        }

        // Failure methods
        public override void DoFailure()
        {
            if (!TestFlightEnabled)
                return;

            engine.Shutdown();
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
            if (node.HasValue("engineIndex"))
                engineIndex = int.Parse(node.GetValue("engineIndex"));
            else
                engineIndex = 0;
        }
    }
}


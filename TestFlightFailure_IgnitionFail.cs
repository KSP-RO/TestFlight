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
        [KSPField]
        public bool restoreIgnitionCharge = false;
        [KSPField]
        public bool ignorePressureOnPad = true;

        [KSPField]
        public FloatCurve baseIgnitionChance = null;
        [KSPField]
        public FloatCurve pressureCurve = null;
        [KSPField]
        public FloatCurve ignitionUseMultiplier = null;
        [KSPField]
        public float additionalFailureChance = 0f;

        [KSPField(isPersistant=true)]
        public int numIgnitions = 0;

        private ITestFlightCore core = null;
        private bool preLaunchFailures;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            core = TestFlightUtil.GetCore(this.part, Configuration);
            if (core != null)
                Startup();
            
            // Get the in-game setting for Launch Pad Ignition Failures
            preLaunchFailures = HighLogic.CurrentGame.Parameters.CustomParams<TestFlightGameSettings>().preLaunchFailures;
        }

        public override void Startup()
        {
            base.Startup();
            if (core == null)
                return;
            // We don't want this getting triggered as a random failure
            core.DisableFailure("TestFlightFailure_IgnitionFail");
        }

        public void OnEnable()
        {
            if (core == null)
                core = TestFlightUtil.GetCore(this.part, Configuration);
            if (core != null)
                Startup();
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
                        double failureRoll = 0d;
                        Log(String.Format("IgnitionFail: Engine {0} transitioning to INGITED state", engine.engine.Module.GetInstanceID()));
                        Log(String.Format("IgnitionFail: Checking curves..."));
                        numIgnitions++;

                        double initialFlightData = core.GetInitialFlightData();
                        float ignitionChance = 1f;
                        float multiplier = 1f;
                        
                        // Check to see if the vessel has not launched and if the player disabled pad failures
                        if (this.vessel.situation == Vessel.Situations.PRELAUNCH && !preLaunchFailures) {
                          ignitionChance = 1.0f;
                        } else {
                          ignitionChance = baseIgnitionChance.Evaluate((float)initialFlightData);
                          if (ignitionChance <= 0)    
                              ignitionChance = 1f;
                        }

                        multiplier = pressureCurve.Evaluate((float)(part.dynamicPressurekPa * 1000d));
                        if (multiplier <= 0f)
                            multiplier = 1f;

                        float minValue, maxValue = -1f;
                        baseIgnitionChance.FindMinMaxValue(out minValue, out maxValue);
                        Log(String.Format("TestFlightFailure_IgnitionFail: IgnitionChance Curve, Min Value {0:F2}:{1:F6}, Max Value {2:F2}:{3:F6}", baseIgnitionChance.minTime, minValue, baseIgnitionChance.maxTime, maxValue));
                          
                        if (this.vessel.situation != Vessel.Situations.PRELAUNCH)
                            ignitionChance = ignitionChance * multiplier * ignitionUseMultiplier.Evaluate(numIgnitions);

                        failureRoll = core.RandomGenerator.NextDouble();
                        Log(String.Format("IgnitionFail: Engine {0} ignition chance {1:F4}, roll {2:F4}", engine.engine.Module.GetInstanceID(), ignitionChance, failureRoll));
                        if (failureRoll > ignitionChance)
                        {
                            engine.failEngine = true;
                            core.TriggerNamedFailure("TestFlightFailure_IgnitionFail");
                            failureRoll = core.RandomGenerator.NextDouble();
                            if (failureRoll < additionalFailureChance)
                            {
                                core.TriggerFailure();
                            }
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
            Log(String.Format("IgnitionFail: Failing {0} engine(s)", engines.Count));
            for (int i = 0; i < engines.Count; i++)
            {
                EngineHandler engine = engines[i];
                if (engine.failEngine)
                {
                    engine.engine.Shutdown();
                    // For some reason, need to disable GUI as well
                    engine.engine.Events["Activate"].active = false;
                    engine.engine.Events["Shutdown"].active = false;
                    engine.engine.Events["Activate"].guiActive = false;
                    engine.engine.Events["Shutdown"].guiActive = false;
                    if ((restoreIgnitionCharge) || (this.vessel.situation == Vessel.Situations.PRELAUNCH) )
                        RestoreIgnitor();
                    engines[i].failEngine = false;
                }
            }

        }
        public override float DoRepair()
        {
            base.DoRepair();
            for (int i = 0; i < engines.Count; i++)
            {
                EngineHandler engine = engines[i];
                {
                    // Prevent auto-ignition on repair
                    engine.engine.Shutdown();
                    engine.engine.Events["Activate"].active = true;
                    engine.engine.Events["Activate"].guiActive = true;
                    engine.engine.Events["Shutdown"].guiActive = true;
                    if (restoreIgnitionCharge || this.vessel.situation == Vessel.Situations.PRELAUNCH)
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

        public override void OnAwake()
        {
            base.OnAwake();
            if (baseIgnitionChance == null)
            {
                baseIgnitionChance = new FloatCurve();
                baseIgnitionChance.Add(0f, 1f);
            }
            if (pressureCurve == null)
            {
                pressureCurve = new FloatCurve();
                pressureCurve.Add(0f, 1f);
            }
            if (ignitionUseMultiplier == null)
            {
                ignitionUseMultiplier = new FloatCurve();
                ignitionUseMultiplier.Add(0f, 1f);
            }
        }

        public override List<string> GetTestFlightInfo()
        {
            List<string> infoStrings = new List<string>();

            if (core == null)
            {
                Log("Core is null");
                return infoStrings;
            }
            if (baseIgnitionChance == null)
            {
                Log("Curve is null");
                return infoStrings;
            }

            infoStrings.Add("<b>Ignition Reliability</b>");
            infoStrings.Add(String.Format("<b>Current Ignition Chance</b>: {0:P}", baseIgnitionChance.Evaluate(core.GetInitialFlightData())));
            infoStrings.Add(String.Format("<b>Maximum Ignition Chance</b>: {0:P}", baseIgnitionChance.Evaluate(baseIgnitionChance.maxTime)));

            if (additionalFailureChance > 0f)
                infoStrings.Add(String.Format("<b>Additional Failure Chance</b>: {0:P}", additionalFailureChance));

            return infoStrings;
        }
    }
}


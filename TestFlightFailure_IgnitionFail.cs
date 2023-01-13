﻿using System;
using System.Collections.Generic;

using UnityEngine;

using TestFlightAPI;
using UnityEngine.Profiling;

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
        [KSPField]
        public FloatCurve restartWindowPenalty = null;

        [KSPField(isPersistant=true)]
        public int numIgnitions = 0;

        private readonly Dictionary<uint, EngineRunData> engineRunData = new Dictionary<uint, EngineRunData>(8);

        private bool preLaunchFailures;
        private bool dynPressurePenalties;
        private bool verboseDebugging;

        [KSPField(isPersistant=true)]
        private double previousTime;

        [KSPField(guiName = "Ignition Chance", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true, guiFormat = "P2")]
        private float ignitionChanceDisplay;
        [KSPField(guiName = "Ignition Penalty for Q", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true, guiFormat = "P2")]
        private float dynamicPressurePenalty;
        [KSPField(guiName = "Restart Ignition Penalty", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true, guiFormat = "P2")]
        private float restartPenalty;
        [KSPField(guiName = "Time Since Shutdown", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true, guiFormat = "N2", guiUnits = "s")]
        private float engineIdleTime;
        [KSPField(guiName = "Last Restart", groupName = "TestFlightDebug", groupDisplayName = "TestFlightDebug", guiActive = true)]
        private string restartRollString;

        [KSPField(guiName = "Current Ignition Chance", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActiveEditor = true, guiFormat = "P2")]
        public float currentIgnitionChance = 0f;

        [KSPField(guiName = "Max Ignition Chance", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActiveEditor = true, guiFormat = "P2")]
        public float maxIgnitionChance = 0f;

        private bool hasRestartWindow;
        private bool engineHasRun;

        private const float curveHigh = 0.8f;
        private const float curveLow = 0.3f;

        enum CurveState
        {
            Low,
            High,
            Unknown
        }

        private List<string> restartWindowDescription;

        private static float ClampModifier(float input)
        {
            if (input >= curveHigh) return 1;
            if (input <= curveLow) return 0;

            return 0.5f;
        }

        private CurveState GetState(float clampedModifier)
        {
            if (clampedModifier >= 1) return CurveState.High;
            if (clampedModifier <= 0) return CurveState.Low;
            return CurveState.Unknown;
        }

        public List<string> RestartCurveDescription()
        {
            var currentValue = ClampModifier(restartWindowPenalty.Evaluate(0f));
            var currentState = GetState(currentValue);
            List<string> info = new List<string>();

            for (float t = 0; t < restartWindowPenalty.maxTime; t += 0.01f)
            {
                var modifier = restartWindowPenalty.Evaluate(t);
                var state = GetState(ClampModifier(modifier));
                if (state != currentState)
                {
                    if (currentState == CurveState.High)
                    {
                        info.Add($"Restart modifier is >={curveHigh:P} until ~{TestFlightUtil.FormatTime(t, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false)}");
                    }
                    else if (currentState == CurveState.Low)
                    {
                        info.Add($"Restart modifier is <={curveLow:P} until ~{TestFlightUtil.FormatTime(t, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false)}");
                    }

                    if (currentState == CurveState.Unknown && state == CurveState.High)
                    {
                        info.Add($"Restart modifier climbs to >={curveHigh:P} at ~{TestFlightUtil.FormatTime(t, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false)}");
                    }
                    if (currentState == CurveState.Unknown && state == CurveState.Low)
                    {
                        info.Add($"Restart modifier drops to <={curveLow:P} at ~{TestFlightUtil.FormatTime(t, TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false)}");
                    }

                    currentState = state;
                }
            }

            return info;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            verboseDebugging = core.DebugEnabled;
            preLaunchFailures = HighLogic.CurrentGame.Parameters.CustomParams<TestFlightGameSettings>().preLaunchFailures;
            dynPressurePenalties = HighLogic.CurrentGame.Parameters.CustomParams<TestFlightGameSettings>().dynPressurePenalties;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            engineRunData.Clear();
            foreach (var configNode in node.GetNodes("ENGINE_RUN_DATA"))
            {
                var data = new EngineRunData(configNode);
                engineRunData.Add(data.id, data);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (var engineRunData in engineRunData.Values)
            {
                var dataNode = node.AddNode("ENGINE_RUN_DATA");
                engineRunData.Save(dataNode);
            }
        }

        public override void Startup()
        {
            base.Startup();
            // We don't want this getting triggered as a random failure
            core.DisableFailure("TestFlightFailure_IgnitionFail");
        }

        public EngineRunData GetEngineRunDataForID(uint id)
        {
            EngineRunData data;
            if (!engineRunData.TryGetValue(id, out data))
            {
                data = new EngineRunData(id);
                engineRunData.Add(id, data);
            }
            return data;
        }

        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            Profiler.BeginSample("TestFlight.IgnitionFail");
            double currentTime = Planetarium.GetUniversalTime();
            double deltaTime = (float)(currentTime - previousTime) / 1d;
            previousTime = currentTime;
            // For each engine we are tracking, compare its current ignition state to our last known ignition state
            for (int i = 0; i < engines.Count; i++)
            {
                EngineHandler engine = engines[i];
                EngineModuleWrapper.EngineIgnitionState currentIgnitionState = engine.engine.IgnitionState;
                var engineData = GetEngineRunDataForID(engine.engine.Module.PersistentId);

                double initialFlightData = core.GetInitialFlightData();
                float ignitionChance = 1f;
                float pressureModifier = 1f;
                float restartWindowModifier = 1f;
                
                // Check to see if the vessel has not launched and if the player disabled pad failures
                if (this.vessel.situation == Vessel.Situations.PRELAUNCH && !preLaunchFailures) {
                  ignitionChance = 1.0f;
                } else {
                  ignitionChance = baseIgnitionChance.Evaluate((float)initialFlightData);
                  if (ignitionChance <= 0)    
                      ignitionChance = 1f;
                }

                if (dynPressurePenalties)
                {
                    pressureModifier = Mathf.Clamp(pressureCurve.Evaluate((float)(part.dynamicPressurekPa * 1000d)), 0, 1);
                    if (pressureModifier <= 0f)
                        pressureModifier = 1f;
                }

                // if this engine has run before, and our config defines a restart window, we need to check against that and modify our ignition chance accordingly
                if (hasRestartWindow && engineData.hasBeenRun)
                {
                    engineIdleTime = engineData.timeSinceLastShutdown;
                    restartWindowModifier = Mathf.Clamp(restartWindowPenalty.Evaluate(engineIdleTime), 0, 1);
                }


                if (this.vessel.situation != Vessel.Situations.PRELAUNCH)
                    ignitionChance = ignitionChance * pressureModifier * ignitionUseMultiplier.Evaluate(numIgnitions) * restartWindowModifier;

                ignitionChanceDisplay = ignitionChance;
                dynamicPressurePenalty = 1f - pressureModifier;
                restartPenalty = 1f - restartWindowModifier;
                engineHasRun = engineData.hasBeenRun;

                if (currentIgnitionState == EngineModuleWrapper.EngineIgnitionState.IGNITED)
                {
                    engineData.timeSinceLastShutdown = 0;
                }
                else
                {
                    engineData.timeSinceLastShutdown += (float)deltaTime;
                }

                // If we are transitioning from not ignited to ignited, we do our check
                // The ignitionFailureRate defines the failure rate per flight data
                if (currentIgnitionState == EngineModuleWrapper.EngineIgnitionState.IGNITED)
                {
                    if (engine.ignitionState == EngineModuleWrapper.EngineIgnitionState.NOT_IGNITED || engine.ignitionState == EngineModuleWrapper.EngineIgnitionState.UNKNOWN)
                    {
                        if (verboseDebugging)
                        {
                            Log($"IgnitionFail: Engine {engine.engine.Module.GetInstanceID()} transitioning to INGITED state");
                            Log("IgnitionFail: Checking curves...");
                        }                        
                        numIgnitions++;

                        double failureRoll = core.RandomGenerator.NextDouble();
                        restartRollString = $"Roll: {failureRoll:P}, Chance: {ignitionChance:P}";
                        
                        if (verboseDebugging)
                        {
                            Log($"IgnitionFail: Engine {engine.engine.Module.GetInstanceID()} ignition chance {ignitionChance:F4}, roll {failureRoll:F4}");
                        }
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
                        else
                        {
                            engineData.hasBeenRun = true;
                        }
                    }
                }
                engine.ignitionState = currentIgnitionState;
            }
            Profiler.EndSample();
        }

        // Failure methods
        public override void DoFailure()
        {
            if (!TestFlightEnabled)
                return;
            Failed = true;
            float multiplier = 0;
            ITestFlightCore core = TestFlightUtil.GetCore(this.part, Configuration);
            if (core != null)
            {
                core.ModifyFlightData(duFail, true);
                string met = KSPUtil.PrintTimeCompact((int)Math.Floor(this.vessel.missionTime), false);
                if (dynPressurePenalties)
                {
                    multiplier = pressureCurve.Evaluate((float)(part.dynamicPressurekPa * 1000d));
                    if (multiplier <= 0f)
                        multiplier = 1f;
                }

                if (multiplier > float.Epsilon)
                {
                    FlightLogger.eventLog.Add($"[{met}] {core.Title} failed: Ignition Failure.  {(float)(part.dynamicPressurekPa * 1000d)}Pa dynamic pressure caused a {(1f-multiplier) * 100f}% reduction in normal ignition reliability.");
                }
                else
                {
                    FlightLogger.eventLog.Add($"[{met}] {core.Title} failed: Ignition Failure.");
                }
            }
            Log($"IgnitionFail: Failing {engines.Count} engine(s)");
            for (int i = 0; i < engines.Count; i++)
            {
                EngineHandler engine = engines[i];
                if (engine.failEngine)
                {
                    if (severity.ToLowerInvariant() == "major")
                    {
                        engine.engine.DisableRestart();
                    }

                    engine.engine.Shutdown();

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
            if (restartWindowPenalty == null)
            {
                restartWindowPenalty = new FloatCurve();
                restartWindowPenalty.Add(0f, 1f);
            }
            base.OnAwake();
        }

        public override void SetActiveConfig(string alias)
        {
            base.SetActiveConfig(alias);
            
            if (currentConfig == null) return;
            
            // update current values with those from the current config node
            currentConfig.TryGetValue("restoreIgnitionCharge", ref restoreIgnitionCharge);
            currentConfig.TryGetValue("ignorePressureOnPad", ref ignorePressureOnPad);
            currentConfig.TryGetValue("additionalFailureChance", ref additionalFailureChance);
            baseIgnitionChance = new FloatCurve();
            if (currentConfig.HasNode("baseIgnitionChance"))
            {
                baseIgnitionChance.Load(currentConfig.GetNode("baseIgnitionChance"));
            }
            else
            {
                baseIgnitionChance.Add(0f,1f);
            }
            pressureCurve = new FloatCurve();
            if (currentConfig.HasNode("pressureCurve"))
            {
                pressureCurve.Load(currentConfig.GetNode("pressureCurve"));
            }
            else
            {
                pressureCurve.Add(0f,1f);
            }
            ignitionUseMultiplier = new FloatCurve();
            if (currentConfig.HasNode("ignitionUseMultiplier"))
            {
                ignitionUseMultiplier.Load(currentConfig.GetNode("ignitionUseMultiplier"));
            }
            else
            {
                ignitionUseMultiplier.Add(0f,1f);
            }

            restartWindowPenalty = new FloatCurve();
            if (currentConfig.HasNode("restartWindowPenalty"))
            {
                restartWindowPenalty.Load(currentConfig.GetNode("restartWindowPenalty"));
                hasRestartWindow = true;
            }
            else
            {
                restartWindowPenalty.Add(0f, 1f);
                hasRestartWindow = false;
            }

            GetIgnitionChance(ref currentIgnitionChance, ref maxIgnitionChance);
        }

        public override string GetModuleInfo(string configuration)
        {
            string infoString = "";

            foreach (var configNode in configs)
            {
                if (!configNode.HasValue("configuration"))
                    continue;

                var nodeConfiguration = configNode.GetValue("configuration");

                if (string.Equals(nodeConfiguration, configuration, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (configNode.HasNode("baseIgnitionChance"))
                    {
                        var nodeIgnitionChance = new FloatCurve();
                        nodeIgnitionChance.Load(configNode.GetNode("baseIgnitionChance"));

                        float pMin = nodeIgnitionChance.Evaluate(nodeIgnitionChance.minTime);
                        float pMax = nodeIgnitionChance.Evaluate(nodeIgnitionChance.maxTime);
                        infoString = $"  Ignition at 0 data: <color=#b1cc00ff>{pMin:P1}</color>\n  Ignition at max data: <color=#b1cc00ff>{pMax:P1}</color>";
                    }
                }
            }

            return infoString;
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

            float flightData = core.GetFlightData();
            if (flightData < 0f)
                flightData = 0f;

            infoStrings.Add("<b>Ignition Reliability</b>");
            infoStrings.Add(String.Format("<b>Current Ignition Chance</b>: {0:P}", baseIgnitionChance.Evaluate(flightData)));
            infoStrings.Add(String.Format("<b>Maximum Ignition Chance</b>: {0:P}", baseIgnitionChance.Evaluate(baseIgnitionChance.maxTime)));

            if (additionalFailureChance > 0f)
                infoStrings.Add(String.Format("<b>Cascade Failure Chance</b>: {0:P}", additionalFailureChance));

            if (pressureCurve != null & pressureCurve.Curve.keys.Length > 1)
            {
                float maxTime = pressureCurve.maxTime;
                infoStrings.Add("<b>Dynamic pressure modifiers</b>");
                infoStrings.Add($"<b>0 kPa Pressure Modifier:</b> {pressureCurve.Evaluate(0)}");
                infoStrings.Add($"<b>{maxTime/1000} kPa Pressure Modifier</b>: {pressureCurve.Evaluate(maxTime):N}");
            }

            if (hasRestartWindow)
            {
                infoStrings.Add("<B>Restart timing modifiers</b>");
                infoStrings.AddRange(RestartCurveDescription());
            }

            return infoStrings;
        }

        /// <summary>
        /// Gets current and max data ignition chance for use in engine PAW
        /// </summary>
        /// <param name="currentIgnitionChance"></param>
        /// <param name="maxIgnitionChance"></param>
        private void GetIgnitionChance(ref float currentIgnitionChance, ref float maxIgnitionChance)
        {
            if (core == null)
            {
                core = TestFlightUtil.GetCore(part);
            }

            foreach (var failureModule in TestFlightUtil.GetFailureModules(this.part, core.Alias))
            {
                TestFlightFailure_IgnitionFail ignitionFailure = failureModule as TestFlightFailure_IgnitionFail;

                if (ignitionFailure != null)
                {
                    FloatCurve curve = ignitionFailure.baseIgnitionChance;

                    if (curve == null)
                    {
                        Log("Curve is null");
                        return;
                    }

                    float flightData = core.GetFlightData();
                    if (flightData < 0f)
                        flightData = 0f;

                    currentIgnitionChance = curve.Evaluate(flightData);
                    maxIgnitionChance = curve.Evaluate(curve.maxTime);

                    return;
                }
            }
        }
    }
}


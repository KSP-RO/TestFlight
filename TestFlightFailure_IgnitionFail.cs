using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using TestFlightAPI;
using TestFlightCore;

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

        private List<EngineRunData> engineRunData;

        private ITestFlightCore core = null;
        private bool preLaunchFailures;
        private bool dynPressurePenalties;
        private bool verboseDebugging;
        private float restartTimeMin;
        private float restartTimeMax = float.MaxValue;
        private bool hasRestartWindow;

        [KSPField(isPersistant=true)]
        private double previousTime;
        
        [KSPField(guiName = "Ignition Chance", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true)]
        private string ignitionChanceString;
        [KSPField(guiName = "Ignition Penalty for Q", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true)]
        private string dynamicPressurePenaltyString;
        [KSPField(guiName = "Restart Ignition Penalty", groupName = "TestFlight", groupDisplayName = "TestFlight", guiActive = true)]
        private string restartPenaltyString;

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
                        info.Add($"Restart modifier is >={curveHigh:P} until ~{t:N} seconds");
                    }
                    else if (currentState == CurveState.Low)
                    {
                        info.Add($"Restart modifier is <={curveLow:P} until ~{t:N} seconds");
                    }

                    if (currentState == CurveState.Unknown && state == CurveState.High)
                    {
                        info.Add($"Restart modifier climbs to >={curveHigh:P} at ~{t:N} seconds");
                    }
                    if (currentState == CurveState.Unknown && state == CurveState.Low)
                    {
                        info.Add($"Restart modifier drops to <={curveLow:P} at ~{t:N} seconds");
                    }

                    currentState = state;
                }
            }

            return info;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            core = TestFlightUtil.GetCore(this.part, Configuration);
            if (core != null)
                Startup();

            verboseDebugging = core.DebugEnabled;
            
            // Get the in-game settings
            preLaunchFailures = HighLogic.CurrentGame.Parameters.CustomParams<TestFlightGameSettings>().preLaunchFailures;
            dynPressurePenalties = HighLogic.CurrentGame.Parameters.CustomParams<TestFlightGameSettings>().dynPressurePenalties;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            engineRunData = new List<EngineRunData>();
            foreach (var configNode in node.GetNodes("ENGINE_RUN_DATA"))
            {
                engineRunData.Add(new EngineRunData(configNode));
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (var engineRunData in engineRunData)
            {
                var dataNode = node.AddNode("ENGINE_RUN_DATA");
                engineRunData.Save(dataNode);
            }
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

        public EngineRunData GetEngineRunDataForID(uint id)
        {
            return engineRunData.FirstOrDefault(data => data.id == id);
        }

        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            double currentTime = Planetarium.GetUniversalTime();
            double deltaTime = (float)(currentTime - previousTime) / 1d;

            // For each engine we are tracking, compare its current ignition state to our last known ignition state
            for (int i = 0; i < engines.Count; i++)
            {
                EngineHandler engine = engines[i];
                EngineModuleWrapper.EngineIgnitionState currentIgnitionState = engine.engine.IgnitionState;
                var engineData = GetEngineRunDataForID(engine.engine.Module.PersistentId);
                if (engineData == null)
                {
                    engineData = new EngineRunData(engine.engine.Module.PersistentId);
                    engineRunData.Add(engineData);
                }

                if (currentIgnitionState == EngineModuleWrapper.EngineIgnitionState.IGNITED)
                {
                    engineData.timeSinceLastShutdown = 0;
                }
                else
                {
                    engineData.timeSinceLastShutdown += (float)deltaTime;
                }
                

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
                    var idleTime = engineData.timeSinceLastShutdown;
                    if (idleTime < restartTimeMin || idleTime > restartTimeMax)
                    {
                        restartWindowModifier = Mathf.Clamp(restartWindowPenalty.Evaluate((float)idleTime), 0, 1);
                    }
                }

                float minValue, maxValue = -1f;
                baseIgnitionChance.FindMinMaxValue(out minValue, out maxValue);
                if (verboseDebugging)
                {
                    Log(String.Format("TestFlightFailure_IgnitionFail: IgnitionChance Curve, Min Value {0:F2}:{1:F6}, Max Value {2:F2}:{3:F6}", baseIgnitionChance.minTime, minValue, baseIgnitionChance.maxTime, maxValue));
                }

                if (this.vessel.situation != Vessel.Situations.PRELAUNCH)
                    ignitionChance = ignitionChance * pressureModifier * ignitionUseMultiplier.Evaluate(numIgnitions) * restartWindowModifier;

                ignitionChanceString = $"{ignitionChance:P}";
                dynamicPressurePenaltyString = $"{1-pressureModifier:P}";
                restartPenaltyString = $"{1-restartWindowModifier:P}";

                // If we are transitioning from not ignited to ignited, we do our check
                // The ignitionFailureRate defines the failure rate per flight data
                if (currentIgnitionState == EngineModuleWrapper.EngineIgnitionState.IGNITED)
                {
                    if (engine.ignitionState == EngineModuleWrapper.EngineIgnitionState.NOT_IGNITED || engine.ignitionState == EngineModuleWrapper.EngineIgnitionState.UNKNOWN)
                    {
                        double failureRoll = 0d;
                        if (verboseDebugging)
                        {
                            Log(String.Format("IgnitionFail: Engine {0} transitioning to INGITED state", engine.engine.Module.GetInstanceID()));
                            Log(String.Format("IgnitionFail: Checking curves..."));
                        }                        
                        numIgnitions++;

                        failureRoll = core.RandomGenerator.NextDouble();
                        if (verboseDebugging)
                        {
                            Log(String.Format("IgnitionFail: Engine {0} ignition chance {1:F4}, roll {2:F4}", engine.engine.Module.GetInstanceID(), ignitionChance, failureRoll));
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
                    FlightLogger.eventLog.Add($"[{met}] {core.Title} failed: Ignition Failure.  {(float)(part.dynamicPressurekPa * 1000d)}Pa dynamic pressure cased a {(1f-multiplier) * 100f}% reduction in normal ignition reliability.");
                }
                else
                {
                    FlightLogger.eventLog.Add($"[{met}] {core.Title} failed: Ignition Failure.");
                }
            }
            Log(String.Format("IgnitionFail: Failing {0} engine(s)", engines.Count));
            for (int i = 0; i < engines.Count; i++)
            {
                EngineHandler engine = engines[i];
                if (engine.failEngine)
                {
                    engine.engine.Shutdown();

                    if (severity.ToLowerInvariant() == "major")
                    {
                        // For some reason, need to disable GUI as well
                        engine.engine.Events["Activate"].active = false;
                        engine.engine.Events["Shutdown"].active = false;
                        engine.engine.Events["Activate"].guiActive = false;
                        engine.engine.Events["Shutdown"].guiActive = false;
                    }
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
            if (!string.IsNullOrEmpty(configNodeData))
            {
                var node = ConfigNode.Parse(configNodeData);
                OnLoad(node);
            }
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
                restartWindowDescription = RestartCurveDescription();
            }
            else
            {
                restartWindowPenalty.Add(0f, 1f);
                restartTimeMin = 0;
                restartTimeMax = float.MaxValue;
                hasRestartWindow = false;
            }
            
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
                infoStrings.AddRange(restartWindowDescription);
            }

            return infoStrings;
        }
    }
}


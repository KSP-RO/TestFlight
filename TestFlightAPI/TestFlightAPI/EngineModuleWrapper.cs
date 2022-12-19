using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using TestFlightAPI;

public class EngineModuleWrapper
{
    public enum EngineModuleType
    {
        UNKNOWN = -1,
        ENGINE,
        SOLVERENGINE
    }

    public enum EngineIgnitionState
    {
        UNKNOWN = -1,
        NOT_IGNITED,
        IGNITED,
    }

    public ModuleEngines moduleEngine;
    public EngineModuleType engineType;

    // Used to store the original fuel flow values
    private float _minFuelFlow;
    private float _maxFuelFlow;
    private float _g;

    // Public methods
    public PartModule Module
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return null;

            return moduleEngine as PartModule;
        }
    }

    public string Status
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return string.Empty;

            return moduleEngine.status;
        }
        set
        {
            if (engineType != EngineModuleType.UNKNOWN)
                moduleEngine.status = value;
        }
    }

    public string StatusL2
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return string.Empty;

            return moduleEngine.statusL2;
        }
        set
        {
            if (engineType != EngineModuleType.UNKNOWN)
            {
                moduleEngine.statusL2 = value;
                if (!string.IsNullOrEmpty(value))
                {
                    moduleEngine.Fields["statusL2"].guiActive = true;
                }
                else
                {
                    moduleEngine.Fields["statusL2"].guiActive = false;
                }
            }
        }
    }

    public EngineModuleType EngineType
    {
        get { return engineType; }
    }

    public bool allowShutdown
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return false;

            return moduleEngine.allowShutdown;
        }
        set
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return;
            moduleEngine.allowShutdown = value;
        }
    }

    public bool throttleLocked
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return false;
            return moduleEngine.throttleLocked;
        }
        set
        {
            moduleEngine.throttleLocked = value;
        }
    }

    public float minFuelFlow
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return 0f;
            return moduleEngine.minFuelFlow;
        }
        set
        {
            moduleEngine.minFuelFlow = value;
        }
    }

    public float maxFuelFlow
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return 0f;
            return moduleEngine.maxFuelFlow;
        }
        set
        {
            moduleEngine.maxFuelFlow = value;
        }
    }

    public float g
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return 0f;
            return moduleEngine.g;
        }
        set
        {
            moduleEngine.g = value;
        }
    }

    public float maxThrust
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return 0f;
            return moduleEngine.maxThrust;
        }
        set
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return;
            moduleEngine.maxThrust = value;
        }
    }

    public float minThrust
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return 0f;

            return moduleEngine.minThrust;
        }
        set
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return;

            moduleEngine.minThrust = value;
        }
    }

    public bool flameout
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return false;

            return moduleEngine.flameout;
        }
        set
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return;
            moduleEngine.flameout = value;
        }
    }

    public bool enabled
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return false;

            return moduleEngine.enabled;
        }
        set
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return;

            moduleEngine.enabled = value;
        }
    }

    // DEPRECATED no longer an engine property in KSP 1.0
    public float requestedThrust
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return 0f;
            
            return 0f;
        }
    }

    public float thrustRatio
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return 0f;
            // current thrust / maxThrust * vac_Isp / current_Isp / ispMult / flowMult
            // var currentThrust = moduleEngine.finalThrust;
            // var maxThrust = moduleEngine.maxThrust;
            // var vac_Isp = moduleEngine.atmosphereCurve.Evaluate(0f);
            // var current_Isp = moduleEngine.realIsp;
            // var ispMult = moduleEngine.multIsp;
            // var flowMult = moduleEngine.flowMultiplier;

            return moduleEngine.finalThrust / moduleEngine.maxThrust * moduleEngine.atmosphereCurve.Evaluate(0f) / 
                   moduleEngine.realIsp / moduleEngine.multIsp / moduleEngine.flowMultiplier;
        }
    }

    public float finalThrust
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return 0f;

            return moduleEngine.finalThrust;
        }
    }

    public bool EngineIgnited
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return false;
            
            return moduleEngine.EngineIgnited;
        }
    }

    public BaseEventList Events
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return null;

            return moduleEngine.Events;
        }
    }

    public EngineIgnitionState IgnitionState
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return EngineIgnitionState.UNKNOWN;

            if (moduleEngine.finalThrust > 0f)
                return EngineIgnitionState.IGNITED;

            return EngineIgnitionState.NOT_IGNITED;
        }
    }
    
    public bool failed { get; set; }
    public string failMessage { get; set; }

    // "Shutdown Engine"
    public void Shutdown()
    {
        if (engineType == EngineModuleType.UNKNOWN)
            return;

        moduleEngine.allowShutdown = true;
        moduleEngine.Shutdown();
        moduleEngine.DeactivateRunningFX();
        moduleEngine.DeactivatePowerFX();
    }

    // Disable restarts for failed engines
    public void DisableRestart()
    {
        if (engineType == EngineModuleType.UNKNOWN)
            return;

        // Need to disable this to prevent other mods from restarting the engine.
        moduleEngine.allowRestart = false;

        // For some reason, need to disable GUI as well
        Events["Activate"].active = false;
        Events["Shutdown"].active = false;
        Events["Activate"].guiActive = false;
        Events["Shutdown"].guiActive = false;
    }

    // Reduce fuel flow
    public void SetFuelFlowMult(float multiplier)
    {
        if (engineType == EngineModuleType.UNKNOWN)
            return;
        
        moduleEngine.multIsp = multiplier;
    }

    public void SetFuelIspMult(float multiplier)
    {
        if (engineType == EngineModuleType.UNKNOWN)
            return;
        
        moduleEngine.multFlow = multiplier;
    }

    public void SetIgnitionCount(int numIgnitions)
    {
        if (engineType == EngineModuleType.SOLVERENGINE)
        {
            if (moduleEngine.GetType().Name == "ModuleEnginesRF")
            {
                moduleEngine.GetType().GetField("ignitions").SetValue(moduleEngine, numIgnitions);
            }
        }
    }

    public void AddIgnitions(int numIgnitions)
    {
        if (engineType == EngineModuleType.SOLVERENGINE)
        {
            if (moduleEngine.GetType().Name == "ModuleEnginesRF")
            {
                int currentIgnitions = GetIgnitionCount();
                if (currentIgnitions < 0)
                    return;
                SetIgnitionCount(numIgnitions + currentIgnitions);
            }
        }
    }

    public void RemoveIgnitions(int numIgnitions)
    {
        // < 0 removes all ignitions
        if (engineType == EngineModuleType.SOLVERENGINE)
        {
            if (moduleEngine.GetType().Name == "ModuleEnginesRF")
            {
                if (numIgnitions < 0)
                {
                    moduleEngine.GetType().GetField("ignitions").SetValue(moduleEngine, 0);
                }
                else
                {
                    int currentIgnitions = GetIgnitionCount();
                    int newIgnitions = Math.Max(0, currentIgnitions - numIgnitions);
                    SetIgnitionCount(newIgnitions);
                }
            }
        }
    }

    public int GetIgnitionCount()
    {
        int currentIgnitions = -1;
        if (engineType == EngineModuleType.SOLVERENGINE)
        {
            if (moduleEngine.GetType().Name == "ModuleEnginesRF")
            {
                currentIgnitions = (int)moduleEngine.GetType().GetField("ignitions").GetValue(moduleEngine);
            }
        }
        return currentIgnitions;
    }

    public EngineModuleWrapper()
    {
    }

    public void Init(Part part)
    {
        InitWithEngine(part, "");
    }

    public void InitWithEngine(Part part, string engineID)
    {
        ModuleEngines _engine = null;
        foreach (PartModule pm in part.Modules)
        {
            _engine = pm as ModuleEngines;
            if (_engine != null && (engineID == "" || _engine.engineID.ToLowerInvariant() == engineID.ToLowerInvariant()))
                break;
        }
        if (_engine != null)
        {
            moduleEngine = _engine;
            string tName = moduleEngine.GetType().Name;
            if (tName == "ModuleEnginesRF" || tName.Contains("ModuleEnginesAJE"))
                engineType = EngineModuleType.SOLVERENGINE;
            else
                engineType = EngineModuleType.ENGINE;

            _minFuelFlow = moduleEngine.minFuelFlow;
            _maxFuelFlow = moduleEngine.maxFuelFlow;
            _g = moduleEngine.g;
        }
        else
        {
            engineType = EngineModuleType.UNKNOWN;
        }
    }

    ~EngineModuleWrapper()
    {
    }

    internal void Log(string message)
    {
        PartModule pm = this.Module;
        if (pm == null)
            return;
        Part part = pm.part;
        if (part == null)
            return;
        string meType = "UNKNOWN";
        if (EngineType == EngineModuleType.ENGINE)
            meType = "ENGINE";
        if (EngineType == EngineModuleType.SOLVERENGINE)
            meType = "SOLVERENGINE";

        message = String.Format("TestFlight_EngineModuleWrapper([{0}]): {1}", meType, message);
        TestFlightUtil.Log(message, part);
    }
}

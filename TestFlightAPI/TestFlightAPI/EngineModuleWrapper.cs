using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using TestFlightAPI;

// Thanks Squad so much for making two different nearly identical modules to handle engines
public class EngineModuleWrapper : ScriptableObject
{
    public enum EngineModuleType
    {
        UNKNOWN = -1,
        ENGINE,
        ENGINEFX
    }

    public enum EngineIgnitionState
    {
        UNKNOWN = -1,
        NOT_IGNITED,
        IGNITED,
    }

    ModuleEngines engine;
    ModuleEnginesFX engineFX;
    public EngineModuleType engineType;

    // Public methods
    public PartModule Module
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return null;

            if (engineType == EngineModuleType.ENGINE)
                return engine as PartModule;
            else
                return engineFX as PartModule;
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

            if (engineType == EngineModuleType.ENGINE)
                return engine.allowShutdown;
            else
                return engineFX.allowShutdown;
        }
        set
        {
            if (engineType == EngineModuleType.ENGINE)
                engine.allowShutdown = value;
            else
                engineFX.allowShutdown = value;
        }
    }

    public bool throttleLocked
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return false;

            if (engineType == EngineModuleType.ENGINE)
                return engine.throttleLocked;
            else
                return engineFX.throttleLocked;
        }
        set
        {
            if (engineType == EngineModuleType.ENGINE)
                engine.throttleLocked = value;
            else
                engineFX.throttleLocked = value;
        }
    }

    public float maxThrust
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return 0f;

            if (engineType == EngineModuleType.ENGINE)
                return engine.maxThrust;
            else
                return engineFX.maxThrust;
        }
        set
        {
            if (engineType == EngineModuleType.ENGINE)
                engine.maxThrust = value;
            else
                engineFX.maxThrust = value;
        }
    }

    public bool flameout
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return false;

            if (engineType == EngineModuleType.ENGINE)
                return engine.flameout;
            else
                return engineFX.flameout;
        }
        set
        {
            if (engineType == EngineModuleType.ENGINE)
                engine.flameout = value;
            else
                engineFX.flameout = value;
        }
    }

    public bool enabled
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return false;

            if (engineType == EngineModuleType.ENGINE)
                return engine.enabled;
            else
                return engineFX.enabled;
        }
        set
        {
            if (engineType == EngineModuleType.ENGINE)
                engine.enabled = value;
            else
                engineFX.enabled = value;
        }
    }

    public float requestedThrust
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return 0f;

            if (engineType == EngineModuleType.ENGINE)
                return engine.requestedThrust;
            else
                return engineFX.requestedThrust;
        }
    }

    public BaseEventList Events
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return null;

            if (engineType == EngineModuleType.ENGINE)
                return engine.Events;
            else
                return engineFX.Events;
        }
    }

    // "Shutdown Engine"
    public EngineIgnitionState IgnitionState
    {
        get
        {
            if (engineType == EngineModuleType.UNKNOWN)
                return EngineIgnitionState.UNKNOWN;

            if (flameout)
                return EngineIgnitionState.NOT_IGNITED;
            if (requestedThrust <= 0f)
                return EngineIgnitionState.NOT_IGNITED;
            if (!throttleLocked && Events.Contains("Shutdown Engine"))
                return EngineIgnitionState.NOT_IGNITED;

            return EngineIgnitionState.IGNITED;
        }
    }

    public void Shutdown()
    {
        if (engineType == EngineModuleType.UNKNOWN)
            return;

        if (engineType == EngineModuleType.ENGINE)
        {
            engine.Shutdown();
            engine.DeactivateRunningFX();
            engine.DeactivatePowerFX();
        }
        else
        {
            engineFX.Shutdown();
            engineFX.DeactivateLoopingFX();
        }
    }

    public EngineModuleWrapper(Part part)
    {
        engineType = EngineModuleType.UNKNOWN;

        if (part.Modules.Contains("ModuleEngines"))
        {
            engine = part.Modules["ModuleEngines"] as ModuleEngines;
            engineFX = null;
            engineType = EngineModuleType.ENGINE;
        }
        else if (part.Modules.Contains("ModuleEnginesFX"))
        {
            engine = null;
            engineFX = part.Modules["ModuleEnginesFX"] as ModuleEnginesFX;
            engineType = EngineModuleType.ENGINEFX;
        }
    }

    public EngineModuleWrapper(Part part, int index)
    {
        List<ModuleEngines> engines = null;
        List<ModuleEnginesFX> enginesFX = null;

        engineType = EngineModuleType.UNKNOWN;
        engine = null;
        engineFX = null;

        if (part.Modules.Contains("ModuleEngines"))
        {
            engines = part.Modules.OfType<ModuleEngines>().ToList();
            if (index < engines.Count)
            {
                engine = engines[index];
                engineFX = null;
                engineType = EngineModuleType.ENGINE;
            }
        }
        if (part.Modules.Contains("ModuleEnginesFX"))
        {
            enginesFX = part.Modules.OfType<ModuleEnginesFX>().ToList();
            if (index < enginesFX.Count)
            {
                engine = null;
                engineFX = enginesFX[index];
                engineType = EngineModuleType.ENGINEFX;
            }
        }
    }

    ~EngineModuleWrapper()
    {
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using KSPPluginFramework;

using TestFlightAPI;

namespace TestFlight
{

    // Method for determing distance from kerbal to part
    // float kerbalDistanceToPart = Vector3.Distance(kerbal.transform.position, targetPart.collider.ClosestPointOnBounds(kerbal.transform.position));

    public class FlightDataRecorder_Engine : FlightDataRecorderBase
    {
        public override void OnAwake()
        {
            base.OnAwake();
        }

        public override bool IsRecordingFlightData()
        {
            bool isRecording = true;

            if (!isEnabled)
                return false;

            // ModuleEngines
            if (this.part.Modules.Contains("ModuleEngines"))
            {
                ModuleEngines engine = (ModuleEngines)this.part.Modules["ModuleEngines"];
                if (!engine.isOperational)
                    return false;
                if (engine.normalizedThrustOutput <= 0)
                    return false;
                if (engine.finalThrust <= 0)
                    return false;
            }
            // ModuleEnginesFX
            if (this.part.Modules.Contains("ModuleEnginesFX"))
            {
                ModuleEnginesFX engine = (ModuleEnginesFX)this.part.Modules["ModuleEnginesFX"];
                if (!engine.isOperational)
                    return false;
                if (engine.normalizedThrustOutput <= 0)
                    return false;
                if (engine.finalThrust <= 0)
                    return false;
            }

            return isRecording;
        }
    }
}


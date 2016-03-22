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
        private EngineModuleWrapper engine;

        public override void OnAwake()
        {
            base.OnAwake();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            engine = new EngineModuleWrapper();
            engine.Init(this.part);
        }
        public override bool IsPartOperating()
        {
            if (!isEnabled)
                return false;

            return engine.IgnitionState == EngineModuleWrapper.EngineIgnitionState.IGNITED;
        }

        public override bool IsRecordingFlightData()
        {
            return IsPartOperating();
        }
    }
}


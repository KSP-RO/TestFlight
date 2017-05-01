using System;
using System.Collections.Generic;
using UnityEngine;

using TestFlightAPI;

namespace TestFlight
{
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
            if (this.part.vessel.situation == Vessel.Situations.PRELAUNCH)
                return false;

            return IsPartOperating();
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_HeatShield : FlightDataRecorderBase
    {
        //private ModuleAblator ablator;
        private double ablationTempThresh = 600f;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            ModuleAblator ablator = base.part.FindModuleImplementing<ModuleAblator>();
            this.ablationTempThresh = ablator.ablationTempThresh;
        }
        public override void OnAwake()
        {
            base.OnAwake();
        }
        public override bool IsRecordingFlightData()
        {
            return base.part.skinTemperature > this.ablationTempThresh;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_Lights : FlightDataRecorderBase
    {
        private ModuleLight module;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.module = base.part.FindModuleImplementing<ModuleLight>();
        }
        public override bool IsPartOperating()
        {
            return this.module.isOn;
        }
    }
}

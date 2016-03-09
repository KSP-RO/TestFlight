using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_Intake : FlightDataRecorderBase
    {
        private ModuleResourceIntake intake;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            intake = base.part.FindModuleImplementing<ModuleResourceIntake>();
        }
        public override bool IsPartOperating()
        {
            if (this.intake.airFlow < 0.1f)
            {
                return false;
            }

            return true;
        }
    }
}

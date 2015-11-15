using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_Solar : FlightDataRecorderBase
    {
        private ModuleDeployableSolarPanel module;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.module = base.part.FindModuleImplementing<ModuleDeployableSolarPanel>();
        }
        public override bool IsPartOperating()
        {
            if (base.part.ShieldedFromAirstream)
            {
                return false;
            }
            if (this.module.panelState == ModuleDeployableSolarPanel.panelStates.BROKEN || this.module.panelState == ModuleDeployableSolarPanel.panelStates.RETRACTED)
            {
                return false;
            }
            if (this.module.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED && this.module.flowRate < 0.01)
            {
                return false;
            }
            return true;
        }
    }
}

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
            if (this.module.deployState == ModuleDeployableSolarPanel.DeployState.BROKEN || this.module.deployState == ModuleDeployableSolarPanel.DeployState.RETRACTED)
            {
                return false;
            }
            if (this.module.deployState == ModuleDeployableSolarPanel.DeployState.EXTENDED && this.module.flowRate < 0.01)
            {
                return false;
            }
            return true;
        }
    }
}

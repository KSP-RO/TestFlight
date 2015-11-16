using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_RCS : FlightDataRecorderBase
    {
        private ModuleRCS rcs;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.rcs = base.part.FindModuleImplementing<ModuleRCS>();
        }
        public override bool IsPartOperating()
        {
            if (!this.rcs.isEnabled || base.part.ShieldedFromAirstream || !base.part.isControllable)
            {
                return false;
            }
            if (!base.vessel.ActionGroups[KSPActionGroup.RCS] || this.rcs.isJustForShow)
            {
                return false;
            }
            if (this.rcs.thrustForces.Count() == 0)
            {
                return false;
            }
            return true;
        }
    }
}

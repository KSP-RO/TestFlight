using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_FARCtrlSrf : FlightDataRecorderBase
    {
        private PartModule module;
        private BaseField fpitch;
        private BaseField froll;
        private BaseField fyaw;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            module = base.part.Modules["FARControllableSurface"];
            fpitch = this.module.Fields["pitchaxis"];
            froll = this.module.Fields["rollaxis"];
            fyaw = this.module.Fields["yawaxis"];
        }
        public override bool IsPartOperating()
        {
            if (base.part.ShieldedFromAirstream || base.vessel.atmDensity < 0.01)
            {
                return false;
            }
            if (Math.Abs(base.vessel.ctrlState.pitch) > 0.01 && fpitch.GetValue<float>(fpitch.host) != 0)
            {
                return true;
            }
            if (Math.Abs(base.vessel.ctrlState.roll) > 0.01 && froll.GetValue<float>(froll.host) != 0)
            {
                return true;
            }
            if (Math.Abs(base.vessel.ctrlState.yaw) > 0.01 && fyaw.GetValue<float>(fyaw.host) != 0)
            {
                return true;
            }
            return false;
        }
    }
}
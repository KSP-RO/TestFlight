using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_ReactionWheel : FlightDataRecorderBase
    {
        private ModuleReactionWheel module;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            module = base.part.FindModuleImplementing<ModuleReactionWheel>();
        }
        public override bool IsPartOperating()
        {
            if (!TestFlightEnabled || module == null)
            {
                return false;
            }
            if (module.wheelState != ModuleReactionWheel.WheelState.Active)
            {
                return false;
            }
            if (module.inputSum < 0.1)
            {
                return false;
            }
            
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_Wheels : FlightDataRecorderBase
    {
        private ModuleWheel wheel;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            wheel = base.part.FindModuleImplementing<ModuleWheel>();
        }
        public override void OnAwake()
        {
            base.OnAwake();
        }
        public override bool IsPartOperating()
        {
            bool isGrounded = false;
            for (int i = 0; i < this.wheel.wheels.Count; i++)
            {
                if (this.wheel.wheels[i].whCollider.isGrounded)
                {
                    isGrounded = true;
                    break;
                }
            }
            if (!isGrounded)
            {
                return false;
            }

            if ((float)base.vessel.horizontalSrfSpeed > 0f)
            {
                if (!this.wheel.steeringLocked && Math.Abs(this.wheel.steeringInput) > 0f)
                {
                    return true;
                }
                if (this.wheel.brakesEngaged)
                {
                    return true;
                }
                if (wheel.motorEnabled && Math.Abs(wheel.throttleInput) > 0f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

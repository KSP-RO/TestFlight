using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;
using ModuleWheels;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_Wheels : FlightDataRecorderBase
    {
        private ModuleWheelSteering wheelSteering;
        private ModuleWheelBrakes wheelBrakes;
        private ModuleWheelBase wheel;
        private ModuleWheelMotor wheelMotor;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            wheel = base.part.FindModuleImplementing<ModuleWheelBase>();
            wheelSteering = base.part.FindModuleImplementing<ModuleWheelSteering>();
            wheelBrakes = base.part.FindModuleImplementing<ModuleWheelBrakes>();
            wheelMotor = base.part.FindModuleImplementing<ModuleWheelMotor>();
        }
        public override void OnAwake()
        {
            base.OnAwake();
        }
        public override bool IsPartOperating()
        {
            bool isGrounded = wheel.isGrounded;

            if (!isGrounded)
            {
                return false;
            }

            if ((float)base.vessel.horizontalSrfSpeed > 0f)
            {
                if (this.wheelSteering.steeringEnabled && Math.Abs(this.wheelSteering.steeringInput) > 0f)
                {
                    return true;
                }
                if (this.wheelBrakes.brakeInput < 0f)
                {
                    return true;
                }
                if (wheelMotor.motorEnabled && Math.Abs(wheelMotor.driveOutput) > 0f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

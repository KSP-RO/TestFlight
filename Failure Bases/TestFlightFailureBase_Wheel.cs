using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;
using ModuleWheels;

namespace TestFlight
{
    public class TestFlightFailureBase_Wheel : TestFlightFailureBase
    {
        protected ModuleWheelBase wheelBase;
        protected ModuleWheelSteering wheelSteering;
        protected ModuleWheelBrakes wheelBrakes;
        protected ModuleWheelMotor wheelMotor;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            this.wheelBase = base.part.FindModuleImplementing<ModuleWheelBase>();
            this.wheelSteering = base.part.FindModuleImplementing<ModuleWheelSteering>();
            this.wheelBrakes = base.part.FindModuleImplementing<ModuleWheelBrakes>();
            this.wheelMotor = base.part.FindModuleImplementing<ModuleWheelMotor>();
        }
    }
}

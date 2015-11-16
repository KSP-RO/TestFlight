using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight
{
    public class TestFlightFailure_ReactionTorque : TestFlightFailureBase_ReactionWheel
    {
        private float PitchTorque;
        private float RollTorque;
        private float YawTorque;
        private int working;
        public override void DoFailure()
        {
            base.DoFailure();
            if (base.module != null)
            {
                this.PitchTorque = base.module.PitchTorque;
                this.RollTorque = base.module.RollTorque;
                this.YawTorque = base.module.YawTorque;
                this.working = 7;

                Random ran = new Random();
                int axis = ran.Next(0, 3);
                float modifier;
                while ((this.working & 2 ^ axis) != 0)
                {
                    this.working -= 2 ^ axis;
                    modifier = ((float)ran.NextDouble() * 2f) - 1f;
                    switch (axis)
                    {
                        case 0: //pitch
                            base.module.PitchTorque = this.PitchTorque * modifier;
                            break;
                        case 1: //roll
                            base.module.RollTorque = this.RollTorque * modifier;
                            break;
                        case 2: //yaw
                            base.module.YawTorque = this.YawTorque * modifier;
                            break;
                    }
                    axis = ran.Next(0, 6); //yes axis are only 0 1 2, but this lowers chance for a 2nd axis failure
                }
            }
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.module.PitchTorque = this.PitchTorque;
            base.module.RollTorque = this.RollTorque;
            base.module.YawTorque = this.YawTorque;
            return 0f;
        }
    }
}
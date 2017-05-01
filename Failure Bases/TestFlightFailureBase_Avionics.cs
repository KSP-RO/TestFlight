using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailureBase_Avionics : TestFlightFailureBase
    {
        public enum FailedState
        {
            Pitch = 0,
            Roll = 1,
            Yaw = 2,
            Rotate = 3,
            TranX = 4,
            TranY = 5,
            TranZ = 6,
            Translate = 7
        }

        [KSPField]
        public bool isFlyByWire = false;

        public FailedState failedState;
        public float failedValue;

        public override void DoFailure()
        {
            base.DoFailure();
            if (base.vessel != null)
            {
                Random ran = new Random();
                this.failedValue = 1f - (float)Math.Pow(ran.NextDouble(), 2);
                this.failedState = (FailedState)ran.Next(0, 8);
                base.vessel.OnFlyByWire -= this.OnFlyByWire;
                base.vessel.OnFlyByWire += this.OnFlyByWire;
            }

        }
        public override float DoRepair()
        {
            base.DoRepair();
            if (base.vessel != null)
            {
                base.vessel.OnFlyByWire -= this.OnFlyByWire;
            }
            return 0f;
        }
        public virtual void OnFlyByWire(FlightCtrlState s)
        {
            if (base.vessel == null || base.vessel != FlightGlobals.ActiveVessel || base.part.isControlSource != Vessel.ControlLevel.FULL)
            {
                return;
            }
            switch (this.failedState)
            {
                case FailedState.Pitch:
                    s.pitch = Calculate(s.pitch);
                    break;
                case FailedState.Roll:
                    s.roll = Calculate(s.roll);
                    break;
                case FailedState.Yaw:
                    s.yaw = Calculate(s.yaw);
                    break;
                case FailedState.Rotate:
                    s.pitch = Calculate(s.pitch);
                    s.roll = Calculate(s.roll);
                    s.yaw = Calculate(s.yaw);
                    break;
                case FailedState.TranX:
                    s.X = Calculate(s.X);
                    break;
                case FailedState.TranY:
                    s.Y = Calculate(s.Y);
                    break;
                case FailedState.TranZ:
                    s.Z = Calculate(s.Z);
                    break;
                case FailedState.Translate:
                    s.X = Calculate(s.X);
                    s.Y = Calculate(s.Y);
                    s.Z = Calculate(s.Z);
                    break;
            }
        }

        public virtual float Calculate(float value)
        {
            return value;
        }
        public void OnDestroy()
        {
            if (base.vessel != null)
            {
                base.vessel.OnFlyByWire -= this.OnFlyByWire;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TestFlightAPI;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_AvionicsThrustJam : TestFlightFailureBase
    {
        [KSPField(isPersistant = true)]
        public float throttle;

        public override void DoFailure()
        {
            base.DoFailure();
            if (base.vessel != null && base.part != null && base.vessel.referenceTransformId == base.part.flightID)
            {
                base.vessel.OnFlyByWire -= this.OnFlyByWire;
                base.vessel.OnFlyByWire += this.OnFlyByWire;
                this.throttle = base.vessel.ctrlState.mainThrottle;
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
        public override void OnFlyByWire(FlightCtrlState s)
        {
            s.mainThrottle = this.throttle;
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

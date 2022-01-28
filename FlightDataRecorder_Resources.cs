using TestFlightAPI;

namespace TestFlight
{
    public class FlightDataRecorder_Resources : FlightDataRecorderBase
    {
        [KSPField]
        public double emptyThreshold = 0.1;

        public override bool IsRecordingFlightData()
        {
            // base checks: TF enabled, PartModule isEnabled, IsPartOperating() and Vessel.situation
            if (base.IsRecordingFlightData())
            {
                foreach (PartResource resource in this.part.Resources)
                {
                    if (resource.amount > emptyThreshold)
                        return true;
                }
            }
            return false;
        }
    }
}


using TestFlightAPI;

namespace TestFlight
{
    public class FlightDataRecorder_Engine : FlightDataRecorderBase
    {
        private EngineModuleWrapper engine;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            engine = new EngineModuleWrapper();
            engine.Init(this.part);
        }

        public override bool IsPartOperating()
        {
            return base.IsPartOperating() && engine.IgnitionState == EngineModuleWrapper.EngineIgnitionState.IGNITED;
        }
    }
}


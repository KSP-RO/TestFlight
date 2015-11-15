using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_DockingPort : FlightDataRecorderBase
    {
        [KSPField]
        public float duDock = 10f;
        [KSPField]
        public float duUndock = 10f;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            //GameEvents.onPartCouple.Add(OnDock);
            //GameEvents.onPartUndock.Add(OnUndock);
        }
        public override void OnAwake()
        {
            base.OnAwake();
        }
        public override bool IsRecordingFlightData()
        {
            return false;
        }
        public override void OnUpdate()
        {
            return;
        }
        private void OnDock(GameEvents.FromToAction<Part,Part> action)
        {

        }
        private void OnUndock(Part part)
        {
            //if (part == base.part)
            //{
            //    base.core.ModifyFlightData(duUndock, true);
            //}
        }
        void OnDestroy()
        {
            //GameEvents.onPartUndock.Remove(OnUndock);
            //GameEvents.onPartCouple.Remove(OnDock);
        }
    }
}

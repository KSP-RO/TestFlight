using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using TestFlightAPI;

namespace TestFlight.Flight_Recorders
{
    public class FlightDataRecorder_RT : FlightDataRecorderBase
    {
        private PartModule module;
        private FieldInfo isActive;
        private FieldInfo isPowered;
        public override void OnAwake()
        {
            base.OnAwake();
            this.module = base.part.Modules["ModuleRTAntenna"];
            if (this.module != null)
            {
                this.isActive = this.module.GetType().GetField("IsRTActive");
                this.isPowered = this.module.GetType().GetField("IsRTPowered");
            }
        }
        public override bool IsPartOperating()
        {
            if (this.module != null && this.isActive != null && this.isPowered != null)
            {
                return (bool)isActive.GetValue(this.module) && (bool)isPowered.GetValue(this.module);
            }
            return false;
        }
        private void OnDestroy()
        {
            this.module = null;
        }
    }
}

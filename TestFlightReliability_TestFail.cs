using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightReliability_TestFail : TestFlightReliabilityBase
    {
        [KSPEvent(guiName = "Force Failure", active = true, guiActive = true)]
        public void ForceFailure()
        {
            string message = "Part has failed after forced failure";
            message = String.Format("TestFlightReliability({0}[{1}]): {2}", TestFlightUtil.GetFullPartName(this.part), Configuration, message);
            TestFlightUtil.Log(message, this.part);
            
            core.TriggerFailure();
        }
        public override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}

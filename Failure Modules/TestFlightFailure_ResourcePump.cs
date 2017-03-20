using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_ResourcePump : TestFlightFailureBase
    {
        [KSPField]
        public string resourceName = "ANY";
        [KSPField]
        public string resourceBlacklist = "";
        public override void DoFailure()
        {
            base.DoFailure();
            SetState(PartResource.FlowMode.None);
        }
        public override float DoRepair()
        {
            base.DoRepair();
            SetState(PartResource.FlowMode.Both);
            return 0f;
        }
        private void SetState(PartResource.FlowMode state)
        {
            List<string> blacklist = this.resourceBlacklist.Split(',').ToList();
            List<PartResource> valid = null;
            for (int i = 0; i < base.part.Resources.ToList().Count; i++)
            {
                PartResource res = base.part.Resources.ToList()[i];
                if (!blacklist.Contains(res.resourceName) && res.info.resourceFlowMode != ResourceFlowMode.NO_FLOW)
                {
                    if (this.resourceName == "ALL" || res.resourceName == this.resourceName)
                    {
                        res.flowMode = state;
                        return;
                    }
                    else if (this.resourceName == "ANY")
                    {
                        if (valid == null)
                        {
                            valid = new List<PartResource>();
                        }
                        valid.Add(res);
                    }
                }
            }
            if (this.resourceName == "ANY" && valid != null)
            {
                Random ran = new Random();
                int roll = ran.Next(0, valid.Count);
                valid[roll].flowMode = state;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;
using KSP;

namespace TestFlight
{
    public class TestFlightFailureBase_Gimbal : TestFlightFailureBase
    {
        [KSPField(isPersistant = true)]
        public string gimbalTransformName = "RANDOM";

        protected ModuleGimbal module;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            List<ModuleGimbal> gimbals = base.part.Modules.OfType<ModuleGimbal>().ToList();
            if (this.gimbalTransformName != "RANDOM")
            {
                for (int i = 0; i < gimbals.Count; i++)
                {
                    ModuleGimbal gimbal = gimbals[i];
                    if (gimbal.gimbalTransformName == this.gimbalTransformName && !gimbal.gimbalLock && gimbal.gimbalRange > 0f)
                    {
                        this.module = gimbal;
                        return;
                    }
                }
                gimbalTransformName = "RANDOM";
            }
            if (this.gimbalTransformName == "RANDOM")
            {
                List<ModuleGimbal> valid = new List<ModuleGimbal>();
                for (int i = 0; i < gimbals.Count; i++)
                {
                    ModuleGimbal gimbal = gimbals[i];
                    if (!gimbal.gimbalLock && gimbal.gimbalRange > 0f)
                    {
                        valid.Add(gimbal);
                    }
                }
                int roll = UnityEngine.Random.Range(0, valid.Count);
                module = valid[roll];
                gimbalTransformName = module.gimbalTransformName;
            }
        }
        public override void DoFailure()
        {
            base.DoFailure();
        }
        public override float DoRepair()
        {
            return base.DoRepair();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;
using KSP;

namespace TestFlight
{
    public class TestFlightFailure_Animation : TestFlightFailureBase
    {
        [KSPField]
        public string animationName = "ALL";
        public override void DoFailure()
        {
            base.DoFailure();
            SetState(false);
        }
        public override float DoRepair()
        {
            base.DoRepair();
            SetState(true);
            return 0F;
        }
        private void SetState(bool state)
        {
            List<ModuleAnimateGeneric> anims = this.part.Modules.OfType<ModuleAnimateGeneric>().ToList();
            for (int i = 0; i < anims.Count; i++)
            {
                if (this.animationName == "ALL" || this.animationName == anims[i].animationName)
                {
                    anims[i].allowManualControl = state;
                    anims[i].enabled = state;
                }
            }
        }
    }
}

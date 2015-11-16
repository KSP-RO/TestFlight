using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_FARCtrlSrfGlitch : TestFlightFailureBase_FARCtrlSrf
    {
        public override void DoFailure()
        {
            base.DoFailure();
            Random ran = new Random();
            int roll = ran.Next(1, 4);
            BaseField mf = base.module.Fields["flapDeflectionLevel"];
            SetFlap(base.module, (mf.GetValue<int>(mf.host) + roll) % 4);
        }
        public override float DoRepair()
        {
            base.DoRepair();
            SetFlap(base.module, 2);
            for (int i = 0; i < base.part.symmetryCounterparts.Count; i++)
            {
                Part p = base.part.symmetryCounterparts[i];
                for (int j = 0; j < p.Modules.Count; j++)
                {
                    PartModule m = p.Modules[j];
                    if (m.moduleName == "FARControllableSurface")
                    {
                        SetFlap(m, 2);
                    }
                }
            }
            return 0f;
        }
        private void SetFlap(PartModule m, int deflect)
        {
            BaseField mf = m.Fields["flapDeflectionLevel"];
            mf.SetValue(deflect, mf.host);
            m.Events["DeflectMore"].active = deflect < 3;
            m.Events["DeflectLess"].active = deflect > 0;
        }
    }
}
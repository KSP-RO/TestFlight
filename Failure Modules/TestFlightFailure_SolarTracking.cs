using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_SolarTracking : TestFlightFailureBase_Solar
    {
        public override void DoFailure()
        {
            base.DoFailure();
            base.module.trackingSpeed = 0;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            if (base.part != null && base.part.partInfo != null)
            {
                Part pf = base.part.partInfo.partPrefab;
                if (pf != null)
                {
                    ModuleDeployableSolarPanel pm = (ModuleDeployableSolarPanel)pf.Modules.OfType<ModuleDeployableSolarPanel>();
                    if (pm != null)
                    {
                        base.module.trackingSpeed = pm.trackingSpeed;
                    }
                }
            }
            return 0f;
        }
    }
}

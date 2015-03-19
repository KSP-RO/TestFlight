using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_LockGimbal : TestFlightFailureBase
    {
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>

        private float gimbalRange;
        public override void DoFailure()
        {
            base.DoFailure();
            List<ModuleGimbal> gimbals = this.part.Modules.OfType<ModuleGimbal>().ToList();
            foreach (ModuleGimbal gimbal in gimbals)
            {
                gimbalRange = gimbal.gimbalRange;
                gimbal.gimbalRange = 0f;
            }
        }

        public override float DoRepair()
        {
            base.DoRepair();
            List<ModuleGimbal> gimbals = this.part.Modules.OfType<ModuleGimbal>().ToList();
            foreach (ModuleGimbal gimbal in gimbals)
            {
                gimbal.gimbalRange = gimbalRange;
            }

            return 0;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TestFlight
{
    public class TestFlightFailure_GimbalSpeed : TestFlightFailureBase_Gimbal
    {
        private float baseSpeed = 10.0f;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            Part p = base.part.partInfo.partPrefab;
            List<ModuleGimbal> gimbals = p.Modules.OfType<ModuleGimbal>().ToList();
            for (int i = 0; i < gimbals.Count; i++)
            {
                ModuleGimbal g = gimbals[i];
                if (g.gimbalTransformName == base.gimbalTransformName)
                {
                    this.baseSpeed = g.gimbalResponseSpeed;
                    break;
                }
            }
        }
        public override void DoFailure()
        {
            base.DoFailure();
            base.module.gimbalResponseSpeed = UnityEngine.Random.Range(0, base.module.gimbalResponseSpeed);
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.module.gimbalResponseSpeed = this.baseSpeed;
            return 0f;
        }
    }
}
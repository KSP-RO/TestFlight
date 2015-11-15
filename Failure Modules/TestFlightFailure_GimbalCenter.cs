using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;
using KSP;
using UnityEngine;

namespace TestFlight
{
    public class TestFlightFailure_GimbalCenter : TestFlightFailureBase_Gimbal
    {
        private float baseRange = 10f;
        private List<Quaternion> _initRots;

        private List<Quaternion> initRots
        {
            get
            {
                if (this._initRots == null)
                {
                    this._initRots = base.module.initRots;
                }
                return this._initRots;
            }
        }
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            Part p = base.part.partInfo.partPrefab;
            List<ModuleGimbal> gimbals = p.Modules.OfType<ModuleGimbal>().ToList();
            for (int i = 0; i < gimbals.Count; i++)
            {
                ModuleGimbal gimbal = gimbals[i];
                if (gimbal.gimbalTransformName == base.gimbalTransformName)
                {
                    this.baseRange = gimbal.gimbalRange;
                    break;
                }
            }
        }
        public override void DoFailure()
        {
            base.DoFailure();
            //initRots[0] = Quaternion.
            float angle1 = UnityEngine.Random.Range(-this.baseRange, this.baseRange);
            float angle2 = UnityEngine.Random.Range(-this.baseRange, this.baseRange);
            float range = this.baseRange - Math.Max(Math.Abs(angle1), Math.Abs(angle2));
            for (int i = 0; i < base.module.initRots.Count; i++)
            {
                this.module.initRots[i] = Quaternion.AngleAxis(angle1, Vector3.forward) * Quaternion.AngleAxis(angle2, Vector3.left) * initRots[i];
            }
            base.module.gimbalRange = range;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            base.module.initRots = this.initRots;
            base.module.gimbalRange = this.baseRange;
            return 0f;
        }
    }
}

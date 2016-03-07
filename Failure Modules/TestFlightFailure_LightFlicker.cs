using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace TestFlight
{
    public class TestFlightFailure_LightFlicker : TestFlightFailureBase_Light
    {
        [KSPField]
        public float maxFlickerTime = 0.4f;
        [KSPField]
        public float minFlickerTime = 0.1f;

        private bool flicker = false;
        private bool state = false;
        private float interval;

        private System.Random random = new System.Random();
        public override void DoFailure()
        {
            base.DoFailure();
            this.flicker = true;
        }
        public override float DoRepair()
        {
            base.DoRepair();
            this.flicker = false;
            SetState(base.module.isOn);
            return 0f;
        }
        public override void OnUpdate()
        { 
            if (this.flicker && base.module != null && base.module.isOn)
            {
                this.interval -= Time.deltaTime;
                if (this.interval < 0)
                {
                    SetState(!this.state);
                    this.interval += ((float)random.NextDouble() * (this.maxFlickerTime - this.minFlickerTime)) + this.minFlickerTime;
                }
            }
        }

        public void SetState(bool newState)
        {
            this.state = newState;
            for (int i = 0; i < base.module.lights.Count; i++)
            {
                base.module.lights[i].enabled = this.state;
            }
        }
    }
}

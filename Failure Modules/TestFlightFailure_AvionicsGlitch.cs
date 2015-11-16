using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using UnityEngine;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_AvionicsGlitch : TestFlightFailureBase_Avionics
    {
        [KSPField]
        public float maxDeadtime = 1f;
        [KSPField]
        public float maxWorkTime = 1f;

        private float currentInterval = 0;
        private float currentTime = 0;
        private bool state = false;
        private Random random;

        public override void DoFailure()
        {
            base.DoFailure();
            this.random = new Random();
        }
        public override float Calculate(float value)
        {
            this.currentTime = this.currentTime + UnityEngine.Time.deltaTime;
            if (this.currentTime > this.currentInterval)
            {
                this.state = !this.state;
                this.currentTime = 0;
                this.currentInterval = (1 - (float)Math.Pow(this.random.NextDouble(), 2));
                if (this.state)
                {
                    this.currentInterval = this.currentInterval * this.maxWorkTime;
                }
                else
                {
                    this.currentInterval = this.currentInterval * this.maxDeadtime;
                }
            }
            if (!this.state)
            {
                return 0;
            }
            return value;
        }
    }
}
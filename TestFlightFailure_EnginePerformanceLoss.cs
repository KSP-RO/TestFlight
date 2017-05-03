using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_EnginePerformanceLoss : TestFlightFailure_Engine
    {
        [KSPField]
        public float ispMultiplier = 0.7f;
        [KSPField]
        public float ispMultiplierJitter = 0.1f;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            base.Startup();
            if (Failed)
                DoFailure();
        }
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            base.DoFailure();
            // for each engine change its fuelFlow which will affect thrust
            foreach (EngineHandler engine in engines)
            {
                float jitter = ispMultiplierJitter - ((float)TestFlightUtil.GetCore(this.part, Configuration).RandomGenerator.NextDouble() * (ispMultiplierJitter * 2));
                float actualMultiplier = ispMultiplier + jitter;
                engine.engine.SetFuelIspMult(actualMultiplier);
            }
        }

        public override float DoRepair()
        {
            base.DoRepair();
            // for each engine restore its fuell flow back to 1.0
            foreach (EngineHandler engine in engines)
            {
                engine.engine.SetFuelIspMult(1.0f);
            }
            return 0;
        }
    }
}


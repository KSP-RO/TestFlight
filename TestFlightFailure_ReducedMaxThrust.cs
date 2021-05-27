using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_ReducedMaxThrust : TestFlightFailure_Engine
    {
        [KSPField]
        public float thrustReduction = 0.5f;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            base.Startup();
            if (Failed)
                DoFailure();
        }
        public override void SetActiveConfig(string alias)
        {
            base.SetActiveConfig(alias);
            
            if (currentConfig == null) return;

            // update current values with those from the current config node
            currentConfig.TryGetValue("thrustReduction", ref thrustReduction);
        }

        public override void OnAwake()
        {
            base.OnAwake();
            if (!string.IsNullOrEmpty(configNodeData))
            {
                var node = ConfigNode.Parse(configNodeData);
                OnLoad(node);
            }
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
                engine.engine.SetFuelFlowMult(thrustReduction);
                engine.engine.failed = true;
                engine.engine.failMessage = failureTitle;
            }
        }

        public override float DoRepair()
        {
            base.DoRepair();
            // for each engine restore its fuell flow back to 1.0
            foreach (EngineHandler engine in engines)
            {
                engine.engine.SetFuelFlowMult(1.0f);
                engine.engine.failed = false;
                engine.engine.failMessage = "";
            }
            return 0;
        }
    }
}


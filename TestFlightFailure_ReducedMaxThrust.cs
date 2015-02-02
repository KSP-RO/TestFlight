using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_ReducedMaxThrust : TestFlightFailureBase
    {
        private float maxThrust;
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            base.DoFailure();
            List<ModuleEngines> partEngines = this.part.Modules.OfType<ModuleEngines>().ToList();
            List<ModuleEnginesFX> partEnginesFX = this.part.Modules.OfType<ModuleEnginesFX>().ToList();
            foreach (ModuleEngines engine in partEngines)
            {
                maxThrust = engine.maxThrust;
                engine.maxThrust = maxThrust * 0.5f;
            }
            foreach (ModuleEnginesFX engineFX in partEnginesFX)
            {
                maxThrust = engineFX.maxThrust;
                engineFX.maxThrust = maxThrust * 0.5f;
            }
        }

        public override double DoRepair()
        {
            base.DoRepair();
            List<ModuleEngines> partEngines = this.part.Modules.OfType<ModuleEngines>().ToList();
            List<ModuleEnginesFX> partEnginesFX = this.part.Modules.OfType<ModuleEnginesFX>().ToList();
            foreach (ModuleEngines engine in partEngines)
            {
                engine.maxThrust = maxThrust;
            }
            foreach (ModuleEnginesFX engineFX in partEnginesFX)
            {
                engineFX.maxThrust = maxThrust;
            }

            return 0;
        }
    }
}


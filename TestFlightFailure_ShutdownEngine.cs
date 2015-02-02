using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_ShutdownEngine : TestFlightFailureBase
    {
        private bool allowShutdown;
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            Debug.Log("TestFlightFailure_ShutdownEngine: Failing part");
            List<ModuleEngines> partEngines = this.part.Modules.OfType<ModuleEngines>().ToList();
            List<ModuleEnginesFX> partEnginesFX = this.part.Modules.OfType<ModuleEnginesFX>().ToList();
            foreach (ModuleEngines engine in partEngines)
            {
                allowShutdown = engine.allowShutdown;
                engine.allowShutdown = true;
                engine.Shutdown();
                engine.DeactivateRunningFX();
                engine.DeactivatePowerFX();
                engine.enabled = false;
            }
            foreach (ModuleEnginesFX engineFX in partEnginesFX)
            {
                allowShutdown = engineFX.allowShutdown;
                engineFX.allowShutdown = true;
                engineFX.Shutdown();
                engineFX.DeactivateLoopingFX();
                engineFX.enabled = false;
            }
        }

        public override double DoRepair()
        {
            List<ModuleEngines> partEngines = this.part.Modules.OfType<ModuleEngines>().ToList();
            List<ModuleEnginesFX> partEnginesFX = this.part.Modules.OfType<ModuleEnginesFX>().ToList();
            foreach (ModuleEngines engine in partEngines)
            {
                engine.enabled = true;
                engine.allowShutdown = allowShutdown;
            }
            foreach (ModuleEnginesFX engineFX in partEnginesFX)
            {
                engineFX.enabled = true;
                engineFX.allowShutdown = allowShutdown;
            }
            return 0;
        }
    }
}


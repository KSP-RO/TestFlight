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
        private EngineModuleWrapper engine;

        internal void Log(string message)
        {
            message = String.Format("TestFlightFailure_ShutDownEngine({0}[{1}]): {2}", TestFlightUtil.GetFullPartName(this.part), Configuration, message);
            TestFlightUtil.Log(message, this.part);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            engine = new EngineModuleWrapper(this.part);
        }
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            base.DoFailure();
            Log("Failing part");
            allowShutdown = engine.allowShutdown;
            engine.Shutdown();
        }

        public override double DoRepair()
        {
            base.DoRepair();
            engine.allowShutdown = allowShutdown;
            engine.enabled = true;
            return 0;
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using TestFlightAPI;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_SolarMech : TestFlightFailureBase_Solar
    {
        private ITestFlightCore core = null;
        private ModuleDeployableSolarPanel.panelStates panelState;
        private bool failureActive = false;
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            StartCoroutine("Attach");
        }

        IEnumerator Attach()
        {
            while (this.part == null || this.part.Modules == null)
                yield return null;

            while (core == null)
            {
                core = TestFlightUtil.GetCore(this.part);
                yield return null;
            }

            Startup();
        }
        public void Startup()
        {

            this.panelState = base.module.panelState;
            if (this.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED)
            {
                core.EnableFailure("TestFlightFailure_SolarMechFail");
            }
            else
            {
                core.DisableFailure("TestFlightFailure_SolarMechFail");
            }
        }
        public override void OnUpdate()
        {
            if (this.panelState != base.module.panelState)
            {
                this.panelState = base.module.panelState;
                if (TestFlightEnabled && !base.part.ShieldedFromAirstream && (this.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED) != this.failureActive)
                {
                    this.failureActive = !this.failureActive;
                    if (this.failureActive)
                    {
                        core.EnableFailure("TestFlightFailure_SolarMechFail");
                    }
                    else
                    {
                        core.DisableFailure("TestFlightFailure_SolarMechFail");
                    }
                }
            }
        }
        public override void DoFailure()
        {
            base.DoFailure();
            base.module.breakPanels();
        }
        public override bool CanAttemptRepair()
        {
            return false;
        }
        public override float DoRepair()
        {
            return -1f;
        }
    }
}
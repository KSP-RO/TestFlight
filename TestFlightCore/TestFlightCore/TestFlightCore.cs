using System;
using System.Collections.Generic;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlightCore
{
    /// <summary>
    /// This is the core PartModule of the TestFlight system, and is the module that everythign else plugins into.
    /// All relevant data for working in the system, as well as all usable API methods live here.
    /// </summary>
    public class TestFlightCore : PartModuleWindow
    {
        private int lastFailureCheck = 0;
        private float lastPolling = 0.0f;
        private TestFlightData currentFlightData;
        private float currentReliability = 0.0f;
        private TestFlightManagerScenario tsm;

        [KSPField(isPersistant = true)]
        public int failureCheckFrequency = 120;
        [KSPField(isPersistant = true)]
        public float pollingInterval = 5.0f;

        [KSPEvent(guiActive = true, guiName = "Toggle TestFlight Debug GUI")]
        public void ToggleDebugGUI()
        {
            Visible = !Visible;
        }

        public override void OnAwake()
        {
            Debug.Log("TestFlightCore: OnAwake()");
            base.OnAwake();
            WindowCaption = "TestFlight";
            WindowRect = new Rect(0, 0, 250, 50);
            Visible = false;
            DragEnabled = true;
            var game = HighLogic.CurrentGame;
            ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(TestFlightManagerScenario).Name);
            if (psm != null)
            {
                tsm = (TestFlightManagerScenario)psm.moduleRef;
            }
        }

        public override void OnUpdate()
        {
            // Check to see if its time to poll
            float currentMet = (float)FlightLogger.met;
            if (currentMet > (lastPolling + pollingInterval))
            {
                // Poll all compatible modules in this order:
                // 1) FlightDataRecorder
                // 2) TestFlightReliability
                // 2A) Determine final computed reliability
                // 3) Determine if failure poll should be done
                // 3A) If yes, roll for failure check
                // 3B) If failure occurs, roll to determine which failure module
                foreach (PartModule pm in this.part.Modules)
                {
                    IFlightDataRecorder fdr = pm as IFlightDataRecorder;
                    if (fdr != null)
                    {
                        currentFlightData = fdr.GetCurrentFlightData();
                        break;
                    }
                }
                float totalReliability = 0.0f;
                foreach (PartModule pm in this.part.Modules)
                {
                    ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                    if (reliabilityModule != null)
                    {
                        totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(currentFlightData);
                    }
                }
                currentReliability = totalReliability;
                lastPolling = currentMet;
            }
            if (FlightLogger.met_secs > (lastFailureCheck + failureCheckFrequency))
            {
                lastFailureCheck = FlightLogger.met_secs;
            }
        }

        internal override void DrawWindow(int id)
        {
			GUILayout.Label(String.Format("TestFlight Debug for {0}", this.part.name));
            GUILayout.Label(String.Format("flight data:{0:F0}", currentFlightData.flightData));
            GUILayout.Label(String.Format("flight time:{0:D}", currentFlightData.flightTime));
            GUILayout.Label(String.Format("reliability:{0:F2}", currentReliability));
        }
    }
}


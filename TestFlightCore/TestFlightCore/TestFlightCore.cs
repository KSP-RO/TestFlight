using System;
using System.Collections.Generic;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlightCore
{
    /// <summary>
    /// This is the core PartModule of the TestFlight system, and is the module that everything else plugins into.
    /// All relevant data for working in the system, as well as all usable API methods live here.
    /// </summary>
    public class TestFlightCore : PartModuleWindow, ITestFlightCore
    {
        private float lastFailureCheck = 0f;
        private float lastPolling = 0.0f;
        private TestFlightData currentFlightData;
        private double currentReliability = 0.0f;
        private List<ITestFlightFailure> failureModules = null;
        private ITestFlightFailure activeFailure = null;

        [KSPField(isPersistant = true)]
        public float failureCheckFrequency = 0f;
        [KSPField(isPersistant = true)]
        public float pollingInterval = 0f;

        [KSPEvent(guiActive = true, guiName = "Toggle TestFlight Debug GUI")]
        public void ToggleDebugGUI()
        {
            Visible = !Visible;
        }

        [KSPEvent(guiActive = true, guiName = "Attempt Part Repair")]
        public void AttemptRepair()
        {
            if (activeFailure == null)
                return;

            if (activeFailure.CanAttemptRepair())
            {
                bool isRepaired = activeFailure.AttemptRepair();
                if (isRepaired)
                {
                    activeFailure = null;
                }
            }
        }

        public override void OnAwake()
        {
            base.OnAwake();
            WindowCaption = "TestFlight";
            WindowRect = new Rect(0, 0, 250, 50);
            Visible = false;
            DragEnabled = true;
        }

        public override void OnStart(StartState state)
        {
            if (failureModules == null)
            {
                failureModules = new List<ITestFlightFailure>();
            }
            failureModules.Clear();
            foreach(PartModule pm in this.part.Modules)
            {
                ITestFlightFailure failureModule = pm as ITestFlightFailure;
                if (failureModule != null)
                {
                    failureModules.Add(failureModule);
                }
            }
            base.OnStart(state);
        }

        public virtual TestFlightData GetCurrentFlightData()
        {
            return currentFlightData;
        }

        public virtual int GetPartStatus()
        {
            if (activeFailure == null)
                return 0;

            if (activeFailure.GetFailureDetails().severity == "minor")
                return 1;
            if (activeFailure.GetFailureDetails().severity == "failure")
                return 1;
            if (activeFailure.GetFailureDetails().severity == "major")
                return 1;

            return -1;
        }

        public virtual ITestFlightFailure GetFailureModule()
        {
            return activeFailure;
        }

        public virtual void DoFlightUpdate(double missionStartTime, double flightDataMultiplier, double flightDataEngineerMultiplier, double globalReliabilityModifier)
        {
            // Check to see if its time to poll
            double totalReliability = 0.0;
            float currentMet = (float)(Planetarium.GetUniversalTime() - missionStartTime);
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
                        fdr.DoFlightUpdate(missionStartTime, flightDataMultiplier, flightDataEngineerMultiplier);
                        currentFlightData = fdr.GetCurrentFlightData();
                        break;
                    }
                }
                foreach (PartModule pm in this.part.Modules)
                {
                    ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                    if (reliabilityModule != null)
                    {
                        totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(currentFlightData);
                    }
                }
                currentReliability = totalReliability * globalReliabilityModifier;
                lastPolling = currentMet;
            }
        }

        public virtual bool DoFailureCheck(double missionStartTime, double globalReliabilityModifier)
        {
            float totalReliability = 0.0f;
            float currentMet = (float)(Planetarium.GetUniversalTime() - missionStartTime);
            foreach (PartModule pm in this.part.Modules)
            {
                ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                if (reliabilityModule != null)
                {
                    totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(currentFlightData);
                }
            }
            currentReliability = totalReliability;
            if ( currentMet > (lastFailureCheck + failureCheckFrequency) && activeFailure == null )
            {
                lastFailureCheck = currentMet;
                // Roll for failure
                float roll = UnityEngine.Random.Range(0.0f,100.0f);
                if (roll > totalReliability)
                {
                    // Failure occurs.  Determine which failure module to trigger
                    int totalWeight = 0;
                    int currentWeight = 0;
                    int chosenWeight = 0;
                    foreach(ITestFlightFailure fm in failureModules)
                    {
                        totalWeight += fm.GetFailureDetails().weight;
                    }
                    Debug.Log("TestFlightCore: Total Weight " + totalWeight);
                    chosenWeight = UnityEngine.Random.Range(1,totalWeight);
                    Debug.Log("TestFlightCore: Chosen Weight " + chosenWeight);
                    foreach(ITestFlightFailure fm in failureModules)
                    {
                        currentWeight += fm.GetFailureDetails().weight;
                        if (currentWeight >= chosenWeight)
                        {
                            // Trigger this module's failure
                            Debug.Log("TestFlightCore: Triggering failure on " + fm);
                            activeFailure = fm;
                            fm.DoFailure();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override void OnUpdate()
        {
//            Debug.Log("TestFlightCore: OnUpdate()");
        }

        internal override void DrawWindow(int id)
        {
			GUILayout.Label(String.Format("TestFlight Debug for {0}", this.part.name));
            GUILayout.Label(String.Format("flight data:{0:F0}", currentFlightData.flightData));
            GUILayout.Label(String.Format("flight data scope:{0}", currentFlightData.scope));
            GUILayout.Label(String.Format("flight time:{0:D}", currentFlightData.flightTime));
            GUILayout.Label(String.Format("reliability:{0:F2}", currentReliability));
            if (activeFailure != null)
            {
                GUILayout.Label(String.Format("active failure:{0}", activeFailure.GetFailureDetails().failureTitle));
            }
        }
    }
}


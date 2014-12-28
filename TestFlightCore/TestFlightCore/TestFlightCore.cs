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
        private List<TestFlightData> initialFlightData;
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

        public bool AttemptRepair()
        {
            if (activeFailure == null)
                return true;

            if (activeFailure.CanAttemptRepair())
            {
                bool isRepaired = activeFailure.AttemptRepair();
                if (isRepaired)
                {
                    activeFailure = null;
                    return true;
                }
            }
            return false;
        }

        public void HighlightPart(bool doHighlight)
        {
            Color highlightColor;
            if (activeFailure == null)
                highlightColor = XKCDColors.HighlighterGreen;
            else
            {
                if (activeFailure.GetFailureDetails().severity == "major")
                    highlightColor = XKCDColors.FireEngineRed;
                else
                    highlightColor = XKCDColors.OrangeYellow;
            }

            if (doHighlight)
            {
                this.part.SetHighlightDefault();
                this.part.SetHighlightType(Part.HighlightType.AlwaysOn);
                this.part.SetHighlight(true, false);
                this.part.SetHighlightColor(highlightColor);
                HighlightingSystem.Highlighter highlighter;
                highlighter = this.part.FindModelTransform("model").gameObject.AddComponent<HighlightingSystem.Highlighter>();
                highlighter.ConstantOn(highlightColor);
                highlighter.SeeThroughOn();
            }
            else
            {
                this.part.SetHighlightDefault();
                this.part.SetHighlightType(Part.HighlightType.AlwaysOn);
                this.part.SetHighlight(false, false);
                this.part.SetHighlightColor(XKCDColors.HighlighterGreen);
                Destroy(this.part.FindModelTransform("model").gameObject.GetComponent(typeof(HighlightingSystem.Highlighter)));
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

        public virtual double GetCurrentReliability(double globalReliabilityModifier)
        {
            // Calculate reliability based on initial flight data, not current
            double totalReliability = 0.0;
            string scope;
            IFlightDataRecorder dataRecorder = null;
            foreach (PartModule pm in this.part.Modules)
            {
                IFlightDataRecorder fdr = pm as IFlightDataRecorder;
                if (fdr != null)
                {
                    dataRecorder = fdr;
                }
            }
            foreach (PartModule pm in this.part.Modules)
            {
                ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                if (reliabilityModule != null)
                {
                    scope = String.Format("{0}_{1}", dataRecorder.GetDataBody(), dataRecorder.GetDataSituation());
                    TestFlightData flightData;
                    if (initialFlightData == null)
                    {
                        Debug.Log("TestFlightCore: initialFlightData is null");
                        flightData = new TestFlightData();
                        flightData.scope = scope;
                        flightData.flightData = 0.0f;
                    }
                    else
                    {
                        Debug.Log("TestFlightCore: initialFlightData is valid");
                        foreach (TestFlightData tfd in initialFlightData)
                        {
                            Debug.Log("TestFlightCore: initialFlightData " + tfd.flightData);
                        }
                        flightData = initialFlightData.Find(fd => fd.scope == scope);
                    }
                    Debug.Log("TestFlightCore: Doing Reliability check with flightData " + flightData.flightData);
                    totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(flightData);
                }
            }
            currentReliability = totalReliability * globalReliabilityModifier;
            return currentReliability;
        }

        public void InitializeFlightData(List<TestFlightData> allFlightData, double globalReliabilityModifier)
        {
            Debug.Log("TestFlightCore: " + this.part.name + "(" + this.part.flightID + ") Initializing");
            initialFlightData = new List<TestFlightData>(allFlightData);
            double totalReliability = 0.0;
            string scope;
            IFlightDataRecorder dataRecorder = null;
            foreach (PartModule pm in this.part.Modules)
            {
                IFlightDataRecorder fdr = pm as IFlightDataRecorder;
                if (fdr != null)
                {
                    dataRecorder = fdr;
                    fdr.InitializeFlightData(allFlightData);
                    currentFlightData = fdr.GetCurrentFlightData();
                }
            }
            // Calculate reliability based on initial flight data, not current
            foreach (PartModule pm in this.part.Modules)
            {
                ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                if (reliabilityModule != null)
                {
                    scope = String.Format("{0}_{1}", dataRecorder.GetDataBody(), dataRecorder.GetDataSituation());
                    TestFlightData flightData;
                    if (initialFlightData == null)
                    {
                        flightData = new TestFlightData();
                        flightData.scope = scope;
                        flightData.flightData = 0.0f;
                    }
                    else
                    {
                        foreach (TestFlightData tfd in initialFlightData)
                        {
                            Debug.Log("TestFlightCore: initialFlightData " + tfd.flightData);
                        }
                        flightData = initialFlightData.Find(fd => fd.scope == scope);
                    }
                    totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(flightData);
                }
            }
            currentReliability = totalReliability * globalReliabilityModifier;
        }


        public virtual void DoFlightUpdate(double missionStartTime, double flightDataMultiplier, double flightDataEngineerMultiplier, double globalReliabilityModifier)
        {
            Debug.Log("TestFlightCore: " + this.part.name + "(" + this.part.flightID + ") FlightUpdate");
            // Check to see if its time to poll
            IFlightDataRecorder dataRecorder = null;
            string scope;
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
                        dataRecorder = fdr;
                        fdr.DoFlightUpdate(missionStartTime, flightDataMultiplier, flightDataEngineerMultiplier);
                        currentFlightData = fdr.GetCurrentFlightData();
                        break;
                    }
                }
                // Calculate reliability based on initial flight data, not current
                double totalReliability = 0.0;
                foreach (PartModule pm in this.part.Modules)
                {
                    ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                    if (reliabilityModule != null)
                    {
                        scope = String.Format("{0}_{1}", dataRecorder.GetDataBody(), dataRecorder.GetDataSituation());
                        TestFlightData flightData;
                        if (initialFlightData == null)
                        {
                            Debug.Log("TestFlightCore: initialFlightData is null");
                            flightData = new TestFlightData();
                            flightData.scope = scope;
                            flightData.flightData = 0.0f;
                        }
                        else
                        {
                            Debug.Log("TestFlightCore: initialFlightData is valid");
                            foreach (TestFlightData tfd in initialFlightData)
                            {
                                Debug.Log("TestFlightCore: initialFlightData " + tfd.flightData);
                            }
                            flightData = initialFlightData.Find(fd => fd.scope == scope);
                        }
                        Debug.Log("TestFlightCore: Doing Reliability check with flightData " + flightData.flightData);
                        totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(flightData);
                    }
                }
                currentReliability = totalReliability * globalReliabilityModifier;
                lastPolling = currentMet;
            }
        }

        public virtual bool DoFailureCheck(double missionStartTime, double globalReliabilityModifier)
        {
            string scope;
            IFlightDataRecorder dataRecorder = null;
            float currentMet = (float)(Planetarium.GetUniversalTime() - missionStartTime);
            if ( currentMet > (lastFailureCheck + failureCheckFrequency) && activeFailure == null )
            {
                lastFailureCheck = currentMet;
                // Calculate reliability based on initial flight data, not current
                double totalReliability = 0.0;
                foreach (PartModule pm in this.part.Modules)
                {
                    IFlightDataRecorder fdr = pm as IFlightDataRecorder;
                    if (fdr != null)
                    {
                        dataRecorder = fdr;
                        break;
                    }
                }
                foreach (PartModule pm in this.part.Modules)
                {
                    ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                    if (reliabilityModule != null)
                    {
                        scope = String.Format("{0}_{1}", dataRecorder.GetDataBody(), dataRecorder.GetDataSituation());
                        TestFlightData flightData;
                        if (initialFlightData == null)
                        {
                            flightData = new TestFlightData();
                            flightData.scope = scope;
                            flightData.flightData = 0.0f;
                        }
                        else
                            flightData = initialFlightData.Find(fd => fd.scope == scope);
                        Debug.Log("TestFlightCore: Doing Failure check with flightData " + flightData.flightData);
                        totalReliability = totalReliability + reliabilityModule.GetCurrentReliability(flightData);
                    }
                }
                currentReliability = totalReliability * globalReliabilityModifier;
                // Roll for failure
                float roll = UnityEngine.Random.Range(0.0f,100.0f);
                Debug.Log("TestFlightCore: " + this.part.name + "(" + this.part.flightID + ") Reliability " + currentReliability + ", Failure Roll " + roll);
                if (roll > currentReliability)
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
        }

        internal override void DrawWindow(int id)
        {
            if (this.part == null)
            {
                Visible = false;
                return;
            }

            GUILayout.Label(String.Format("TestFlight Debug for {0}({1})", this.part.name, this.part.flightID));
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


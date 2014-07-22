using System;
using System.Collections.Generic;


using UnityEngine;
using KSPPluginFramework;

namespace TestFlight
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class TestFlightManager : MonoBehaviour
    {
        public void Start()
        {
            var game = HighLogic.CurrentGame;
            ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(TestFlightManagerScenario).Name);
            if (psm == null)
            {
                GameScenes[] desiredScenes = new GameScenes[4] { GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPH };
                psm = game.AddProtoScenarioModule(typeof(TestFlightManagerScenario), desiredScenes);
            }
        }
    }

    public class TestFlightManagerScenario : ScenarioModule
    {
        [KSPField(isPersistant = true)]
        public string aSavedValue = "the default value";

        public override void OnAwake()
        {
            Debug.Log("TestFlightManagerScanario: OnAwake()");
            base.OnAwake();
        }
        public void Update()
        {
        }
        public override void OnSave(ConfigNode node)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("TestFlight: Saving in FLIGHT scene");
                foreach (Vessel vessel in FlightGlobals.Vessels)
                {
                    foreach (Part part in vessel.parts)
                    {
                        PartModuleList pmList = part.Modules;
                        print("Dumping part modules");
                        foreach (PartModule pm in pmList)
                        {
                            if (pm.moduleName == "FlightDataRecorder")
                            {
                                FlightDataRecorder fdr = (FlightDataRecorder)pm;
                            }
                        }
                    }
                }
            }
            Debug.Log("TestFlight: Scenario Saved");
        }

    }
    public class FlightDataBody : IConfigNode
    {
        [KSPField(isPersistant = true)]
        public string bodyName;
        [KSPField(isPersistant = true)]
        public float dataAtmosphere;
        [KSPField(isPersistant = true)]
        public float dataSpace;

        public void Load(ConfigNode node)
        {
            Debug.Log("FlightDataBody Load");
            Debug.Log(node.ToString());
            if (node.HasValue("bodyName"))
                bodyName = node.GetValue("bodyName");
            else
                bodyName = "DEFAULTBODY";

            if (node.HasValue("dataAtmosphere"))
                dataAtmosphere = float.Parse(node.GetValue("dataAtmosphere"));
            else
                dataAtmosphere = 0.0f;

            if (node.HasValue("dataSpace"))
                dataSpace = float.Parse(node.GetValue("dataSpace"));
            else
                dataSpace = 0.0f;
        }

        public void Save(ConfigNode node)
        {
            Debug.Log("FlightDataBody Save");
            Debug.Log(node.ToString());
            node.AddValue("bodyName", bodyName);
            node.AddValue("dataAtmosphere", dataAtmosphere);
            node.AddValue("dataSpace", dataSpace);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class FlightData : IConfigNode
    {
        [KSPField(isPersistant=true)]
        public float dataDeepSpace;

        List<FlightDataBody> dataBodies;

        /// <summary>
        /// Returns the ConfigNode containing flight data for the given body
        /// </summary>
        /// <param name="name">The requested ConfigNode</param>
        /// <returns></returns>
        public FlightDataBody GetBodyData(string name)
        {
            if (dataBodies == null)
                return null;

            return dataBodies.Find(s => s.bodyName == name);
        }
        
        /// <summary>
        /// Adds flight data for the given body.  Creates new entry if one doesn't yet exist, or updates one if it exists
        /// </summary>
        /// <param name="name">Name of the body as given by Vessel.mainBody.name</param>
        /// <param name="atmosphere">Flight data value for Atmosphere</param>
        /// <param name="space">Flight data value for Space</param>
        public void AddBodyData(string name, float atmosphere, float space)
        {
            if (dataBodies == null)
            {
                dataBodies = new List<FlightDataBody>();
                FlightDataBody body = new FlightDataBody();
                body.bodyName = name;
                body.dataAtmosphere = atmosphere;
                body.dataSpace = space;
                dataBodies.Add(body);
            }
            else
            {
                FlightDataBody body = dataBodies.Find(s => s.bodyName == name);
                if (body != null)
                {
                    body.dataAtmosphere = atmosphere;
                    body.dataSpace = space;
                }
                else
                {
                    body = new FlightDataBody();
                    body.bodyName = name;
                    body.dataAtmosphere = atmosphere;
                    body.dataSpace = space;
                    dataBodies.Add(body);
                }
            }
        }

        public void Load(ConfigNode node)
        {
            Debug.Log("FlightData Load");
            Debug.Log(node.ToString());
            if (node.HasValue("dataDeepSpace"))
            {
                dataDeepSpace = float.Parse(node.GetValue("dataDeepSpace"));
                Debug.Log("Loaded dataDeepSpace value of " + dataDeepSpace.ToString());
            }
            else
            {
                Debug.Log("Could not find dataDeepSpace node");
                dataDeepSpace = 0.0f;
            }

            if (node.HasNode("bodyData"))
            {
                if (dataBodies == null)
                    dataBodies = new List<FlightDataBody>();
                else
                    dataBodies.Clear();
                foreach (ConfigNode bodyNode in node.GetNodes("bodyData"))
                {
                    FlightDataBody body = new FlightDataBody();
                    body.Load(bodyNode);
                    dataBodies.Add(body);
                }
            }
        }

        public void Save(ConfigNode node)
        {
            Debug.Log("FlightData Save");
            Debug.Log(node.ToString());
            node.AddValue("dataDeepSpace", dataDeepSpace);
            if (dataBodies != null)
            {
                foreach (FlightDataBody body in dataBodies)
                {
                    ConfigNode bodyNode = node.AddNode("bodyData");
                    body.Save(bodyNode);
                }
            }
            Debug.Log(node.ToString());
        }
    }

    // Method for determing distance from kerbal to part
    // float kerbalDistanceToPart = Vector3.Distance(kerbal.transform.position, targetPart.collider.ClosestPointOnBounds(kerbal.transform.position));

    public class FlightDataRecorder : PartModuleWindow
    {
        private float currentDataDeepSpace = 0.0f;
        private float currentBodyDataAtmosphere = 0.0f;
        private float currentBodyDataSpace = 0.0f;

        #region KSPFields
        [KSPField(isPersistant = true)]
        public FlightData flightData;
        [KSPField(isPersistant = true)]
        public string currentBody = "DEFAULTBODY";
        [KSPField(isPersistant = true)]
        public float initialDataDeepSpace = 0.0f;
        [KSPField(isPersistant = true)]
        public float initialDataAtmosphere = 0.0f;
        [KSPField(isPersistant = true)]
        public float initialDataSpace = 0.0f;
        [KSPField(isPersistant = true)]
        public float initialKerbinDataAtmosphere = 0.0f;
        [KSPField(isPersistant = true)]
        public float initialKerbinDataSpace = 0.0f;
        [KSPField(isPersistant = true)]
        public int reliabilityFactor = 3;
        [KSPField(isPersistant = true)]
        public float reliabilityMultiplier = 2.0f;
        [KSPField(isPersistant = true)]
        public float maximumReliabilityDeepSpace = 93.0f;
        [KSPField(isPersistant = true)]
        public float maximumReliabilityAtmosphere = 99.5f;
        [KSPField(isPersistant = true)]
        public float maximumReliabilitySpace = 98.0f;
        [KSPField(isPersistant = true)]
        public float minorFailureThreshold = 25.0f;
        [KSPField(isPersistant = true)]
        public float majorFailureThreshold = 85.0f;
        #endregion

        TestFlightManagerScenario tsm;

        [KSPEvent(guiActive=true, guiName="Toggle FlightData GUI")]
        public void ToggleDebugGUI()
        {
            Visible = !Visible;
        }

        public string GetDataSituation()
        {
            string situation = this.vessel.situation.ToString().ToLower();
            // Determine if we are recording data in SPACE or ATMOSHPHERE
            if (situation == "sub_orbital" || situation == "orbiting" || situation == "escaping")
                if (this.vessel.altitude > 1000000)
                    situation = "deep-space";
                else
                    situation = "space";
            else if (situation == "flying")
                situation = "atmosphere";
            else
                situation = null;

            return situation;
        }

        public bool IsRecordingFlightData()
        {
            bool isRecording = true;

            if (!isEnabled)
                return false;

            // ModuleEngines
            if (this.part.Modules.Contains("ModuleEngines"))
            {
                ModuleEngines engine = (ModuleEngines)this.part.Modules["ModuleEngines"];
                if (!engine.isOperational)
                    return false;
                if (engine.normalizedThrustOutput <= 0)
                    return false;
                if (engine.finalThrust <= 0)
                    return false;
            }
            // ModuleEnginesFX
            if (this.part.Modules.Contains("ModuleEnginesFX"))
            {
                ModuleEnginesFX engine = (ModuleEnginesFX)this.part.Modules["ModuleEnginesFX"];
                if (!engine.isOperational)
                    return false;
                if (engine.normalizedThrustOutput <= 0)
                    return false;
                if (engine.finalThrust <= 0)
                    return false;
            }

            return isRecording;
        }
        public override void OnAwake()
        {
            base.OnAwake();
            WindowCaption = "FlightData";
            WindowRect = new Rect(0, 0, 250, 50);
            Visible = false;
            DragEnabled = true;
            //var game = HighLogic.CurrentGame;
            //ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(TestFlightManagerScenario).Name);
            //if (psm != null)
            //{
            //    tsm = (TestFlightManagerScenario)psm.moduleRef;
            //}
            if (flightData == null)
                flightData = new FlightData();
            // Try getting defaults from the GameDatabase
            print("OnAwake() - Looking through GameDatabase");
            foreach (UrlDir.UrlConfig url in GameDatabase.Instance.GetConfigs("PART"))
            {
                if (url.name == this.part.name)
                {
                    print("Found match");
                    print(url.config.ToString());
                    ConfigNode[] flightDataNode = url.config.GetNodes("flightData");
                    print(flightDataNode.ToString());
                }
            }
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            print("FlightDataRecorder: OnLoad");
            print("FlightDataRecorder: " + node.ToString());
        }

        public override void OnStart(StartState state)
        {
            print("FlightDataRecorder: onStart()");
            print("FlightDataRecorder: State = " + state);
        }
        public override void OnUpdate()
        {
        }

        public override void OnSave(ConfigNode node)
        {
            print("FlightDataRecorder: onSave()");
            // Make sure our FlightData configs are up to date
            flightData.AddBodyData(currentBody, currentBodyDataAtmosphere, currentBodyDataSpace);
            base.OnSave(node);
        }

        public override void OnFixedUpdate()
        {
            if (!IsRecordingFlightData())
                return;

            string bodyName = this.vessel.mainBody.name;
            
            // Check to see if we have changed bodies
            if (bodyName != currentBody)
            {
                // If we have moved to a new body then we need to reset our current data counters
                // First save what we have for the old body
                flightData.AddBodyData(currentBody, currentBodyDataAtmosphere, currentBodyDataSpace);
                // Try to get any existing stored data for this body, or set it to 0
                FlightDataBody bodyData = flightData.GetBodyData(bodyName);
                if (bodyData != null)
                {
                    currentBodyDataAtmosphere = bodyData.dataAtmosphere;
                    currentBodyDataSpace = bodyData.dataSpace;
                }
                else
                {
                    currentBodyDataAtmosphere = 0.0f;
                    currentBodyDataSpace = 0.0f;
                }
                // move to the new body
                currentBody = bodyName;
            }

            string situation = GetDataSituation();
            // Drop out if we don't have a valid situation (This would be things like landed, pre-launch, or splashed down)
            if (situation == null)
                return;

            if (situation == "atmosphere")
                currentBodyDataAtmosphere = currentBodyDataAtmosphere + 1;
            else if (situation == "space")
                currentBodyDataSpace = currentBodyDataSpace + 1;
            else if (situation == "deep-space")
                currentDataDeepSpace = currentDataDeepSpace + 1;

        }
        public override void OnActive()
        {
            print("FlightDataRecorder: onActive");
        }
        public override void OnInactive()
        {
            print("FlightDataRecorder: onInactive");
        }
        internal override void DrawWindow(int id)
        {
            string rawSituation = this.vessel.situation.ToString().ToLower();
            string rawBody = this.vessel.mainBody.name;
            string situation = GetDataSituation();


            GUILayout.Label(String.Format("FlightData Details for {0}", this.part.name));
            GUILayout.Label(String.Format("Raw Body:{0}", rawBody));
            GUILayout.Label(String.Format("Raw Situation:{0}", rawSituation));
            GUILayout.Label(String.Format("Altitude:{0} (Threshold 1,000,000)", this.vessel.altitude.ToString()));
            GUILayout.Label(String.Format("Current Body:{0}", currentBody));
            GUILayout.Label(String.Format("Current Situation:{0}", situation));
            GUILayout.Label(String.Format("Current Data - Deep Space:{0}", currentDataDeepSpace.ToString()));
            GUILayout.Label(String.Format("Current Data - Atmosphere:{0}", currentBodyDataAtmosphere.ToString()));
            GUILayout.Label(String.Format("Current Data - Space:{0}", currentBodyDataSpace.ToString()));
            // This value is here for a quickie test - this needs to be done proper
            double reliabilityAtmosphere = Math.Pow(currentBodyDataAtmosphere * reliabilityMultiplier, 1.0 / reliabilityFactor);
            double reliabilitySpace = Math.Pow(currentBodyDataSpace * reliabilityMultiplier, 1.0 / reliabilityFactor);
            double reliabilityDeepSpace = Math.Pow(currentDataDeepSpace * reliabilityMultiplier, 1.0 / reliabilityFactor);
            GUILayout.Label(String.Format("{0} Atmosphere Reliability:{1}", currentBody, reliabilityAtmosphere.ToString()));
            GUILayout.Label(String.Format("{0} Space Reliability:{1}", currentBody, reliabilitySpace.ToString()));
            GUILayout.Label(String.Format("Deep Space Reliability:{0}", reliabilityDeepSpace.ToString()));

            //GUILayout.Label(String.Format("Drag Enabled:{0}", DragEnabled.ToString()));
            //GUILayout.Label(String.Format("ClampToScreen:{0}", ClampToScreen.ToString()));
            //GUILayout.Label(String.Format("Tooltips:{0}", TooltipsEnabled.ToString()));

            //if (GUILayout.Button("Toggle Drag"))
            //    DragEnabled = !DragEnabled;
            //if (GUILayout.Button("Toggle Screen Clamping"))
            //    ClampToScreen = !ClampToScreen;

            //if (GUILayout.Button(new GUIContent("Toggle Tooltips", "Can you see my Tooltip?")))
            //    TooltipsEnabled = !TooltipsEnabled;
            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Max Tooltip Width");
            //TooltipMaxWidth = Convert.ToInt32(GUILayout.TextField(TooltipMaxWidth.ToString()));
            //GUILayout.EndHorizontal();
            //GUILayout.Label("Width of 0 means no limit");
        }
    }
}

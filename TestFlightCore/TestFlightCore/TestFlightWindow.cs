using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using KSPPluginFramework;

namespace TestFlightCore
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class TestFlightWindow : MonoBehaviourWindowPlus
    {
        private TestFlightManagerScenario tfScenario;
        private ApplicationLauncherButton appLauncherButton;
        private bool stickyWindow;
        private Settings settings = null;

        private DropDownList ddlCurrentView = null;

        internal override void Start()
        {
            var game = HighLogic.CurrentGame;
            ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(TestFlightManagerScenario).Name);
            if (psm == null)
            {
                GameScenes[] desiredScenes = new GameScenes[4] { GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER };
                psm = game.AddProtoScenarioModule(typeof(TestFlightManagerScenario), desiredScenes);
            }
            psm.Load(ScenarioRunner.fetch);
            tfScenario = game.scenarios.Select(s => s.moduleRef).OfType<TestFlightManagerScenario>().SingleOrDefault();
            base.Start();
        }

        internal override void Awake()
        {
            if (settings == null)
            {
                settings = new Settings("../settings.cgf");
            }
            string assemblyPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = System.IO.Path.Combine(assemblyPath, "../settings.cfg").Replace("\\","/");
            if (!System.IO.File.Exists(filePath))
            {
                settings.flightDataEngineerMultiplier = 1.0;
                settings.flightDataMultiplier = 1.0;
                settings.globalReliabilityModifier = 1.0;
                settings.minTimeBetweenDataPoll = 0.5;
                settings.minTimeBetweenFailurePoll = 60;
                settings.processAllVessels = false;
                settings.masterStatusUpdateFrequency = 10;
                settings.currentView = 0;
                settings.Save();
            }
            settings.Load();
            StartCoroutine("AddToToolbar");
            // Start up our UI Update worker
            StartRepeatingWorker(2);
            base.Awake();
        }

        internal override void OnGUIOnceOnly()
        {
            Styles.Init();
            SkinsLibrary.SetCurrent("Default");
            // Default position and size -- will get proper bounds calculated when needed
            WindowRect = new Rect(0, 50, 500, 50);
            DragEnabled = true;
            ClampToScreen = true;
            TooltipsEnabled = true;
            WindowCaption = "TestFlight Master Status Display";
            List<string> views = new List<string>()
            {
                "Vessel Status",
                "Settings"
            };
            ddlCurrentView = new DropDownList(views, this);
            ddlManager.AddDDL(ddlCurrentView);

        }

        internal void CalculateWindowBounds()
        {
            if (appLauncherButton == null)
                return;
            if (tfScenario == null)
                return;
            float windowWidth = 650f;
            float left = Screen.width - windowWidth;
            float windowHeight = 100f;
            float top = 40f;

            // Calculate height based on amount of parts
            Dictionary<Guid, MasterStatusItem> masterStatus = tfScenario.GetMasterStatus();

            if (masterStatus != null && masterStatus.Count() > 0)
            {
                Guid currentVessel = masterStatus.First().Key;
                windowHeight += masterStatus[currentVessel].allPartsStatus.Count() * 20f;
            }
            if (!ApplicationLauncher.Instance.IsPositionedAtTop)
            {
                top = Screen.height - windowHeight - 40f;
            }

            WindowRect = new Rect(left, top, windowWidth, windowHeight);
        }

        IEnumerator AddToToolbar()
        {
            while (!ApplicationLauncher.Ready)
            {
                yield return null;
            }
            try
            {
                // Load the icon for the button
                Debug.Log("TestFlight MasterStatusDisplay: Loading icon texture");
                Texture iconTexture = GameDatabase.Instance.GetTexture("TestFlight/Resources/AppLauncherIcon", false);
                if (iconTexture == null)
                {
                    throw new Exception("TestFlight MasterStatusDisplay: Failed to load icon texture");
                }
                Debug.Log("TestFlight MasterStatusDisplay: Creating icon on toolbar");
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
                    OpenWindow,
                    CloseWindow,
                    HoverInButton,
                    HoverOutButton,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.ALWAYS,
                    iconTexture);
                ApplicationLauncher.Instance.AddOnHideCallback(HideButton);
                ApplicationLauncher.Instance.AddOnRepositionCallback(RepostionWindow);
            }
            catch (Exception e)
            {
                Debug.Log("TestFlight MasterStatusDisplay: Unable to add button to application launcher: " + e.Message);
                throw e;
            }
        }
        void PrepareWindowState()
        {
            return;
        }

        void OpenWindow()
        {
            CalculateWindowBounds();
            PrepareWindowState();
            Visible = true;
            stickyWindow = true;
        }
        void CloseWindow()
        {
            Visible = false;
            stickyWindow = false;
        }
        void HideButton()
        {
            ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
        }
        void RepostionWindow()
        {
            CalculateWindowBounds();
            Debug.Log("TestFlight MasterStatusDisplay: RepositionWindow");
        }
        void HoverInButton()
        {
            CalculateWindowBounds();
            PrepareWindowState();
            Visible = true;
        }
        void HoverOutButton()
        {
            if (!stickyWindow)
                Visible = false;
        }
        internal override void RepeatingWorker()
        {
            if (!Visible)
                return;
            // We update the window bounds here, around twice a second, instead of in the GUI draw
            // This way for one it will cause less overhead, and also shouldn't cause as much flashing
            LogFormatted_DebugOnly("Recalculating Window Bounds");
            CalculateWindowBounds();

            base.RepeatingWorker();
        }
        internal override void DrawWindow(Int32 id)
        {
            GUILayout.BeginVertical();
            Dictionary<Guid, MasterStatusItem> masterStatus = tfScenario.GetMasterStatus();
            if (masterStatus == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Button(TestFlight.Resources.btnChevronDown);
                GUILayout.Label("TestFlight is starting up...");
                GUILayout.EndHorizontal();
            }
            else if (masterStatus.Count() <= 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Button(new GUIContent(TestFlight.Resources.btnChevronLeft, "Open Settings Pane"));
                GUILayout.Label("TestFlight is not currently tracking any vessels");
                GUILayout.EndHorizontal();
            }
            else
            {
                // Display information on active vessel
                Guid currentVessl = FlightGlobals.ActiveVessel.id;
                GUILayout.BeginHorizontal();
                GUILayout.Button(TestFlight.Resources.btnChevronDown);
                GUILayout.Label("MSD for " + masterStatus[currentVessl].vesselName);
                GUILayout.EndHorizontal();
                foreach (PartStatus status in masterStatus[currentVessl].allPartsStatus)
                {
                    // Display part data
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(String.Format("{0,50}", status.partName));
                    GUILayout.Label(String.Format("{0,7:F2}du", status.flightData));
                    GUILayout.Label(String.Format("{0,7:F2}%", status.reliability));
                    string goNoGo;
                    GUIStyle useStyle;
                    if (status.activeFailure != null)
                    {
                        if (status.activeFailure.GetFailureDetails().severity == "major")
                            useStyle = Styles.textStyleCritical;
                        else
                            useStyle = Styles.textStyleWarning;
                        goNoGo = String.Format("{0,-25}", status.activeFailure.GetFailureDetails().failureTitle);
                    }
                    else
                    {
                        useStyle = Styles.textStyleSafe;
                        goNoGo = String.Format("{0,-25}", "Status OK");
                    }
                    string tooltip = status.repairRequirements;
                    GUILayout.Label(new GUIContent(goNoGo, tooltip), useStyle);
                    if (status.activeFailure != null)
                    {
                        if (GUILayout.Button("R"))
                        {
                            // attempt repair
                            bool repairSuccess = status.flightCore.AttemptRepair();
                        }
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }

        void ViewSelection_OnSelectionChanged(MonoBehaviourWindowPlus.DropDownList sender, int oldIndex, int newIndex)
        {
        }
    }
}

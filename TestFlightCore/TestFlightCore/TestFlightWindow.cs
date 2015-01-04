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
            settings = tfScenario.settings;
            if (settings == null)
            {
                settings = new Settings("../settings.cfg");
                tfScenario.settings = settings;
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
                settings.displaySettingsWindow = true;
                settings.Save();
            }
            settings.Load();
            StartCoroutine("AddToToolbar");
            // Start up our UI Update worker
            StartRepeatingWorker(2);
            TestFlight.Resources.LoadTextures();
            base.Start();
        }

        internal override void Awake()
        {
            base.Awake();
        }

        internal override void OnGUIOnceOnly()
        {
            Styles.InitStyles();
            Styles.InitSkins();
            SkinsLibrary.SetCurrent("Unity");
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
            GUIContent settingsButton = new GUIContent(TestFlight.Resources.btnChevronDown, "Open Settings Panel");
            if (settings.displaySettingsWindow)
            {
                settingsButton.image = TestFlight.Resources.btnChevronUp;
                settingsButton.tooltip = "Close Settings Panel";
            }

            if (masterStatus == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("TestFlight is starting up...");
                if (GUILayout.Button(settingsButton, GUILayout.Width(38)))
                {
                    settings.displaySettingsWindow = !settings.displaySettingsWindow;
                    CalculateWindowBounds();
                    settings.Save();
                }
                GUILayout.EndHorizontal();
            }
            else if (masterStatus.Count() <= 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("TestFlight is not currently tracking any vessels");
                if (GUILayout.Button(settingsButton, GUILayout.Width(38)))
                {
                    settings.displaySettingsWindow = !settings.displaySettingsWindow;
                    CalculateWindowBounds();
                    settings.Save();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                // Display information on active vessel
                Guid currentVessl = FlightGlobals.ActiveVessel.id;
                GUILayout.BeginHorizontal();
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
                if (GUILayout.Button(settingsButton, GUILayout.Width(38)))
                {
                    settings.displaySettingsWindow = !settings.displaySettingsWindow;
                    CalculateWindowBounds();
                    settings.Save();
                }
            }

            // Draw settings pane if opened
            if (settings.displaySettingsWindow)
            {
                GUILayout.Space(5);
                GUILayout.Label("", Styles.styleSeparatorH, GUILayout.Width(WindowRect.width - 15), GUILayout.Height(2));

                GUILayout.Label("GUI Settings");
                GUILayout.BeginHorizontal();
                if (DrawToggle(ref settings.enableHUD, "Enable HUD in Flight Scene", Styles.styleToggle))
                {
                    settings.Save();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Master Status Update Frequency", "Sets how often the Master Status Display is updated.\nLower settings will make the MSD respond to vessel changes faster but at the possible cost of performance."));
                if (DrawHorizontalSlider(ref settings.masterStatusUpdateFrequency, 0, 30, GUILayout.Width(300)))
                {
                    settings.Save();
                }
                GUILayout.Label(String.Format("{0,5:f2}", settings.masterStatusUpdateFrequency));
                GUILayout.EndHorizontal();

                GUILayout.Label("Performance Settings");
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Minimum Update Rate", "Define the time in seconds between updates to all parts.\nSetting this lower will ensure you always have up to date data, but might be a performance issue on large craft.\nIncrease this if you find it affecting performance"));
                if (DrawHorizontalSlider(ref settings.minTimeBetweenDataPoll, 0, 10, GUILayout.Width(300)))
                {
                    settings.Save();
                }
                GUILayout.Label(String.Format("{0,5:f2}", settings.minTimeBetweenDataPoll));
                GUILayout.EndHorizontal();

                GUILayout.Label("Difficulty Settings");
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("MinimumTime Between Failure Checks", "Define the minimum time in seconds that the system will check all parts to see if any have failed.\nConsider this a difficulty slider of sorts, as the more often checks are done, the more often you can run into failures"));
                if (DrawHorizontalSlider(ref settings.minTimeBetweenFailurePoll, 15, 120, GUILayout.Width(300)))
                {
                    settings.Save();
                }
                GUILayout.Label(String.Format("{0,5:f2}", settings.minTimeBetweenFailurePoll));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Flight Data Multiplier", "Overall difficulty slider.\nIncrease to make all parts accumuate flight data faster.  Decrease to make them accumulate flight data slower.\nA setting of 1 is normal rate"));
                if (DrawHorizontalSlider(ref settings.flightDataMultiplier, 0.5, 2, GUILayout.Width(300)))
                {
                    settings.Save();
                }
                GUILayout.Label(String.Format("{0,5:f2}", settings.flightDataMultiplier));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Flight Data Engineer Multiplier", "Overall difficulty slider\nIncreases or decreases the bonus applied to the accumulation of flight data from having Engineers in your crew.\nA setting of 1 is normal difficulty."));
                if (DrawHorizontalSlider(ref settings.flightDataEngineerMultiplier, 0.5, 2, GUILayout.Width(300)))
                {
                    settings.Save();
                }
                GUILayout.Label(String.Format("{0,5:f2}", settings.flightDataEngineerMultiplier));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Global Reliability Modifier", "Overall difficulty slider\nStraight modifier added to the final reliability calculation for a part."));
                if (DrawHorizontalSlider(ref settings.globalReliabilityModifier, -25, 25, GUILayout.Width(300)))
                {
                    settings.Save();
                }
                GUILayout.Label(String.Format("{0,5:f2}", settings.globalReliabilityModifier));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        void ViewSelection_OnSelectionChanged(MonoBehaviourWindowPlus.DropDownList sender, int oldIndex, int newIndex)
        {
        }
    }
}

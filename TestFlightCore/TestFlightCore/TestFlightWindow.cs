using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using KSPPluginFramework;

namespace TestFlightCore
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TestFlightWindow : MonoBehaviourWindowPlus
    {
        internal TestFlightManagerScenario tfScenario;
        internal TestFlightManager tfManager;
        private bool isReady = false;
        private ApplicationLauncherButton appLauncherButton;
        private TestFlightHUD hud;
        private bool stickyWindow;
        private int lastPartCount = 0;
        private string[] guiSizes = { "Small", "Normal", "Large" };

        private DropDownList ddlSettingsPage = null;

        internal override void Start()
        {
            Visible = false;
            isReady = false;
            tfScenario = null;
            StartCoroutine("ConnectToScenario");
            base.Start();
        }

        IEnumerator ConnectToScenario()
        {
            while (TestFlightManagerScenario.Instance == null)
            {
                yield return null;
            }

            while (TestFlightManager.Instance == null)
            {
                yield return null;
            }
            tfScenario = TestFlightManagerScenario.Instance;
            while (!tfScenario.isReady)
            {
                yield return null;
            }
            Startup();
        }

        internal void Startup()
        {
            tfScenario = TestFlightManagerScenario.Instance;
            tfScenario.settings.Load();
            tfManager = TestFlightManager.Instance;
            LogFormatted_DebugOnly("Starting coroutine to add toolbar icon");
            StartCoroutine("AddToToolbar");
            TestFlight.Resources.LoadTextures();

            if (HighLogic.LoadedSceneIsFlight && tfScenario.settings.enableHUD && hud == null)
            {
                hud = gameObject.AddComponent(typeof(TestFlightHUD)) as TestFlightHUD;
                if (hud != null)
                {
                    LogFormatted_DebugOnly("Starting up TestFlightHUD");
                    hud.Startup(this);
                }
                GameEvents.onGameSceneLoadRequested.Add(Event_OnGameSceneLoadRequested);
            }
            // Default position and size -- will get proper bounds calculated when needed
            WindowRect = new Rect(0, 50, 500, 50);
            DragEnabled = !tfScenario.settings.mainWindowLocked;
            ClampToScreen = true;
            TooltipsEnabled = true;
            TooltipMouseOffset = new Vector2d(10, 10);
            TooltipStatic = true;
            WindowCaption = "";
            List<string> views = new List<string>()
            {
                "Visual Settings",
                "Difficulty/Performance Settings",
                "Miscellaneous"
            };
            ddlSettingsPage = new DropDownList(views, this);
            ddlManager.AddDDL(ddlSettingsPage);
            ddlSettingsPage.OnSelectionChanged += SettingsPage_OnSelectionChanged;
            WindowMoveEventsEnabled = true;
            onWindowMoveComplete += MainWindow_OnWindowMoveComplete;
            isReady = true;
        }

        public void Event_OnGameSceneLoadRequested(GameScenes scene)
        {
            LogFormatted_DebugOnly("Destroying Flight HUD");
            hud.Shutdown();
            Destroy(hud);
            hud = null;
            LogFormatted_DebugOnly("Unhooking event");
            GameEvents.onGameSceneLoadRequested.Remove(Event_OnGameSceneLoadRequested);
        }

        internal override void Awake()
        {
            base.Awake();
        }

        internal override void OnGUIOnceOnly()
        {
            Styles.InitStyles();
            Styles.InitSkins();
            SkinsLibrary.SetCurrent("SolarizedDark");
        }

        internal void CalculateWindowBounds()
        {
            LogFormatted_DebugOnly("Calculating Window Bounds");
            if (appLauncherButton == null)
                return;
            if (tfScenario == null)
                return;
            float windowWidth = 710f;
            if (tfScenario.settings.shortenPartNameInMSD)
                windowWidth -= 100f;
            if (!tfScenario.settings.showFlightDataInMSD)
                windowWidth -= 75f;
            if (!tfScenario.settings.showFailureRateInMSD)
                windowWidth -= 75f;
            if (!tfScenario.settings.showMTBFStringInMSD)
                windowWidth -= 150f;
            if (!tfScenario.settings.showStatusTextInMSD)
                windowWidth -= 100f;

            float left = Screen.width - windowWidth;
            float windowHeight = 10f;;
            float top = 40f;

            if (tfScenario.settings.currentMSDSize == 0)
                windowHeight += 100f;
            else if (tfScenario.settings.currentMSDSize == 1)
                windowHeight += 200f;
            else if (tfScenario.settings.currentMSDSize == 2)
                windowHeight += 300f;

            if (tfScenario.settings.displaySettingsWindow)
                windowHeight += 250f;
            if (!tfScenario.settings.mainWindowLocked)
            {
                left = tfScenario.settings.mainWindowPosition.xMin;
                top = tfScenario.settings.mainWindowPosition.yMin;
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
                    ApplicationLauncher.AppScenes.FLIGHT,
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
            Dictionary<Guid, MasterStatusItem> masterStatus = tfManager.GetMasterStatus();
            GUIContent settingsButton = new GUIContent(TestFlight.Resources.btnChevronDown, "Open Settings Panel");
            if (tfScenario.settings.displaySettingsWindow)
            {
                settingsButton.image = TestFlight.Resources.btnChevronUp;
                settingsButton.tooltip = "Close Settings Panel";
            }

            if (masterStatus == null)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("TestFlight is starting up...");
                if (GUILayout.Button(settingsButton, GUILayout.Width(38)))
                {
                    tfScenario.settings.displaySettingsWindow = !tfScenario.settings.displaySettingsWindow;
                    CalculateWindowBounds();
                    tfScenario.settings.Save();
                }
                GUILayout.EndHorizontal();
            }
            else if (masterStatus.Count() <= 0)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("TestFlight is not currently tracking any vessels");
                if (GUILayout.Button(settingsButton, GUILayout.Width(38)))
                {
                    tfScenario.settings.displaySettingsWindow = !tfScenario.settings.displaySettingsWindow;
                    CalculateWindowBounds();
                    tfScenario.settings.Save();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                // Display information on active vessel
                Guid currentVessel = FlightGlobals.ActiveVessel.id;

                if (tfScenario.settings.showFailedPartsOnlyInMSD)
                {
                    if (masterStatus[currentVessel].allPartsStatus.Count(ps => ps.activeFailure != null) < lastPartCount)
                    {
                        lastPartCount = masterStatus[currentVessel].allPartsStatus.Count(ps => ps.activeFailure != null);
                        CalculateWindowBounds();
                    }
                }
                else
                {
                    if (masterStatus[currentVessel].allPartsStatus.Count < lastPartCount)
                    {
                        lastPartCount = masterStatus[currentVessel].allPartsStatus.Count;
                        CalculateWindowBounds();
                    }
                }
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("MSD for " + masterStatus[currentVessel].vesselName);
                GUILayout.EndHorizontal();
                tfScenario.settings.currentMSDScrollPosition = GUILayout.BeginScrollView(tfScenario.settings.currentMSDScrollPosition);
                foreach (PartStatus status in masterStatus[currentVessel].allPartsStatus)
                {
                    // Display part data
//                    GUILayout.Label(String.Format("{0,50}", status.partName));
//                    GUILayout.Label(String.Format("{0,7:F2}du", status.flightData));
//                    GUILayout.Label(String.Format("{0,7:F2}%", status.reliability));

                    if (tfScenario.settings.showFailedPartsOnlyInMSD && status.activeFailure == null)
                        continue;
                    if (tfScenario.settings.showFailedPartsOnlyInMSD && status.acknowledged)
                        continue;

                    GUILayout.BeginHorizontal();
                    string partDisplay;
                    // Part Name
                    string tooltip = status.repairRequirements;
                    if (tfScenario.settings.shortenPartNameInMSD)
                        GUILayout.Label(new GUIContent(status.partName, tooltip), GUILayout.Width(100));
                    else
                        GUILayout.Label(new GUIContent(status.partName, tooltip), GUILayout.Width(200));
                    GUILayout.Space(10);
                    // Flight Data
                    if (tfScenario.settings.showFlightDataInMSD)
                    {
                        GUILayout.Label(String.Format("{0,-7:F2}<b>du</b>", status.flightData), GUILayout.Width(75));
                        GUILayout.Space(10);
                    }
                    // Resting Reliability
                    if (tfScenario.settings.showMTBFStringInMSD)
                    {
                        GUILayout.Label(String.Format("{0} <b>MTBF</b>", status.mtbfString), GUILayout.Width(130));
                        GUILayout.Space(10);
                    }
                    // Momentary Reliability
                    if (tfScenario.settings.showFailureRateInMSD)
                    {
                        GUILayout.Label(String.Format("{0:F6}", status.momentaryFailureRate), GUILayout.Width(60));
                        GUILayout.Space(10);
                    }
                    // Part Status Text
                    if (tfScenario.settings.showStatusTextInMSD)
                    {
                        if (status.activeFailure == null)
                            partDisplay = String.Format("<color=#859900ff>{0,-30}</color>", "OK");
                        else
                        {
                            if (status.activeFailure.GetFailureDetails().severity == "major")
                                partDisplay = String.Format("<color=#dc322fff>{0,-30}</color>", status.activeFailure.GetFailureDetails().failureTitle);
                            else
                                partDisplay = String.Format("<color=#b58900ff>{0,-30}</color>", status.activeFailure.GetFailureDetails().failureTitle);
                        }
                        GUILayout.Label(partDisplay, GUILayout.Width(100));
                    }
                    if (status.activeFailure != null)
                    {
                        if (status.activeFailure.CanAttemptRepair())
                        {
                            if (GUILayout.Button("R", GUILayout.Width(38)))
                            {
                                // attempt repair
                                bool repairSuccess = status.flightCore.AttemptRepair();
                            }
                        }
                        if (GUILayout.Button("A", GUILayout.Width(38)))
                        {
                            // attempt repair
                            status.flightCore.AcknowledgeFailure();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();

                if (GUILayout.Button(settingsButton, GUILayout.Width(38)))
                {
                    tfScenario.settings.displaySettingsWindow = !tfScenario.settings.displaySettingsWindow;
                    CalculateWindowBounds();
                    tfScenario.settings.Save();
                }
            }

            // Draw settings pane if opened
            if (tfScenario.settings.displaySettingsWindow)
            {
                GUILayout.Space(15);
                if (ddlSettingsPage == null)
                {
                    GUILayout.Space(10);
                    GUILayout.EndVertical();
                    return;
                }
                ddlSettingsPage.DrawButton();

                switch (tfScenario.settings.settingsPage)
                {
                    case 0:
                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.settings.showFailedPartsOnlyInMSD, "Show Failed Parts Only", Styles.styleToggle))
                        {
                            tfScenario.settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.settings.shortenPartNameInMSD, "Short Part Names", Styles.styleToggle))
                        {
                            tfScenario.settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.settings.showFlightDataInMSD, "Show Flight Data", Styles.styleToggle))
                        {
                            tfScenario.settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.settings.showMTBFStringInMSD, "Show MTBF", Styles.styleToggle))
                        {
                            tfScenario.settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.settings.showFailureRateInMSD, "Show Failure Rate", Styles.styleToggle))
                        {
                            tfScenario.settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.settings.showStatusTextInMSD, "Show Part Status Text", Styles.styleToggle))
                        {
                            tfScenario.settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.settings.mainWindowLocked, "Lock MSD Position", Styles.styleToggle))
                        {
                            if (tfScenario.settings.mainWindowLocked)
                            {
                                tfScenario.settings.mainWindowLocked = true;
                                CalculateWindowBounds();
                                tfScenario.settings.mainWindowPosition = WindowRect;
                                DragEnabled = false;
                            }
                            else
                            {
                                DragEnabled = true;
                            }
                            tfScenario.settings.Save();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("MSD Size", GUILayout.Width(200));
                        tfScenario.settings.currentMSDSize = GUILayout.Toolbar(tfScenario.settings.currentMSDSize,guiSizes);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.settings.enableHUD, "Enable Flight HUD", Styles.styleToggle))
                        {
                            tfScenario.settings.Save();
                            if (tfScenario.settings.enableHUD)
                            {
                                hud = gameObject.AddComponent(typeof(TestFlightHUD)) as TestFlightHUD;
                                if (hud != null)
                                {
                                    LogFormatted_DebugOnly("Starting up Flight HUD");
                                    hud.Startup(this);
                                }
                                GameEvents.onGameSceneLoadRequested.Add(Event_OnGameSceneLoadRequested);
                            }
                            else
                            {
                                LogFormatted_DebugOnly("Destroying Flight HUD");
                                hud.Shutdown();
                                Destroy(hud);
                                hud = null;
                                GameEvents.onGameSceneLoadRequested.Remove(Event_OnGameSceneLoadRequested);
                            }
                        }
                        GUILayout.EndHorizontal();
                        break;
                    case 1:
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Minimum Update Rate", 
                            "Define the time in seconds between updates to all parts.\n" +
                            "Setting this lower will ensure you always have up to date data, but might be a performance issue on large craft.\n" +
                            "Increase this if you find it affecting performance"),
                            GUILayout.Width(200)
                        );
                        if (DrawHorizontalSlider(ref tfScenario.settings.minTimeBetweenDataPoll, 0, 1, GUILayout.Width(150)))
                        {
                            tfScenario.settings.Save();
                        }
                        GUILayout.Label(String.Format("{0,5:f2}", tfScenario.settings.minTimeBetweenDataPoll), GUILayout.Width(75));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Flight Data Multiplier", "Overall difficulty slider.\n" +
                            "Increase to make all parts accumuate flight data faster.  Decrease to make them accumulate flight data slower.\n" + 
                            "A setting of 1 is normal rate"),
                            GUILayout.Width(200)
                        );
                        if (DrawHorizontalSlider(ref tfScenario.settings.flightDataMultiplier, 0.5, 2, GUILayout.Width(150)))
                        {
                            tfScenario.settings.Save();
                        }
                        GUILayout.Label(String.Format("{0,5:f2}", tfScenario.settings.flightDataMultiplier), GUILayout.Width(75));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Flight Data Engineer Multiplier", "Overall difficulty slider\n" + 
                            "Increases or decreases the bonus applied to the accumulation of flight data from having Engineers in your crew.\n" + 
                            "A setting of 1 is normal difficulty."),
                            GUILayout.Width(200)
                        );
                        if (DrawHorizontalSlider(ref tfScenario.settings.flightDataEngineerMultiplier, 0.5, 2, GUILayout.Width(150)))
                        {
                            tfScenario.settings.Save();
                        }
                        GUILayout.Label(String.Format("{0,5:f2}", tfScenario.settings.flightDataEngineerMultiplier), GUILayout.Width(75));
                        GUILayout.EndHorizontal();

                        break;
                    case 2:
                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.settings.debugLog, "Eable Debugging", Styles.styleToggle))
                        {
                            tfScenario.settings.Save();
                        }
                        GUILayout.EndHorizontal();
                        break;
                }

            }
            GUILayout.Space(10);
            GUILayout.EndVertical();
            if (GUI.changed)
            {
                CalculateWindowBounds();
                tfScenario.settings.Save();
            }
        }

        // GUI EVent Handlers
        void SettingsPage_OnSelectionChanged(MonoBehaviourWindowPlus.DropDownList sender, int oldIndex, int newIndex)
        {
            tfScenario.settings.settingsPage = newIndex;
            tfScenario.settings.Save();
        }
        void MainWindow_OnWindowMoveComplete(MonoBehaviourWindow sender)
        {
            tfScenario.settings.mainWindowPosition = WindowRect;
            tfScenario.settings.Save();
        }
    }
}

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
        private bool isReady = false;
        private ApplicationLauncherButton appLauncherButton;
        private TestFlightHUD hud;
        private bool stickyWindow;
        internal Settings settings = null;
        private int lastPartCount = 0;
        private string[] guiSizes = { "Small", "Normal", "Large" };

        private DropDownList ddlSettingsPage = null;

        internal override void Start()
        {
            LogFormatted_DebugOnly("TRACE MSD Start");
            Visible = false;
            isReady = false;
            tfScenario = null;
            settings = null;
            StartCoroutine("ConnectToScenario");
            base.Start();
        }

        IEnumerator ConnectToScenario()
        {
            while (TestFlightManagerScenario.Instance == null)
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
            LogFormatted_DebugOnly("TRACE TestFlightManagerScenario = " + tfScenario);
            settings = tfScenario.settings;
            if (settings == null)
            {
                settings = new Settings("../settings.cfg");
                tfScenario.settings = settings;
            }
            settings.Load();
            LogFormatted_DebugOnly("Starting coroutine to add toolbar icon");
            StartCoroutine("AddToToolbar");
            TestFlight.Resources.LoadTextures();

            if (HighLogic.LoadedSceneIsFlight && settings.enableHUD && hud == null)
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
            DragEnabled = !settings.mainWindowLocked;
            ClampToScreen = true;
            TooltipsEnabled = true;
            TooltipMouseOffset = new Vector2d(10, 10);
            TooltipStatic = true;
            WindowCaption = "";
            List<string> views = new List<string>()
            {
                "Visual Settings",
                "Difficulty/Performance Settings"
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
            LogFormatted_DebugOnly("TRACE MSD OnGUIOnceOnly()");
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
            float windowWidth = 670f;
            if (settings.shortenPartNameInMSD)
                windowWidth -= 100f;
            if (!settings.showFlightDataInMSD)
                windowWidth -= 75f;
            if (!settings.showMomentaryReliabilityInMSD)
                windowWidth -= 75f;
            if (!settings.showRestingReliabilityInMSD)
                windowWidth -= 75f;
            if (!settings.showStatusTextInMSD)
                windowWidth -= 100f;

            float left = Screen.width - windowWidth;
            float windowHeight = 50f;;
            float top = 40f;

            if (settings.currentMSDSize == 0)
                windowHeight += 100f;
            else if (settings.currentMSDSize == 1)
                windowHeight += 200f;
            else if (settings.currentMSDSize == 2)
                windowHeight += 300f;

            if (settings.displaySettingsWindow)
                windowHeight += 250f;
            if (!settings.mainWindowLocked)
            {
                left = settings.mainWindowPosition.xMin;
                top = settings.mainWindowPosition.yMin;
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
            Dictionary<Guid, MasterStatusItem> masterStatus = tfScenario.GetMasterStatus();
            GUIContent settingsButton = new GUIContent(TestFlight.Resources.btnChevronDown, "Open Settings Panel");
            if (settings.displaySettingsWindow)
            {
                settingsButton.image = TestFlight.Resources.btnChevronUp;
                settingsButton.tooltip = "Close Settings Panel";
            }

            if (masterStatus == null)
            {
                LogFormatted_DebugOnly("TRACE masterStatus = null");
                GUILayout.Space(10);
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
                LogFormatted_DebugOnly("TRACE masterStatus.Count <= 0");
                GUILayout.Space(10);
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
                LogFormatted_DebugOnly("TRACE masterStatus is valid");
                // Display information on active vessel
                Guid currentVessel = FlightGlobals.ActiveVessel.id;
                LogFormatted_DebugOnly("TRACE grabbed currentVessel");

                if (settings.showFailedPartsOnlyInMSD)
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
                LogFormatted_DebugOnly("TRACE starting layout");
                GUILayout.Label("MSD for " + masterStatus[currentVessel].vesselName);
                GUILayout.EndHorizontal();
                settings.currentMSDScrollPosition = GUILayout.BeginScrollView(settings.currentMSDScrollPosition);
                LogFormatted_DebugOnly("TRACE setup scrollview");
                LogFormatted_DebugOnly("TRACE checking for valid currentVessel");
                if (masterStatus.ContainsKey(currentVessel))
                    LogFormatted_DebugOnly("TRACE found currentVessel in masterStatus");
                else
                    LogFormatted_DebugOnly("TRACE Could not find currentVessel in masterStatus");
                    
                foreach (PartStatus status in masterStatus[currentVessel].allPartsStatus)
                {
                    LogFormatted_DebugOnly("TRACE Displaying data for part " + status.partName);
                    // Display part data
//                    GUILayout.Label(String.Format("{0,50}", status.partName));
//                    GUILayout.Label(String.Format("{0,7:F2}du", status.flightData));
//                    GUILayout.Label(String.Format("{0,7:F2}%", status.reliability));
                    if (settings.showFailedPartsOnlyInMSD && status.activeFailure == null)
                        continue;
                    GUILayout.BeginHorizontal();
                    string partDisplay;
                    // Part Name
                    string tooltip = status.repairRequirements;
                    if (settings.shortenPartNameInMSD)
                        GUILayout.Label(new GUIContent(status.partName, tooltip), GUILayout.Width(100));
                    else
                        GUILayout.Label(new GUIContent(status.partName, tooltip), GUILayout.Width(200));
                    GUILayout.Space(10);
                    // Flight Data
                    if (settings.showFlightDataInMSD)
                    {
                        GUILayout.Label(String.Format("{0,-7:F2}<b>du</b>", status.flightData), GUILayout.Width(75));
                        GUILayout.Space(10);
                    }
                    // Resting Reliability
                    if (settings.showRestingReliabilityInMSD)
                    {
                        GUILayout.Label(String.Format("{0,-5:F2}<b>%R</b>", status.reliability), GUILayout.Width(75));
                        GUILayout.Space(10);
                    }
                    // Momentary Reliability
                    if (settings.showMomentaryReliabilityInMSD)
                    {
                        GUILayout.Label(String.Format("{0,-5:F2}<b>%M</b>", status.momentaryReliability), GUILayout.Width(75));
                        GUILayout.Space(10);
                    }
                    // Part Status Text
                    if (settings.showStatusTextInMSD)
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
                        if (GUILayout.Button("R", GUILayout.Width(38)))
                        {
                            // attempt repair
                            bool repairSuccess = status.flightCore.AttemptRepair();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                LogFormatted_DebugOnly("TRACE Done displaying part data");

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
                LogFormatted_DebugOnly("TRACE Drawing settings selection");
                GUILayout.Space(15);
                if (ddlSettingsPage == null)
                {
                    LogFormatted_DebugOnly("TRACE ddlSettings is invalid!");
                    GUILayout.Space(10);
                    GUILayout.EndVertical();
                    return;
                }
                ddlSettingsPage.DrawButton();
                LogFormatted_DebugOnly("TRACE Drawing selected settings pane");

                switch (settings.settingsPage)
                {
                    case 0:
                        LogFormatted_DebugOnly("TRACE Drawing visual settings");
                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref settings.showFailedPartsOnlyInMSD, "Short Failed Parts Only", Styles.styleToggle))
                        {
                            settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref settings.shortenPartNameInMSD, "Short Part Names", Styles.styleToggle))
                        {
                            settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref settings.showFlightDataInMSD, "Flight Data", Styles.styleToggle))
                        {
                            settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref settings.showRestingReliabilityInMSD, "Resting Reliability", Styles.styleToggle))
                        {
                            settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref settings.showMomentaryReliabilityInMSD, "Momentary Reliability", Styles.styleToggle))
                        {
                            settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref settings.showStatusTextInMSD, "Part Status Text", Styles.styleToggle))
                        {
                            settings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref settings.mainWindowLocked, "Lock MSD Position", Styles.styleToggle))
                        {
                            if (settings.mainWindowLocked)
                            {
                                settings.mainWindowLocked = true;
                                CalculateWindowBounds();
                                settings.mainWindowPosition = WindowRect;
                                DragEnabled = false;
                            }
                            else
                            {
                                DragEnabled = true;
                            }
                            settings.Save();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("MSD Size", GUILayout.Width(200));
                        settings.currentMSDSize = GUILayout.Toolbar(settings.currentMSDSize,guiSizes);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref settings.enableHUD, "Enable Flight HUD", Styles.styleToggle))
                        {
                            settings.Save();
                            if (settings.enableHUD)
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
                        if (DrawHorizontalSlider(ref settings.minTimeBetweenDataPoll, 0, 10, GUILayout.Width(150)))
                        {
                            settings.Save();
                        }
                        GUILayout.Label(String.Format("{0,5:f2}", settings.minTimeBetweenDataPoll), GUILayout.Width(75));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Minimum Time Between Failure Checks", 
                            "Define the minimum time in seconds that the system will check all parts to see if any have failed.\n" +
                            "Consider this a difficulty slider of sorts, as the more often checks are done, the more often you can run into failures"),
                            GUILayout.Width(200)
                        );
                        if (DrawHorizontalSlider(ref settings.minTimeBetweenFailurePoll, 15, 120, GUILayout.Width(150)))
                        {
                            settings.Save();
                        }
                        GUILayout.Label(String.Format("{0,5:f2}", settings.minTimeBetweenFailurePoll), GUILayout.Width(75));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Flight Data Multiplier", "Overall difficulty slider.\n" +
                            "Increase to make all parts accumuate flight data faster.  Decrease to make them accumulate flight data slower.\n" + 
                            "A setting of 1 is normal rate"),
                            GUILayout.Width(200)
                        );
                        if (DrawHorizontalSlider(ref settings.flightDataMultiplier, 0.5, 2, GUILayout.Width(150)))
                        {
                            settings.Save();
                        }
                        GUILayout.Label(String.Format("{0,5:f2}", settings.flightDataMultiplier), GUILayout.Width(75));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Flight Data Engineer Multiplier", "Overall difficulty slider\n" + 
                            "Increases or decreases the bonus applied to the accumulation of flight data from having Engineers in your crew.\n" + 
                            "A setting of 1 is normal difficulty."),
                            GUILayout.Width(200)
                        );
                        if (DrawHorizontalSlider(ref settings.flightDataEngineerMultiplier, 0.5, 2, GUILayout.Width(150)))
                        {
                            settings.Save();
                        }
                        GUILayout.Label(String.Format("{0,5:f2}", settings.flightDataEngineerMultiplier), GUILayout.Width(75));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Global Reliability Modifier", "Overall difficulty slider\n"+ 
                            "Straight modifier added to the final reliability calculation for a part."),
                            GUILayout.Width(200)
                        );
                        if (DrawHorizontalSlider(ref settings.globalReliabilityModifier, -25, 25, GUILayout.Width(150)))
                        {
                            settings.Save();
                        }
                        GUILayout.Label(String.Format("{0,5:f2}", settings.globalReliabilityModifier), GUILayout.Width(75));
                        GUILayout.EndHorizontal();
                        break;
                }

            }
            GUILayout.Space(10);
            GUILayout.EndVertical();
            if (GUI.changed)
                CalculateWindowBounds();
        }

        // GUI EVent Handlers
        void SettingsPage_OnSelectionChanged(MonoBehaviourWindowPlus.DropDownList sender, int oldIndex, int newIndex)
        {
            settings.settingsPage = newIndex;
            settings.Save();
        }
        void MainWindow_OnWindowMoveComplete(MonoBehaviourWindow sender)
        {
            settings.mainWindowPosition = WindowRect;
            settings.Save();
        }
    }
}

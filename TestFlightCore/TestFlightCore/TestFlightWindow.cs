using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using KSP.UI.Screens;

using KSPPluginFramework;

using TestFlightAPI;

namespace TestFlightCore
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TestFlightWindow : MonoBehaviourWindowPlus
    {
        internal TestFlightManagerScenario tfScenario;
        internal TestFlightManager tfManager;
        private ApplicationLauncherButton appLauncherButton;
        private TestFlightHUD hud;
        private bool stickyWindow;
        private string[] guiSizes = { "Small", "Normal", "Large" };

        private DropDownList ddlSettingsPage = null;

        internal void Log(string message)
        {
            if (TestFlightManagerScenario.Instance == null || TestFlightManagerScenario.Instance.userSettings == null)
                return;

            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = "TestFlightWindow: " + message;
            TestFlightUtil.Log(message, debug);
        }

        internal override void Start()
        {
            Visible = false;
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
            tfScenario.userSettings.Load();
            tfManager = TestFlightManager.Instance;
            Log("Starting coroutine to add toolbar icon");
            StartCoroutine("AddToToolbar");
            TestFlight.Resources.LoadTextures();

            if (HighLogic.LoadedSceneIsFlight && tfScenario.userSettings.enableHUD && hud == null)
            {
                hud = gameObject.AddComponent(typeof(TestFlightHUD)) as TestFlightHUD;
                if (hud != null)
                {
                    Log("Starting up TestFlightHUD");
                    hud.Startup(this);
                }
                GameEvents.onGameSceneLoadRequested.Add(Event_OnGameSceneLoadRequested);
            }
            // Default position and size -- will get proper bounds calculated when needed
            WindowRect = new Rect(0, 50, 500, 50);
            DragEnabled = !tfScenario.userSettings.mainWindowLocked;
            ClampToScreen = true;
            TooltipsEnabled = true;
            TooltipMouseOffset = new Vector2d(10, 10);
            TooltipStatic = true;
            WindowCaption = "";
            List<string> views = new List<string>()
            {
                "Visual Settings",
                "Difficulty/Performance Settings",
                "Miscellaneous",
                "SaveGame Settings"
            };
            ddlSettingsPage = new DropDownList(views, this);
            ddlManager.AddDDL(ddlSettingsPage);
            ddlSettingsPage.OnSelectionChanged += SettingsPage_OnSelectionChanged;
            WindowMoveEventsEnabled = true;
            onWindowMoveComplete += MainWindow_OnWindowMoveComplete;
            CalculateWindowBounds();
            Visible = tfScenario.userSettings.showMSD;
        }

        public void Event_OnGameSceneLoadRequested(GameScenes scene)
        {
            Log("Destroying Flight HUD");
            hud.Shutdown();
            Destroy(hud);
            hud = null;
            Log("Unhooking event");
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
            SkinsLibrary.CurrentSkin.label.wordWrap = false;
        }

        internal void CalculateWindowBounds()
        {
            Log("Calculating Window Bounds");
            if (appLauncherButton == null)
                return;
            if (tfScenario == null)
                return;
            float windowWidth = 740f;
            if (tfScenario.userSettings.shortenPartNameInMSD)
                windowWidth -= 100f;
            if (!tfScenario.userSettings.showFlightDataInMSD)
                windowWidth -= 75f;
            if (!tfScenario.userSettings.showFailureRateInMSD)
                windowWidth -= 60f;
            if (!tfScenario.userSettings.showMTBFStringInMSD)
                windowWidth -= 130f;
            if (!tfScenario.userSettings.showStatusTextInMSD)
                windowWidth -= 100f;

            float left = Screen.width - windowWidth;
            float windowHeight = 10f;
            float top = 40f;

            if (tfScenario.userSettings.currentMSDSize == 0)
                windowHeight += 100f;
            else if (tfScenario.userSettings.currentMSDSize == 1)
                windowHeight += 200f;
            else if (tfScenario.userSettings.currentMSDSize == 2)
                windowHeight += 300f;

            if (tfScenario.userSettings.displaySettingsWindow)
                windowHeight += 250f;
            if (!tfScenario.userSettings.mainWindowLocked)
            {
                left = tfScenario.userSettings.mainWindowPosition.xMin;
                top = tfScenario.userSettings.mainWindowPosition.yMin;
            }
            WindowRect = new Rect(left, top, windowWidth, windowHeight);
        }


        IEnumerator AddToToolbar()
        {
            while (!ApplicationLauncher.Ready)
            {
                Log("Application launcher not ready..waiting");
                yield return null;
            }
            try
            {
                // Load the icon for the button
                Texture iconTexture = GameDatabase.Instance.GetTexture("TestFlight/Resources/AppLauncherIcon", false);
                if (iconTexture == null)
                {
                    throw new Exception("TestFlight MasterStatusDisplay: Failed to load icon texture");
                }
                Log("TestFlight MasterStatusDisplay: Creating icon on toolbar");
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
                    OpenWindow,
                    CloseWindow,
                    HoverInButton,
                    HoverOutButton,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                    iconTexture);
                ApplicationLauncher.Instance.AddOnHideCallback(HideButton);
                ApplicationLauncher.Instance.AddOnRepositionCallback(RepostionWindow);
                CalculateWindowBounds();
            }
            catch (Exception e)
            {
                Log("TestFlight MasterStatusDisplay: Unable to add button to application launcher: " + e.Message);
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
            tfScenario.userSettings.showMSD = true;
            tfScenario.userSettings.Save();
        }
        void CloseWindow()
        {
            Visible = false;
            stickyWindow = false;
            tfScenario.userSettings.showMSD = false;
            tfScenario.userSettings.Save();
        }
        void HideButton()
        {
            ApplicationLauncher.Instance.RemoveOnHideCallback(HideButton);
            ApplicationLauncher.Instance.RemoveOnRepositionCallback(RepostionWindow);
            ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
            ApplicationLauncher.Instance.AddOnShowCallback(ShowButton);
        }
        void ShowButton()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                // Load the icon for the button
                Texture iconTexture = GameDatabase.Instance.GetTexture("TestFlight/Resources/AppLauncherIcon", false);
                if (iconTexture == null)
                {
                    throw new Exception("TestFlight MasterStatusDisplay: Failed to load icon texture");
                }
                Log("TestFlight MasterStatusDisplay: Creating icon on toolbar");
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
                    OpenWindow,
                    CloseWindow,
                    HoverInButton,
                    HoverOutButton,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                    iconTexture);
                ApplicationLauncher.Instance.AddOnHideCallback(HideButton);
                ApplicationLauncher.Instance.AddOnRepositionCallback(RepostionWindow);
                CalculateWindowBounds();
            }
            else
            {
                ApplicationLauncher.Instance.RemoveOnShowCallback(ShowButton);
            }
        }
        void RepostionWindow()
        {
            CalculateWindowBounds();
            Log("TestFlight MasterStatusDisplay: RepositionWindow");
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
            Log("Recalculating Window Bounds");
            CalculateWindowBounds();

            base.RepeatingWorker();
        }
        internal override void DrawWindow(Int32 id)
        {
            if (tfManager == null)
                return;
            
            Dictionary<Guid, MasterStatusItem> masterStatus = tfManager.GetMasterStatus();
            GUIContent settingsButton = new GUIContent(TestFlight.Resources.btnChevronDown, "Open Settings Panel");
            if (tfScenario.userSettings.displaySettingsWindow)
            {
                settingsButton.image = TestFlight.Resources.btnChevronUp;
                settingsButton.tooltip = "Close Settings Panel";
            }

            if (masterStatus == null)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("TestFlight is starting up...");
                if (GUILayout.Button(settingsButton, GUILayout.Width(38)))
                {
                    tfScenario.userSettings.displaySettingsWindow = !tfScenario.userSettings.displaySettingsWindow;
                    CalculateWindowBounds();
                    tfScenario.userSettings.Save();
                }
                GUILayout.EndHorizontal();
            }
            else if (masterStatus.Count() <= 0)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("TestFlight is not currently tracking any vessels");
                if (GUILayout.Button(settingsButton, GUILayout.Width(38)))
                {
                    tfScenario.userSettings.displaySettingsWindow = !tfScenario.userSettings.displaySettingsWindow;
                    CalculateWindowBounds();
                    tfScenario.userSettings.Save();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginVertical();
                GUILayout.Space(10);
                // Display information on active vessel
                Guid currentVessel = FlightGlobals.ActiveVessel.id;

                if (masterStatus.ContainsKey(currentVessel) && masterStatus[currentVessel].allPartsStatus.Count > 0)
                {
                    tfScenario.userSettings.currentMSDScrollPosition = GUILayout.BeginScrollView(tfScenario.userSettings.currentMSDScrollPosition);
                    foreach (PartStatus status in masterStatus[currentVessel].allPartsStatus)
                    {
                        // Display part data
                        //                    GUILayout.Label(String.Format("{0,50}", status.partName));
                        //                    GUILayout.Label(String.Format("{0,7:F2}du", status.flightData));
                        //                    GUILayout.Label(String.Format("{0,7:F2}%", status.reliability));

                        if (tfScenario.userSettings.showFailedPartsOnlyInMSD && status.failures == null)
                            continue;
                        if (tfScenario.userSettings.showFailedPartsOnlyInMSD && status.failures.Count <= 0)
                            continue;

                        GUILayout.BeginHorizontal();
                        string partDisplay;
                        // Part Name
                        string tooltip = "";
                        if (status.failures == null || status.failures.Count <= 0)
                            tooltip = "Status OK";
                        else
                        {
                            for (int i = 0; i < status.failures.Count; i++)
                            {
                                tooltip += string.Format("<color=#{0}>{1}</color>\n", status.failures[i].GetFailureDetails().severity.ToLowerInvariant() == "major" ? "dc322fff" : "b58900ff", status.failures[i].GetFailureDetails().failureTitle);
                            }
                        }
                        if (tfScenario.userSettings.shortenPartNameInMSD)
                            GUILayout.Label(new GUIContent(status.partName, tooltip), GUILayout.Width(100));
                        else
                            GUILayout.Label(new GUIContent(status.partName, tooltip), GUILayout.Width(200));
                        GUILayout.Space(10);
                        // Flight Data
                        if (tfScenario.userSettings.showFlightDataInMSD)
                        {
                            GUILayout.Label(String.Format("{0,-7:F2}<b>du</b>", status.flightData), GUILayout.Width(75));
                            GUILayout.Space(10);
                        }
                        // Resting Reliability
                        if (tfScenario.userSettings.showMTBFStringInMSD)
                        {
                            GUILayout.Label(String.Format("{0} <b>MTBF</b>", status.mtbfString), GUILayout.Width(130));
                            GUILayout.Space(10);
                        }
                        // Momentary Reliability
                        if (tfScenario.userSettings.showFailureRateInMSD)
                        {
                            GUILayout.Label(String.Format("{0:F6}", status.momentaryFailureRate), GUILayout.Width(60));
                            GUILayout.Space(10);
                        }
                        // Part Status Text
                        if (tfScenario.userSettings.showStatusTextInMSD)
                        {
                            if (status.failures == null || status.failures.Count <= 0)
                                partDisplay = String.Format("<color=#859900ff>{0,-30}</color>", "OK");
                            else
                            {
                                ITestFlightFailure latestFailure = status.failures.Last();
                                if (latestFailure.GetFailureDetails().severity.ToLowerInvariant() == "major")
                                {
                                    partDisplay = String.Format("<color=#dc322fff>{0,-30}</color>", latestFailure.GetFailureDetails().failureTitle);
                                }
                                else
                                {
                                    partDisplay = String.Format("<color=#b58900ff>{0,-30}</color>", latestFailure.GetFailureDetails().failureTitle);
                                }
                            }
                            GUILayout.Label(partDisplay, GUILayout.Width(100));
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();

                    if (GUILayout.Button(settingsButton, GUILayout.Width(38)))
                    {
                        tfScenario.userSettings.displaySettingsWindow = !tfScenario.userSettings.displaySettingsWindow;
                        CalculateWindowBounds();
                        tfScenario.userSettings.Save();
                    }
                }
            }
              
            // Draw settings pane if opened
            if (tfScenario.userSettings.displaySettingsWindow)
            {
                GUILayout.Space(15);
                if (ddlSettingsPage == null)
                {
                    GUILayout.Space(10);
                    GUILayout.EndVertical();
                    return;
                }
                ddlSettingsPage.styleListBox = Styles.styleDropDownListBoxUnity;
                ddlSettingsPage.styleListBlocker = Styles.styleDropDownListBoxUnity;
                ddlSettingsPage.SelectedIndex = tfScenario.userSettings.settingsPage;
                ddlSettingsPage.DrawButton();

                switch (tfScenario.userSettings.settingsPage)
                {
                    case 0:
                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.userSettings.showFailedPartsOnlyInMSD, "Show Failed Parts Only", Styles.styleToggle))
                        {
                            tfScenario.userSettings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.userSettings.shortenPartNameInMSD, "Short Part Names", Styles.styleToggle))
                        {
                            tfScenario.userSettings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.userSettings.showFlightDataInMSD, "Show Flight Data", Styles.styleToggle))
                        {
                            tfScenario.userSettings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.userSettings.showMTBFStringInMSD, "Show MTBF", Styles.styleToggle))
                        {
                            tfScenario.userSettings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.userSettings.showFailureRateInMSD, "Show Failure Rate", Styles.styleToggle))
                        {
                            tfScenario.userSettings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.userSettings.showStatusTextInMSD, "Show Part Status Text", Styles.styleToggle))
                        {
                            tfScenario.userSettings.Save();
                            CalculateWindowBounds();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.userSettings.mainWindowLocked, "Lock MSD Position", Styles.styleToggle))
                        {
                            if (tfScenario.userSettings.mainWindowLocked)
                            {
                                tfScenario.userSettings.mainWindowLocked = true;
                                CalculateWindowBounds();
                                tfScenario.userSettings.mainWindowPosition = WindowRect;
                                DragEnabled = false;
                                tfScenario.userSettings.showMSD = false;
                            }
                            else
                            {
                                tfScenario.userSettings.showMSD = Visible;
                                DragEnabled = true;
                            }
                            tfScenario.userSettings.Save();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("MSD Size", GUILayout.Width(200));
                        tfScenario.userSettings.currentMSDSize = GUILayout.Toolbar(tfScenario.userSettings.currentMSDSize,guiSizes);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.userSettings.enableHUD, "Enable Flight HUD", Styles.styleToggle))
                        {
                            tfScenario.userSettings.Save();
                            if (tfScenario.userSettings.enableHUD)
                            {
                                hud = gameObject.AddComponent(typeof(TestFlightHUD)) as TestFlightHUD;
                                if (hud != null)
                                {
                                    Log("Starting up Flight HUD");
                                    hud.Startup(this);
                                }
                                GameEvents.onGameSceneLoadRequested.Add(Event_OnGameSceneLoadRequested);
                            }
                            else
                            {
                                Log("Destroying Flight HUD");
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
                        GUILayout.Label(new GUIContent("Flight Data Multiplier", "Overall difficulty slider.\n" +
                        "Increase to make all parts accumuate flight data faster.  Decrease to make them accumulate flight data slower.\n" +
                        "A setting of 1 is normal rate"),
                            GUILayout.Width(200)
                        );
                        if (DrawHorizontalSlider(ref tfScenario.userSettings.flightDataMultiplier, 0.5f, 2f, GUILayout.Width(150)))
                        {
                            tfScenario.userSettings.Save();
                        }
                        GUILayout.Label(String.Format("{0,5:f2}", tfScenario.userSettings.flightDataMultiplier), GUILayout.Width(75));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Flight Data Engineer Multiplier", "Overall difficulty slider\n" +
                        "Increases or decreases the bonus applied to the accumulation of flight data from having Engineers in your crew.\n" +
                        "A setting of 1 is normal difficulty."),
                            GUILayout.Width(200)
                        );
                        if (DrawHorizontalSlider(ref tfScenario.userSettings.flightDataEngineerMultiplier, 0.5f, 2f, GUILayout.Width(150)))
                        {
                            tfScenario.userSettings.Save();
                        }
                        GUILayout.Label(String.Format("{0,5:f2}", tfScenario.userSettings.flightDataEngineerMultiplier), GUILayout.Width(75));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.userSettings.singleScope, "Use a single scope for all data", Styles.styleToggle))
                        {
                            tfScenario.userSettings.Save();
                        }
                        GUILayout.EndHorizontal();
                        break;
                    case 2:
                        GUILayout.BeginHorizontal();
                        if (DrawToggle(ref tfScenario.userSettings.debugLog, "Enable Debugging", Styles.styleToggle))
                        {
                            tfScenario.userSettings.Save();
                        }
                        GUILayout.EndHorizontal();
                        break;
                    case 3:
                        GUILayout.BeginHorizontal();
                        bool saveEnabled = tfScenario.SettingsEnabled;
                        if (DrawToggle(ref saveEnabled, "TestFlight Enabled", Styles.styleToggle))
                        {
                            tfScenario.SettingsEnabled = saveEnabled;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        bool saveMaxData = tfScenario.SettingsAlwaysMaxData;
                        if (DrawToggle(ref saveMaxData, "Parts always have Maximum Data", Styles.styleToggle))
                        {
                            tfScenario.SettingsAlwaysMaxData = saveMaxData;
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
                tfScenario.userSettings.Save();
            }
        }
            
        // GUI EVent Handlers
        void SettingsPage_OnSelectionChanged(MonoBehaviourWindowPlus.DropDownList sender, int oldIndex, int newIndex)
        {
            tfScenario.userSettings.settingsPage = newIndex;
            tfScenario.userSettings.Save();
        }
        void MainWindow_OnWindowMoveComplete(MonoBehaviourWindow sender)
        {
            tfScenario.userSettings.mainWindowPosition = WindowRect;
            tfScenario.userSettings.Save();
        }
    }
}

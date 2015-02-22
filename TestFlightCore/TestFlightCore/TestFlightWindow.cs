using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

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
        private int lastPartCount = 0;
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
                "Miscellaneous"
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
            float windowHeight = 10f;;
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
            Log("Recalculating Window Bounds");
            CalculateWindowBounds();

            base.RepeatingWorker();
        }
        internal override void DrawWindow(Int32 id)
        {
            Dictionary<Guid, MasterStatusItem> masterStatus = tfManager.GetMasterStatus();
            GUIContent settingsButton = new GUIContent(TestFlight.Resources.btnChevronDown, "Open Settings Panel");
            GUILayout.BeginVertical();
            if (tfScenario.userSettings.displaySettingsWindow)
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
                    tfScenario.userSettings.displaySettingsWindow = !tfScenario.userSettings.displaySettingsWindow;
                    CalculateWindowBounds();
                    tfScenario.userSettings.Save();
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
                    tfScenario.userSettings.displaySettingsWindow = !tfScenario.userSettings.displaySettingsWindow;
                    CalculateWindowBounds();
                    tfScenario.userSettings.Save();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                // Display information on active vessel
                Guid currentVessel = FlightGlobals.ActiveVessel.id;

                if (tfScenario.userSettings.showFailedPartsOnlyInMSD)
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
                tfScenario.userSettings.currentMSDScrollPosition = GUILayout.BeginScrollView(tfScenario.userSettings.currentMSDScrollPosition);
                foreach (PartStatus status in masterStatus[currentVessel].allPartsStatus)
                {
                    // Display part data
                    //                    GUILayout.Label(String.Format("{0,50}", status.partName));
                    //                    GUILayout.Label(String.Format("{0,7:F2}du", status.flightData));
                    //                    GUILayout.Label(String.Format("{0,7:F2}%", status.reliability));

                    if (tfScenario.userSettings.showFailedPartsOnlyInMSD && status.activeFailure == null)
                        continue;
                    if (tfScenario.userSettings.showFailedPartsOnlyInMSD && status.acknowledged)
                        continue;

                    GUILayout.BeginHorizontal();
                    string partDisplay;
                    // Part Name
                    string tooltip = status.repairRequirements;
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
                        if (status.activeFailure == null)
                            partDisplay = String.Format("<color=#859900ff>{0,-30}</color>", "OK");
                        else
                        {
                            if (status.timeToRepair > 0)
                            {
                                if (status.activeFailure.GetFailureDetails().severity == "major")
                                    partDisplay = String.Format("<color=#dc322fff>{0,-30}</color>", GetColonFormattedTime(status.timeToRepair));
                                else
                                    partDisplay = String.Format("<color=#b58900ff>{0,-30}</color>", GetColonFormattedTime(status.timeToRepair));
                            }
                            else
                            {
                                if (status.activeFailure.GetFailureDetails().severity == "major")
                                    partDisplay = String.Format("<color=#dc322fff>{0,-30}</color>", status.activeFailure.GetFailureDetails().failureTitle);
                                else
                                    partDisplay = String.Format("<color=#b58900ff>{0,-30}</color>", status.activeFailure.GetFailureDetails().failureTitle);
                            }
                        }
                        GUILayout.Label(partDisplay, GUILayout.Width(100));
                    }
                    if (status.activeFailure != null)
                    {
                        if (status.activeFailure.CanAttemptRepair() && status.timeToRepair <= 0)
                        {
                            if (GUILayout.Button("R", GUILayout.Width(38)))
                            {
                                // attempt repair
                                status.flightCore.AttemptRepair();
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
                    tfScenario.userSettings.displaySettingsWindow = !tfScenario.userSettings.displaySettingsWindow;
                    CalculateWindowBounds();
                    tfScenario.userSettings.Save();
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
                        if (DrawHorizontalSlider(ref tfScenario.userSettings.flightDataMultiplier, 0.5, 2, GUILayout.Width(150)))
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
                        if (DrawHorizontalSlider(ref tfScenario.userSettings.flightDataEngineerMultiplier, 0.5, 2, GUILayout.Width(150)))
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

        // nicked from magico13's Kerbal Construction Time mod with permission
        // https://github.com/magico13/KCT/blob/master/Kerbal_Construction_Time/KCT_Utilities.cs#L46-L73
        public static string GetColonFormattedTime(double time)
        {
            if (time > 0)
            {
                StringBuilder formatedTime = new StringBuilder();
                if (GameSettings.KERBIN_TIME)
                {
                    formatedTime.AppendFormat("{0,2:00}<b>:</b>", Math.Floor(time / 21600));
                    time = time % 21600;
                }
                else
                {
                    formatedTime.AppendFormat("{0,2:00}<b>:</b>", Math.Floor(time / 86400));
                    time = time % 86400;
                }
                formatedTime.AppendFormat("{0,2:00}<b>:</b>", Math.Floor(time / 3600));
                time = time % 3600;
                formatedTime.AppendFormat("{0,2:00}<b>:</b>", Math.Floor(time / 60));
                time = time % 60;
                formatedTime.AppendFormat("{0,2:00}", time);
                return formatedTime.ToString();
            }
            else
            {
                return "00<b>:</b>00<b>:</b>00<b>:</b>00";
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

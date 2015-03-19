using System;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using KSPPluginFramework;

using TestFlightAPI;

namespace TestFlightCore
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class TestFlightKSCWindow : MonoBehaviourWindowPlus
    {
        internal static TestFlightKSCWindow Instance;
        internal TestFlightManagerScenario tfScenario = null;
        internal bool isReady = false;
        private ApplicationLauncherButton appLauncherButton;
        bool stickyWindow = false;
        private DropDownList ddlSettingsPage = null;

        internal void Log(string message)
        {
            if (TestFlightManagerScenario.Instance == null || TestFlightManagerScenario.Instance.userSettings == null)
                return;

            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = "TestFlightKSCWindow: " + message;
            TestFlightUtil.Log(message, debug);
        }

        internal override void Start()
        {
            Log("Initializing KSC Window Hook");
            Instance = this;
            StartCoroutine("ConnectToScenario");
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

        internal override void Update()
        {
        }

        internal void Startup()
        {
            CalculateWindowBounds();
            WindowMoveEventsEnabled = true;
            ClampToScreen = true;
            TooltipsEnabled = true;
            TooltipMouseOffset = new Vector2d(10, 10);
            TooltipStatic = true;
            WindowCaption = "";
            StartCoroutine("AddToToolbar");
            TestFlight.Resources.LoadTextures();
            List<string> views = new List<string>()
            {
                "Visual Settings",
                "Difficulty/Performance Settings",
                "Miscellaneous"
            };
            ddlSettingsPage = new DropDownList(views, this);
            ddlManager.AddDDL(ddlSettingsPage);
            ddlSettingsPage.OnSelectionChanged += SettingsPage_OnSelectionChanged;
            isReady = true;
        }

        internal void CalculateWindowBounds()
        {
            if (appLauncherButton == null)
                return;
            if (tfScenario == null)
                return;

            float windowWidth = 500f;
            float left = Screen.width - windowWidth - 75f;
            float windowHeight = 100f;
            float top = 40f;
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
                Log("Loading icon texture");
                Texture iconTexture = GameDatabase.Instance.GetTexture("TestFlight/Resources/AppLauncherIcon", false);
                if (iconTexture == null)
                {
                    throw new Exception("TestFlight MasterStatusDisplay: Failed to load icon texture");
                }
                Log("Creating icon on toolbar");
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
                    OpenWindow,
                    CloseWindow,
                    HoverInButton,
                    HoverOutButton,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.SPACECENTER,
                    iconTexture);
                ApplicationLauncher.Instance.AddOnHideCallback(HideButton);
                ApplicationLauncher.Instance.AddOnRepositionCallback(RepostionWindow);
            }
            catch (Exception e)
            {
                Log("Unable to add button to application launcher: " + e.Message);
                throw e;
            }
        }
        void OpenWindow()
        {
            CalculateWindowBounds();
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
        }
        void HoverInButton()
        {
            CalculateWindowBounds();
            Visible = true;
        }
        void HoverOutButton()
        {
            if (!stickyWindow)
                Visible = false;
        }
        internal override void OnGUIOnceOnly()
        {
            Log("Initializing GUI styles and skins");
            Styles.InitStyles();
            Styles.InitSkins();
            SkinsLibrary.SetCurrent("SolarizedDark");
            SkinsLibrary.CurrentSkin.label.wordWrap = true;
        }
        internal override void DrawWindow(int id)
        {
            if (!isReady)
                return;

            GUILayout.BeginVertical();
            string[] pages = { "R&D", "Settings" };
            tfScenario.userSettings.kscWindowPage = GUILayout.Toolbar(tfScenario.userSettings.kscWindowPage, pages);

            switch (tfScenario.userSettings.kscWindowPage)
            {
                case 0:
                    GUILayout.Label("Research & Development");
                    GUILayout.Label("Here you can allocate R&D teams to working on improving your hardware.\nMouse over for help.");
//                    tfScenario.userSettings.currentResearchScrollPosition = GUILayout.BeginScrollView(tfScenario.userSettings.currentResearchScrollPosition);
                    break;
                case 1:
                    if (ddlSettingsPage == null)
                    {
                        break;
                    }
                    ddlSettingsPage.styleListBox = Styles.styleDropDownListBoxUnity;
                    ddlSettingsPage.styleListBlocker = Styles.styleDropDownListBoxUnity;
                    ddlSettingsPage.SelectedIndex = tfScenario.userSettings.settingsPage;
                    ddlSettingsPage.DrawButton();

                    switch (tfScenario.userSettings.settingsPage)
                    {
                        case 0:
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Visual settings for the Master Status Display can be set in the settings page in the Flight scene");
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
                    }
                    break;
            }
            GUILayout.EndVertical();

        }
        // GUI EVent Handlers
        void SettingsPage_OnSelectionChanged(MonoBehaviourWindowPlus.DropDownList sender, int oldIndex, int newIndex)
        {
            tfScenario.userSettings.settingsPage = newIndex;
            tfScenario.userSettings.Save();
        }
    }
}
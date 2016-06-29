using System;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using KSP.UI.Screens;

using KSPPluginFramework;

using TestFlightAPI;
using KSPAssets.Loaders;

namespace TestFlightCore
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class TestFlightKSCWindow : MonoBehaviourWindowPlus
    {
        internal static TestFlightKSCWindow Instance;
        internal TestFlightManagerScenario tfScenario = null;
        internal TestFlightRnDScenario tfRnDScenario = null;
        internal bool isReady = false;
        private ApplicationLauncherButton appLauncherButton;
        bool stickyWindow = false;
        private DropDownList ddlSettingsPage = null;

        bool assetBundleLoaded = false;
        Canvas kscCanvas = null;

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
            // Load out AssetBundle
            AssetLoader.LoadAssets(AssetLoaded, AssetLoader.GetAssetDefinitionWithName("TestFlight/testflight", "TFKSCCanvas"));
            StartCoroutine("ConnectToScenario");
        }

        void AssetLoaded(AssetLoader.Loader loader)
        {
            // You get a object that contains all the object that match your laoding request
            for (int i = 0; i < loader.definitions.Length; i++ )
            {
                UnityEngine.Object o = loader.objects[i];
                if (o == null)
                    continue;

                if (o.GetType() == typeof(Canvas))
                    kscCanvas = o as Canvas;
            }
            assetBundleLoaded = true;
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

            while (TestFlightRnDScenario.Instance == null)
            {
                yield return null;
            }

            tfRnDScenario = TestFlightRnDScenario.Instance;
            while (!tfRnDScenario.isReady)
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
                "Miscellaneous",
                "SaveGame Settings"
            };
            ddlSettingsPage = new DropDownList(views, this);
            ddlManager.AddDDL(ddlSettingsPage);
            ddlSettingsPage.OnSelectionChanged += SettingsPage_OnSelectionChanged;
            isReady = true;
        }

        internal void CalculateWindowBounds()
        {
            Log("CalculateWindowBounds PreCheck");
            if (appLauncherButton == null)
                return;
            if (tfScenario == null)
                return;

            Log("CalculateWindowBounds");
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
                CalculateWindowBounds();
            }
            catch (Exception e)
            {
                Log("Unable to add button to application launcher: " + e.Message);
                throw e;
            }
        }
        void OpenWindow()
        {
            Log("Open Window");
            CalculateWindowBounds();
            Visible = true;
            stickyWindow = true;
        }
        void CloseWindow()
        {
            Log("Close Window");
            Visible = false;
            stickyWindow = false;
        }
        void HideButton()
        {
            ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
        }
        void RepostionWindow()
        {
            Log("Reposition Window");
            CalculateWindowBounds();
        }
        void HoverInButton()
        {
            Log("Hover In");
            CalculateWindowBounds();
            Visible = true;
        }
        void HoverOutButton()
        {
            Log("Hover Out");
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
                    List<string> partsInResearch = tfRnDScenario.GetPartsInResearch();
                    if (!tfScenario.SettingsEnabled)
                        GUILayout.Label("R&D is not available because TestFlight is disabled in this save.\nYou can enable it from the settings tab.");
                    else if (partsInResearch == null || partsInResearch.Count == 0)
                        GUILayout.Label("Here you can manage engineering teams working on your hardware.\nYou can start new research programs from the VAB.");
                    else
                    {
                        GUILayout.BeginVertical();
                        foreach (string partInResearch in partsInResearch)
                        {
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button("Stop", GUILayout.Width(50)))
                            {
                                tfRnDScenario.RemoveResearch(partInResearch);
                            }
                            if (tfRnDScenario.GetPartResearchState(partInResearch))
                            {
                                if (GUILayout.Button("Pause", GUILayout.Width(75)))
                                {
                                    tfRnDScenario.SetPartResearchState(partInResearch, false);
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("Resume", GUILayout.Width(75)))
                                {
                                    tfRnDScenario.SetPartResearchState(partInResearch, true);
                                }
                            }
                            GUILayout.Label(partInResearch);
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
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
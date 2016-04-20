using System;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using KSP.UI.Screens;
using KSPPluginFramework;

using TestFlightAPI;

namespace TestFlightCore
{
    public class TestFlightEditorInfoWindow : MonoBehaviour
    {
        private bool show = false;
        private Rect position;
        private Part selectedPart;

        public void OnGUI()
        {
            if (selectedPart == null)
                return;
            ITestFlightCore core = TestFlightUtil.GetCore(selectedPart);
            if (core == null)
                return;

            position = GUILayout.Window(GetInstanceID(), position, DrawWindow, String.Empty, Styles.styleEditorPanel);
        }

        public void Update()
        {
            if (EditorLogic.RootPart == null || EditorLogic.fetch.editorScreen != EditorScreen.Parts)
            {
                return;
            }

            position.x = Mathf.Clamp(Input.mousePosition.x - 20f - position.width, 0.0f, Screen.width - position.width);
            position.y = Mathf.Clamp(Screen.height - Input.mousePosition.y, 0.0f, Screen.height - position.height);
            selectedPart = EditorLogic.fetch.ship.parts.Find(p => p.stackIcon.Highlighted) ?? EditorLogic.SelectedPart;
            if (selectedPart != null)
            {
                if (!show && Input.GetMouseButtonDown(2))
                {
                    show = true;
                }
                else if (show && Input.GetMouseButtonDown(2))
                {
                    show = false;
                    position.height = 0;
                    position.width = 0;
                }
            } // End selectedPart
            else
                show = false;
        }

        public void DrawWindow(int windowID)
        {
            if (selectedPart != null)
            {
                ITestFlightCore core = TestFlightUtil.GetCore(selectedPart);
                if (core != null)
                {
                    GUILayout.Label(selectedPart.partInfo.title, Styles.styleEditorTitle);
                    if (show)
                    {
                        List<string> infoParts = core.GetTestFlightInfo();
                        GUILayout.BeginVertical();
                        foreach (string info in infoParts)
                        {
                            GUILayout.Label(info, Styles.styleEditorText);
                        }
                        GUILayout.EndVertical();
                    }
                    else
                    {
                        GUILayout.Space(2.0f);
                        GUILayout.Label("Middle click to show TestFlight info...", Styles.styleEditorText);
                    }
                }
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class TestFlightEditorWindow : MonoBehaviourWindowPlus
    {
        internal static TestFlightEditorWindow Instance;
        internal TestFlightEditorInfoWindow infoWindow = null;
        private bool locked = false;
        private Part _selectedPart;
        internal Part SelectedPart
        {
            set
            {
                if (!locked)
                {
                    this._selectedPart = value;
                    CalculateWindowBounds();
                }
            }
            get
            {
                return this._selectedPart;
            }
        }
        internal TestFlightManagerScenario tfScenario = null;
        internal TestFlightRnDScenario tfRnDScenario = null;
        internal bool isReady = false;
        private ApplicationLauncherButton appLauncherButton;
        bool stickyWindow = false;
        private string selectedAlias = "";

        public void LockPart(Part partToLock, string alias)
        {
            if (!locked)
            {
                locked = true;
                SelectedPart = partToLock;
                selectedAlias = alias;
                return;
            }

            if (partToLock == SelectedPart)
                locked = false;
            else
            {
                locked = false;
                SelectedPart = partToLock;
                locked = true;
            }
        }

        public void UnlockPart()
        {
            locked = false;
        }

        internal void Log(string message)
        {
            if (TestFlightManagerScenario.Instance == null || TestFlightManagerScenario.Instance.userSettings == null)
                return;

            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = "TestFlightEditor: " + message;
            TestFlightUtil.Log(message, debug);
        }

        internal override void Awake()
        {
            infoWindow = this.gameObject.AddComponent<TestFlightEditorInfoWindow>();
        }

        internal override void OnDestroy()
        {
            if (infoWindow != null)
                Destroy(infoWindow);
        }

        internal override void Start()
        {
            Log("TestFlightEditor: Initializing Editor Hook");
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
            if (locked)
                return;

            Part selectedPart = EditorLogic.SelectedPart;

            if (selectedPart == null)
                selectedPart = EditorLogic.fetch.ship.parts.Find(p => p.stackIcon.Highlighted);

            if (selectedPart != null)
                SelectedPart = selectedPart;
        }

        internal void Startup()
        {
            Log("Startup");
            CalculateWindowBounds();
            DragEnabled = !tfScenario.userSettings.editorWindowLocked;
            WindowMoveEventsEnabled = true;
            WindowMoveCompleteAfter = 0.1f;
            ClampToScreen = true;
            TooltipsEnabled = true;
            TooltipMouseOffset = new Vector2d(10, 10);
            TooltipStatic = true;
            WindowCaption = "";
//            StartCoroutine("AddToToolbar");
            onWindowMoveComplete += EditorWindow_OnWindowMoveComplete;
            isReady = true;
        }

        internal void CalculateWindowBounds()
        {
            if (tfScenario == null)
                return;

            float windowWidth = 250f;
            float left = Screen.width - windowWidth - 75f;
            float windowHeight = 150f;

            windowHeight += 20f;
            float top = Screen.height - windowHeight - 60f;

            left = tfScenario.userSettings.editorWindowPosition.xMin;
            top = tfScenario.userSettings.editorWindowPosition.yMin;
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
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
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
            Debug.Log("TestFlight MasterStatusDisplay: RepositionWindow");
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

        public void ToggleWindow()
        {
            Visible = !Visible;
        }

        internal override void OnGUIOnceOnly()
        {
            Styles.InitStyles();
            Styles.InitSkins();
            SkinsLibrary.SetCurrent("TestFlightEditor");
        }
        internal override void DrawWindow(int id)
        {
            if (!isReady)
                return;

            if (SelectedPart == null)
                return;

            ITestFlightCore core = null;
            GUILayout.BeginVertical();
            GUILayout.Label(String.Format("Selected Part: {0}", selectedAlias), Styles.styleEditorTitle);

            float flightData = TestFlightManagerScenario.Instance.GetFlightDataForPartName(selectedAlias);
            if (flightData < 0f)
                flightData = 0f;
            core = TestFlightUtil.GetCore(SelectedPart, selectedAlias);
            if (core != null)
            {
                core.InitializeFlightData(flightData);
                GUILayout.BeginHorizontal();
                double failureRate = core.GetBaseFailureRate();
                Log(String.Format("Failure rate is {0:f2}", failureRate));
                String mtbfString = core.FailureRateToMTBFString(failureRate, TestFlightUtil.MTBFUnits.SECONDS, 999);
                GUILayout.Label(String.Format("{0,-7:F2}<b>du</b>", flightData), GUILayout.Width(75));
                GUILayout.Label(String.Format("{0,-5:F2} MTBF", mtbfString), GUILayout.Width(125));
                GUILayout.EndHorizontal();
                Log("Checking for RnD Status");
                string partName = selectedAlias;
                float maxRnDData = core.GetMaximumRnDData();
                if (flightData >= maxRnDData)
                {
                    Log("Part has reached Max RnD");
                    GUILayout.Label("Part flight data meets or exceeds maximum lab R&D amount", Styles.styleEditorText);
                }
                else
                {
                    Log("Part is RnD Eligible");
                    if (!tfRnDScenario.IsPartBeingResearched(partName))
                    {
                        Log("Part is not being researched.  Show research buttons");
                        int frequency = (int)tfRnDScenario.updateFrequency;
                        Log(String.Format("Update Frequency is {0}",frequency)); 
                        TimeSpan span = new TimeSpan(0, 0, frequency);
                        string cycleString;
                        if (span.TotalDays > 1d)
                        {
                            cycleString = String.Format("{0:F2} days", span.TotalDays);
                        }
                        else
                        {
                            cycleString = String.Format("{0:F2} day", span.TotalDays);
                        }
                        GUILayout.Label("Hire Research Team", Styles.styleEditorTitle);
                        GUILayout.Label("Research teams deduct funds and add flight data to your part", Styles.styleEditorText);
                        GUILayout.Label("A research cycle lasts <b>" + cycleString + "</b>", Styles.styleEditorText);
                        GUILayout.Label("At the end of each cycle, funds will be deducted and data added", Styles.styleEditorText);
                        GUILayout.Label("The costs and research rate varies by team", Styles.styleEditorText);

                        List<TestFlightRNDTeamSettings> teams = tfRnDScenario.GetAvailableTeams();
                        if (teams != null)
                        {
                            int numTeams = teams.Count;
                            for (int i = 0; i < numTeams; i++)
                            {
                                GUILayout.BeginHorizontal();
                                if (GUILayout.Button(String.Format("Hire Team"), GUILayout.Width(100)))
                                {
                                    tfRnDScenario.AddResearchTeam(SelectedPart, selectedAlias, i);
                                }
                                Log(String.Format("cycle is {0}", cycleString));
                                float points = teams[i].points;
                                float cost = teams[i].costFactor * points;
                                points = points * core.GetRnDRate();
                                cost = cost * core.GetRnDCost();
                                GUILayout.Label(String.Format("<b>{0,7:F2}</b> data, <b>{1,7:F2}</b> funds", points, cost), Styles.styleEditorTextAligned);
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                    else
                    {
                        Log("Part is already being researched.  Show button to stop");
                        if (GUILayout.Button("Stop Research", GUILayout.Width(200)))
                        {
                            tfRnDScenario.RemoveResearch(partName);
                        }
                    }
                }
            }
            if (DrawToggle(ref tfScenario.userSettings.editorWindowLocked, "Lock Window", Styles.styleToggle))
            {
                if (tfScenario.userSettings.editorWindowLocked)
                {
                    CalculateWindowBounds();
                    tfScenario.userSettings.editorWindowPosition = WindowRect;
                    DragEnabled = false;
                }
                else
                {
                    DragEnabled = true;
                }
                tfScenario.userSettings.Save();
            }
            GUILayout.EndVertical();
        }
        void EditorWindow_OnWindowMoveComplete(MonoBehaviourWindow sender)
        {
            Log("Saving editor window position");
            tfScenario.userSettings.editorWindowPosition = WindowRect;
            tfScenario.userSettings.Save();
        }
        internal override void OnGUIEvery()
        {
            base.OnGUIEvery();
        }
    }
    public static class Drawing
    {
        private static Texture2D lineTex = null;
        public static void DrawLine(Rect rect) { DrawLine(rect, GUI.contentColor, 1.0f); }
        public static void DrawLine(Rect rect, Color color) { DrawLine(rect, color, 1.0f); }
        public static void DrawLine(Rect rect, float width) { DrawLine(rect, GUI.contentColor, width); }
        public static void DrawLine(Rect rect, Color color, float width) { DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x + rect.width, rect.y + rect.height), color, width); }
        public static void DrawLine(Vector2 pointA, Vector2 pointB) { DrawLine(pointA, pointB, GUI.contentColor, 1.0f); }
        public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color) { DrawLine(pointA, pointB, color, 1.0f); }
        public static void DrawLine(Vector2 pointA, Vector2 pointB, float width) { DrawLine(pointA, pointB, GUI.contentColor, width); }
        public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
        {
            // Save the current GUI matrix and color, since we're going to make changes to it.
            Matrix4x4 matrix = GUI.matrix;
            Color savedColor = GUI.color;
            // Generate a single pixel texture if it doesn't exist
            if (!lineTex) { lineTex = new Texture2D(1, 1); lineTex.SetPixel(0, 0, color); lineTex.Apply(); }
            // and set the GUI color to the color parameter
            GUI.color = color;
            // Determine the angle of the line.
            Single angle = Vector3.Angle(pointB - pointA, Vector2.right);
            // Vector3.Angle always returns a positive number.
            // If pointB is above pointA, then angle needs to be negative.
            if (pointA.y > pointB.y) { angle = -angle; }
            // Set the rotation for the line. The angle was calculated with pointA as the origin.
            GUIUtility.RotateAroundPivot(angle, pointA);
            // Finally, draw the actual line. we've rotated the GUI, so now we draw the length and width
            GUI.DrawTexture(new Rect(pointA.x, pointA.y, (pointA-pointB).magnitude, width), lineTex);
            // We're done. Restore the GUI matrix and GUI color to whatever they were before.
            GUI.matrix = matrix;
            GUI.color = savedColor;
        }
    }
}

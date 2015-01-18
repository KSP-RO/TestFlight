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
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class TestFlightEditorWindow : MonoBehaviourExtended
    {
        internal static TestFlightEditorWindow Instance;
        internal Part selectedPart;
        internal bool showGraph = true;
        internal Rect WindowRect;
        internal bool Visible;
        internal TestFlightManagerScenario tfScenario = null;
        internal bool isReady = false;
        private ApplicationLauncherButton appLauncherButton;
        bool stickyWindow = false;


        internal override void Start()
        {
            LogFormatted_DebugOnly("TestFlightEditor: Initializing Editor Hook");
            EditorPartList.Instance.iconPrefab.gameObject.AddComponent<TestFlightEditorHook>();
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

        internal void Startup()
        {
            CalculateWindowBounds();
            StartCoroutine("AddToToolbar");
            isReady = true;
        }

        internal void CalculateWindowBounds()
        {
            WindowRect = new Rect(Screen.width - 575, Screen.height - 250, 350, 200);
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

        internal override void OnGUIOnceOnly()
        {
            Styles.InitStyles();
            Styles.InitSkins();
            SkinsLibrary.SetCurrent("SolarizedDark");
        }
        internal override void OnGUIEvery()
        {
            base.OnGUIEvery();
            if (!isReady)
                return;

            if (Visible)
            {
                ITestFlightCore core = null;
                GUILayout.BeginArea(WindowRect, SkinsLibrary.CurrentSkin.GetStyle("HUD"));
                if (selectedPart != null)
                {
                    GUILayout.BeginVertical();
                    GUILayout.Label(String.Format("Selected Part: {0}", selectedPart.name));

                    tfScenario.settings.currentEditorScrollPosition = GUILayout.BeginScrollView(tfScenario.settings.currentEditorScrollPosition);
                    PartFlightData partData = tfScenario.GetFlightDataForPartName(selectedPart.name);
                    if (partData != null)
                    {
                        foreach (PartModule pm in selectedPart.Modules)
                        {
                            core = pm as ITestFlightCore;
                            if (core != null)
                            {
                                break;
                            }
                        }
                        if (core != null && showGraph)
                        {
                            List<TestFlightData> flightData = partData.GetFlightData();
                            core.InitializeFlightData(flightData, 1.0);
                            foreach (TestFlightData data in flightData)
                            {
                                GUILayout.BeginHorizontal();
                                double failureRate = core.GetBaseFailureRateForScope(data.scope);
                                String mtbfString = core.FailureRateToMTBFString(failureRate, TestFlightUtil.MTBFUnits.SECONDS, 999);
                                // 10 characters for body max plus 10 characters for situation plus underscore = 21 characters needed for longest scope string
                                GUILayout.Label(core.PrettyStringForScope(data.scope), GUILayout.Width(125));
                                GUILayout.Label(String.Format("{0,-7:F2}<b>du</b>", data.flightData), GUILayout.Width(75));
                                //TODO this needs to change to be MTBF once the new system goes in
//                                GUILayout.Label(String.Format("{0,-5:F2} MTBF", mtbfString), GUILayout.Width(125));
                                GUILayout.Label(String.Format("{0,-5:F2}<b>%R</b>", failureRate), GUILayout.Width(125));
                                FloatCurve curve = null;
                                curve = core.GetBaseReliabilityCurveForScope(data.scope);
                                if (curve != null)
                                {
                                    if (GUILayout.Button("G", GUILayout.Width(38)))
                                    {
                                        // Display graph for scope
                                        Vector2 startPoint = new Vector2(5, 200);
                                        Vector2 endPoint = new Vector2(100, 500);
                                        Color lineColor = XKCDColors.Yellow;
                                        float start = curve.minTime;
                                        float end = curve.maxTime;
                                        // fit the curve
                                        Drawing.DrawLine(startPoint, endPoint, lineColor, 5f);
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("No flight data has been recorded for this part.");
                    }
                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                }
                else
                {
                    GUILayout.BeginVertical();
                    GUILayout.Label("This window will show accumulated flight data and failure rate details for a part when you mouse over it in the parts bin.");
                    GUILayout.EndVertical();
                }
                GUILayout.EndArea();
            }
        }
        internal static Boolean DrawToggle(ref Boolean blnVar, String ButtonText, GUIStyle style, params GUILayoutOption[] options)
        {
            Boolean blnOld = blnVar;
            blnVar = GUILayout.Toggle(blnVar, ButtonText, style, options);

            return DrawResultChanged(blnOld, blnVar, "Toggle");
        }
        private static Boolean DrawResultChanged<T>(T Original, T New, String Message) 
        {
            if (Original.Equals(New)) {
                return false;
            } else {
                LogFormatted_DebugOnly("{0} Changed. {1}->{2}", Message, Original.ToString(), New.ToString());
                return true;
            }

        }
    }
    // MEGA thanks to xxEvilReeperxx for pointing me in the right direction on this!
    // Never would have found out that KSP uses EZGUI rather than stock Unity GUI on my own.
    class TestFlightEditorHook : MonoBehaviourExtended
    {
        AvailablePart selectedPart;
        bool mouseFlag = false;

        internal override void Start()
        {
            GetComponent<UIButton>().AddInputDelegate(OnInput);
            selectedPart = GetComponent<EditorPartIcon>().partInfo;
            LogFormatted_DebugOnly("TestFlightEditor: Added input delegate to " + selectedPart.partPrefab.name);
        }

        internal void OnInput(ref POINTER_INFO ptr)
        {
            switch (ptr.evt)
            {
                case POINTER_INFO.INPUT_EVENT.PRESS:
                    if (Input.GetMouseButtonDown(0))
                    {
                        // Left button press
                        Debug.Log("LEFT");
                    }
                    else if (Input.GetMouseButtonDown(1))
                    {
                        // Right button press
                        Debug.Log("RIGHT");
                    }
                    break;
                case POINTER_INFO.INPUT_EVENT.MOVE:
                    if (!mouseFlag)
                    {
                        mouseFlag = true;
                        TestFlightEditorWindow.Instance.selectedPart = selectedPart.partPrefab;
//                        TestFlightEditorWindow.Instance.Visible = true;
                    }
                    break;
                case POINTER_INFO.INPUT_EVENT.MOVE_OFF:
                    mouseFlag = false;
                    TestFlightEditorWindow.Instance.selectedPart = null;
//                    TestFlightEditorWindow.Instance.Visible = false;
                    break;
            }
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

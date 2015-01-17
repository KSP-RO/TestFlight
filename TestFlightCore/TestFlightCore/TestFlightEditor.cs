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
            isReady = true;
        }

        internal void CalculateWindowBounds()
        {
            if (showGraph)
                WindowRect = new Rect(Screen.width - 550, Screen.height - 550, 500, 500);
            else
                WindowRect = new Rect(Screen.width - 550, Screen.height - 300, 500, 250);
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
                GUILayout.BeginVertical();
                GUILayout.Label(String.Format("Selected Part: {0}", selectedPart.name));
                tfScenario.settings.editorShowGraph = DrawToggle(ref tfScenario.settings.editorShowGraph, "Show Reliability Graph", Styles.styleToggle);
                tfScenario.settings.editorShowOnDemand = DrawToggle(ref tfScenario.settings.editorShowOnDemand, "Show Window on Demand", Styles.styleToggle);

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
                    if (core != null)
                    {
                        List<TestFlightData> flightData = partData.GetFlightData();
                        core.InitializeFlightData(flightData, 1.0);
                        foreach (TestFlightData data in flightData)
                        {
                            GUILayout.BeginHorizontal();
                            double failureRate = core.GetBaseFailureRateForScope(data.scope);
                            String mtbfString = core.FailureRateToMTBFString(failureRate, TestFlightUtil.MTBFUnits.SECONDS, 999);
                            // 10 characters for body max plus 10 characters for situation plus underscore = 21 characters needed for longest scope string
                            GUILayout.Label(data.scope, GUILayout.Width(150));
                            GUILayout.Label(String.Format("{0,-7:F2}<b>du</b>", data.flightData), GUILayout.Width(75));
                            GUILayout.Label(String.Format("{0,-5:F2} MTBF", mtbfString), GUILayout.Width(150));
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                else
                {
                    GUILayout.Label("No flight data has been recorded for this part.");
                }
                GUILayout.EndScrollView();

                GUILayout.Space(250);
                GUILayout.EndVertical();
                Vector2 startPoint = new Vector2(5, 200);
                Vector2 endPoint = new Vector2(100, 500);
                Color lineColor = XKCDColors.Yellow;
                Drawing.DrawLine(startPoint, endPoint, lineColor, 5f);
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
                case POINTER_INFO.INPUT_EVENT.MOVE:
                    if (!mouseFlag)
                    {
                        mouseFlag = true;
                        TestFlightEditorWindow.Instance.selectedPart = selectedPart.partPrefab;
                        TestFlightEditorWindow.Instance.Visible = true;
                    }
                    break;
                case POINTER_INFO.INPUT_EVENT.MOVE_OFF:
                    mouseFlag = false;
                    TestFlightEditorWindow.Instance.Visible = false;
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

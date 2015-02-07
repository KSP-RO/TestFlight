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
    public class TestFlightHUD : MonoBehaviourWindowPlus
    {
        private TestFlightManagerScenario tfScenario;
        private TestFlightWindow parentWindow;
        private int lastPartCount = 0;

        internal override void Start()
        {
            onWindowMoveComplete += Window_OnWindowMoveComplete;
        }

        internal void Log(string message)
        {
            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = "TestFlightHUD: " + message;
            TestFlightUtil.Log(message, debug);
        }

        internal TestFlightHUD Startup(TestFlightWindow parent)
        {
            Log("TestFlightHUD Startup()");
            parentWindow = parent;
            tfScenario = TestFlightManagerScenario.Instance;
            WindowMoveEventsEnabled = true;
            onWindowMoveComplete += Window_OnWindowMoveComplete;
            return this;
        }

        internal void Shutdown()
        {
            Visible = false;
            onWindowMoveComplete -= Window_OnWindowMoveComplete;
            WindowMoveEventsEnabled = false;
        }

        internal override void Awake()
        {
            base.Awake();
        }

        internal override void OnGUIOnceOnly()
        {
            // Default position and size -- will get proper bounds calculated when needed
            WindowRect = new Rect(tfScenario.userSettings.flightHUDPosition.xMin, tfScenario.userSettings.flightHUDPosition.yMin, 50f, 50f);
            DragEnabled = true;
            ClampToScreen = true;
            TooltipsEnabled = true;
            TooltipMouseOffset = new Vector2d(10, 10);
            TooltipStatic = true;
            WindowCaption = "";
            WindowStyle = SkinsLibrary.CurrentSkin.GetStyle("HUD");
            Visible = true;
            onWindowMoveComplete += Window_OnWindowMoveComplete;
        }

        internal void CalculateWindowBounds()
        {
            Log("TestFlightHUD Calculating Window Bounds");
            WindowRect = new Rect(tfScenario.userSettings.flightHUDPosition.xMin, tfScenario.userSettings.flightHUDPosition.yMin, 50f, 50f);
        }

        internal override void DrawWindow(Int32 id)
        {
            GUILayout.BeginVertical();
            Dictionary<Guid, MasterStatusItem> masterStatus = parentWindow.tfManager.GetMasterStatus();

            if (masterStatus == null || masterStatus.Count <= 0)
                return;

            // Display information on active vessel
            Guid currentVessl = FlightGlobals.ActiveVessel.id;
            if (masterStatus[currentVessl].allPartsStatus.Count(ps => ps.activeFailure != null) < lastPartCount)
            {
                Log("TestFlightHUD less parts to display than last time.  Need to recalculate window bounds");
                CalculateWindowBounds();
            }
            lastPartCount = masterStatus[currentVessl].allPartsStatus.Count(ps => ps.activeFailure != null);

            foreach (PartStatus status in masterStatus[currentVessl].allPartsStatus)
            {
                // We only show failed parts in Flight HUD
                if (status.activeFailure == null || status.acknowledged)
                    continue;

                GUILayout.BeginHorizontal();
                // Part Name
                string tooltip = status.activeFailure.GetFailureDetails().failureTitle + "\n";
                if (status.timeToRepair > 0)
                    tooltip += GetColonFormattedTime(status.timeToRepair) + "\n";
                tooltip += status.repairRequirements;
                if (status.activeFailure.GetFailureDetails().severity == "minor")
                    GUILayout.Label(new GUIContent(String.Format("<color=#859900ff>{0}</color>", status.partName), tooltip), GUILayout.Width(200));
                else if (status.activeFailure.GetFailureDetails().severity == "failure")
                    GUILayout.Label(new GUIContent(String.Format("<color=#b58900ff>{0}</color>", status.partName), tooltip), GUILayout.Width(200));
                else if (status.activeFailure.GetFailureDetails().severity == "major")
                    GUILayout.Label(new GUIContent(String.Format("<color=#dc322fff>{0}</color>", status.partName), tooltip), GUILayout.Width(200));
                GUILayout.Space(10);
                if (status.activeFailure != null)
                {
                    if (status.activeFailure.CanAttemptRepair())
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
            GUILayout.EndVertical();
        }
        // nicked from magico13's Kerbal Construction Time mod
        // Hope he doesn't mind!  Just seemed silly to reimplement the wheel here
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
        void Window_OnWindowMoveComplete(MonoBehaviourWindow sender)
        {
            Log("TestFlightHUD Saving window position");
            tfScenario.userSettings.flightHUDPosition = WindowRect;
            tfScenario.userSettings.Save();
        }
    }
}

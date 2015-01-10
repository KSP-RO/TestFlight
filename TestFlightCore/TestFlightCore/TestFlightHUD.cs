using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using KSPPluginFramework;

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

        internal TestFlightHUD Startup(TestFlightWindow parent)
        {
            LogFormatted_DebugOnly("TestFlightHUD Startup()");
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
            WindowRect = new Rect(tfScenario.settings.flightHUDPosition.xMin, tfScenario.settings.flightHUDPosition.yMin, 50f, 50f);
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
            LogFormatted_DebugOnly("TestFlightHUD Calculating Window Bounds");
            WindowRect = new Rect(tfScenario.settings.flightHUDPosition.xMin, tfScenario.settings.flightHUDPosition.yMin, 50f, 50f);
        }

        internal override void DrawWindow(Int32 id)
        {
            GUILayout.BeginVertical();
            Dictionary<Guid, MasterStatusItem> masterStatus = tfScenario.GetMasterStatus();

            if (masterStatus == null || masterStatus.Count <= 0)
                return;

            // Display information on active vessel
            Guid currentVessl = FlightGlobals.ActiveVessel.id;
            if (masterStatus[currentVessl].allPartsStatus.Count(ps => ps.activeFailure != null) < lastPartCount)
            {
                LogFormatted_DebugOnly("TestFlightHUD less parts to display than last time.  Need to recalculate window bounds");
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
                string tooltip = status.activeFailure.GetFailureDetails().failureTitle + "\n" + status.repairRequirements;
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
            GUILayout.EndVertical();
        }

        // GUI EVent Handlers
        void Window_OnWindowMoveComplete(MonoBehaviourWindow sender)
        {
            LogFormatted_DebugOnly("TestFlightHUD Saving window position");
            tfScenario.settings.flightHUDPosition = WindowRect;
            tfScenario.settings.Save();
        }
    }
}

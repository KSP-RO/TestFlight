using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using TestFlightCore.KSPPluginFramework;
using KSP.UI.Screens;


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
            if (TestFlightManagerScenario.Instance == null)
                return;
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
            Dictionary<Guid, MasterStatusItem> masterStatus = parentWindow.tfManager.GetMasterStatus();

            if (masterStatus == null || masterStatus.Count <= 0)
                return;

            // Display information on active vessel
            Guid currentVessl = FlightGlobals.ActiveVessel.id;
            if (!masterStatus.ContainsKey(currentVessl))
                return;
            
//            if (masterStatus[currentVessl].allPartsStatus.Count(ps => ps.activeFailure != null) < lastPartCount)
            if (masterStatus[currentVessl].allPartsStatus.Count < lastPartCount)
            {
                CalculateWindowBounds();
            }
//            lastPartCount = masterStatus[currentVessl].allPartsStatus.Count(ps => ps.activeFailure != null);
            lastPartCount = masterStatus[currentVessl].allPartsStatus.Count;

            GUILayout.BeginVertical();

            foreach (PartStatus status in masterStatus[currentVessl].allPartsStatus)
            {
                // We only show failed parts in Flight HUD
                if (status.failures.Count <= 0)
                    continue;

                GUILayout.BeginHorizontal();
                // Part Name
//                string tooltip = status.activeFailure.GetFailureDetails().failureTitle + "\n";
                string tooltip = "";
//                if (status.timeToRepair > 0)
//                    tooltip += GetColonFormattedTime(status.timeToRepair) + "\n";
//                tooltip += status.repairRequirements;
                ITestFlightFailure latestFailure = status.failures.Last();
                string label = string.Format("<color=#{0}>{1}</color>\n", latestFailure.GetFailureDetails().severity.ToLowerInvariant() == "major" ? "dc322fff" : "b58900ff", status.partName);
                GUILayout.Label(new GUIContent(label, tooltip), GUILayout.Width(200));
//                if (status.activeFailure.GetFailureDetails().severity == "minor")
//                    GUILayout.Label(new GUIContent(String.Format("<color=#859900ff>{0}</color>", status.partName), tooltip), GUILayout.Width(200));
//                else if (status.activeFailure.GetFailureDetails().severity == "failure")
//                    GUILayout.Label(new GUIContent(String.Format("<color=#b58900ff>{0}</color>", status.partName), tooltip), GUILayout.Width(200));
//                else if (status.activeFailure.GetFailureDetails().severity == "major")
//                    GUILayout.Label(new GUIContent(String.Format("<color=#dc322fff>{0}</color>", status.partName), tooltip), GUILayout.Width(200));
                GUILayout.Space(10);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
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

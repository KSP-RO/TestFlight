using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEngine;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightGameSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "TestFlight Options"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "TestFlight"; } }
        public override string DisplaySection { get { return Section; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }

        [GameParameters.CustomParameterUI("Pre-Launch Ignition Failures", toolTip = "Set to enable ignition failures on the Launch Pad.")]
        public bool preLaunchFailures = true;

        [GameParameters.CustomParameterUI("Ignition Chance Penalty For High Dynamic Pressure", toolTip = "Whether engine ignition chance will suffer a penalty based on dynamic pressure.")]
        public bool dynPressurePenalties = true;

        // The following values are persisted to the savegame but are not shown in the difficulty settings UI
        public bool dynPressurePenaltyReminderShown = false;
        public bool restartWindowPenaltyReminderShown = false;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                case GameParameters.Preset.Normal:
                case GameParameters.Preset.Moderate:
                    preLaunchFailures = false;
                    break;
                case GameParameters.Preset.Hard:
                    preLaunchFailures = true;
                    break;
            }
        }
    }
}

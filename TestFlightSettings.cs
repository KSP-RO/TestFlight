using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEngine;

using TestFlightAPI;

namespace TestFlight {
  public class TestFlightGameSettings : GameParameters.CustomParameterNode {
    public override string Title { get { return "TestFlight Options"; } }
    public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
    public override string Section { get { return "TestFlight"; } }
    public override string DisplaySection { get { return Section; } }
    public override int SelectionOrder { get { return 1; } }
    public override bool HasPresets { get { return true; } }
    
    [GameParameters.CustomParameterUI("Pre-Launch Ignition Failures Enabled?", toolTip = "Set to enable ignition failures on the Launch Pad.")]
    public bool preLaunchFailures = true;
    
    public override void SetDifficultyPreset(GameParameters.Preset preset) {
      Debug.Log("Setting difficulty preset");
      switch (preset)
      {
        case GameParameters.Preset.Easy:
            preLaunchFailures = true;
            break;
        case GameParameters.Preset.Normal:
            preLaunchFailures = true;
            break;
        case GameParameters.Preset.Moderate:
            preLaunchFailures = true;
            break;
        case GameParameters.Preset.Hard:
            preLaunchFailures = true;
            break;
        case GameParameters.Preset.Custom:
            preLaunchFailures = true;
            break;
        default:
            preLaunchFailures = true;
            break;
      }
    }
  }
}

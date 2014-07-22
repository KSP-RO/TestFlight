using System;
using System.Collections.Generic;


using UnityEngine;
using KSPPluginFramework;

namespace TestFlight
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class TestFlightManager : MonoBehaviour
    {
        public void Start()
        {
            var game = HighLogic.CurrentGame;
            ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(TestFlightManagerScenario).Name);
            if (psm == null)
            {
                GameScenes[] desiredScenes = new GameScenes[4] { GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPH };
                psm = game.AddProtoScenarioModule(typeof(TestFlightManagerScenario), desiredScenes);
            }
        }
    }

    public class TestFlightManagerScenario : ScenarioModule
    {
        [KSPField(isPersistant = true)]
        public string aSavedValue = "the default value";

        public override void OnAwake()
        {
            Debug.Log("TestFlightManagerScanario: OnAwake()");
            base.OnAwake();
        }
        public void Update()
        {
        }
        public override void OnSave(ConfigNode node)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("TestFlight: Saving in FLIGHT scene");
                foreach (Vessel vessel in FlightGlobals.Vessels)
                {
                    foreach (Part part in vessel.parts)
                    {
                    }
                }
            }
            Debug.Log("TestFlight: Scenario Saved");
        }

    }
}

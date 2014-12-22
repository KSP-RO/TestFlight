using System;
using System.Collections.Generic;


using UnityEngine;
using KSPPluginFramework;

using TestFlightAPI;

namespace TestFlightCore
{
    public class PartFlightData : IConfigNode
    {
        private List<TestFlightData> flightData = null;
        private string partName = "";

        public void AddFlightData(string name, TestFlightData data)
        {
            if (flightData == null)
            {
                flightData = new List<TestFlightData>();
                partName = name;
                // add new entry for this scope
                TestFlightData newData = new TestFlightData();
                newData.scope = data.scope;
                newData.flightData = data.flightData;
                newData.flightTime = 0;
                flightData.Add(newData);
            }
            else
            {
                int dataIndex = flightData.FindIndex(s => s.scope == data.scope);
                if (dataIndex >= 0)
                {
                    TestFlightData currentData = flightData[dataIndex];
                    // We only update the data if its higher than what we already have
                    if (data.flightData > currentData.flightData)
                    {
                        currentData.flightData = data.flightData;
                        flightData[dataIndex] = currentData;
                    }
                    // We don't care about flightTime, so set it to 0
                    currentData.flightTime = 0;
                }
                else
                {
                    // add new entry for this scope
                    TestFlightData newData = new TestFlightData();
                    newData.scope = data.scope;
                    newData.flightData = data.flightData;
                    newData.flightTime = 0;
                    flightData.Add(newData);
                }
            }
        }

        public List<TestFlightData> GetFlightData()
        {
            return flightData;
        }

        public string GetPartName()
        {
            return partName;
        }

        public override string ToString()
        {
            string baseString = partName + ":";
            foreach (TestFlightData data in flightData)
            {
                string dataString = String.Format("{0},{1},0", data.scope, data.flightData);
                baseString = baseString + dataString + " ";
            }

            return baseString;
        }

        public static PartFlightData FromString(string str)
        {
            // String format is
            // partName:scope,data,0 scope,data scope,data,0 scope,data,0 
            PartFlightData newData = null;
            if (str.IndexOf(':') > -1)
            {
                newData = new PartFlightData();
                string[] baseString = str.Split(new char[1]{ ':' });
                newData.partName = baseString[0];
                string[] dataStrings = baseString[1].Split(new char[1]{ ' ' });
                foreach (string dataString in dataStrings)
                {
                    if (newData.flightData == null)
                        newData.flightData = new List<TestFlightData>();

                    if (dataString.Trim().Length > 0)
                    {
                        string[] dataMembers = dataString.Split(new char[1]{ ',' });
                        if (dataMembers.Length == 3)
                        {
                            TestFlightData tfData = new TestFlightData();
                            tfData.scope = dataMembers[0];;
                            tfData.flightData = float.Parse(dataMembers[1]);
                            tfData.flightTime = 0;
                            newData.flightData.Add(tfData);
                        }
                    }
                }
            }
            return newData;
        }

        public void Load(ConfigNode node)
        {
            partName = node.GetValue("partName");
            Debug.Log("Loading FlightData for " + partName);
            if (node.HasNode("FLIGHTDATA"))
            {
                flightData = new List<TestFlightData>();
                foreach (ConfigNode dataNode in node.GetNodes("FLIGHTDATA"))
                {
                    TestFlightData newData = new TestFlightData();
                    newData.scope = dataNode.GetValue("scope");
                    if (dataNode.HasValue("flightData"))
                        newData.flightData = float.Parse(dataNode.GetValue("flightData"));
                    Debug.Log("SCOPE: " + newData.scope);
                    Debug.Log("DATA: " + newData.flightData);
                    flightData.Add(newData);
                }
            }
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("partName", partName);
            Debug.Log("Saving FlightData for " + partName);
            foreach (TestFlightData data in flightData)
            {
                ConfigNode dataNode = node.AddNode("FLIGHTDATA");
                Debug.Log("SCOPE: " + data.scope);
                dataNode.AddValue("scope", data.scope);
                Debug.Log("DATA: " + data.flightData);
                dataNode.AddValue("flightData", data.flightData);
            }
        }


    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
	public class TestFlightManager : MonoBehaviour
	{
		public void Start()
		{
			var game = HighLogic.CurrentGame;
			ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(TestFlightManagerScenario).Name);
			if (psm == null)
			{
                Debug.Log("Creating new TestFlightManagerScenario");
				GameScenes[] desiredScenes = new GameScenes[4] { GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPH };
				psm = game.AddProtoScenarioModule(typeof(TestFlightManagerScenario), desiredScenes);
			}
		}
	}

	public class TestFlightManagerScenario : ScenarioModule
	{
        public List<PartFlightData> partsFlightData;
        public List<String> partsPackedStrings;
        [KSPField(isPersistant = true)]
        public string test;
        private bool havePartsBeenInitialized = false;

		public override void OnAwake()
		{
			Debug.Log("TestFlightManagerScenario: OnAwake()");
            if (partsFlightData == null)
            {
                Debug.Log("Parts data is null");
                partsFlightData = new List<PartFlightData>();
                if (partsPackedStrings != null)
                {
                    Debug.Log("Found string data");
                    foreach (string packedString in partsPackedStrings)
                    {
                        Debug.Log("Adding Data");
                        Debug.Log(packedString);
                        PartFlightData data = PartFlightData.FromString(packedString);
                        partsFlightData.Add(data);
                    }
                }
            }
            if (partsPackedStrings == null)
            {
                Debug.Log("Strings were null");
                partsPackedStrings = new List<string>();
            }
            Debug.Log(test);
			base.OnAwake();
		}

        private PartFlightData GetFlightDataForPartName(string partName)
        {
            foreach (PartFlightData data in partsFlightData)
            {
                if (data.GetPartName() == partName)
                    return data;
            }
            return null;
        }

        private void InitializeParts()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            foreach (Part part in vessel.parts)
            {
                foreach (PartModule pm in part.Modules)
                {
                    IFlightDataRecorder fdr = pm as IFlightDataRecorder;
                    if (fdr != null)
                    {
                        PartFlightData partData = GetFlightDataForPartName(pm.part.partName);
                        if (partData != null)
                        {
                            Debug.Log("Init flight data");
                            fdr.InitializeFlightData(partData.GetFlightData());
                        }
                        break;
                    }
                }

            }
        }

		public void Update()
		{
            if (HighLogic.LoadedSceneIsFlight)
            {
                // PRELAUNCH
                // FLIGHT
//                Debug.Log(FlightGlobals.ActiveVessel.situation);
                if (havePartsBeenInitialized == false)
                {
                    InitializeParts();
                    havePartsBeenInitialized = true;
//                    PerformPreflightTests();
                }
                else
                {
                    foreach (Vessel vessel in FlightGlobals.Vessels)
                    {
                        foreach (Part part in vessel.parts)
                        {
                            foreach (PartModule pm in part.Modules)
                            {
                                IFlightDataRecorder fdr = pm as IFlightDataRecorder;
                                if (fdr != null)
                                {
                                    TestFlightData currentFlightData = fdr.GetCurrentFlightData();
                                    PartFlightData data = GetFlightDataForPartName(part.name);
                                    if (data != null)
                                    {
                                        data.AddFlightData(part.name, currentFlightData);
                                    }
                                    else
                                    {
                                        data = new PartFlightData();
                                        data.AddFlightData(part.name, currentFlightData);
                                        partsFlightData.Add(data);
                                    }
                                    break;
                                }
                            }

                        }
                    }
                }
            }
            else
                havePartsBeenInitialized = false;
		}

        public override void OnLoad(ConfigNode node)
        {
            test = "one";
            if (node.HasNode("FLIGHTDATA_PART"))
            {
                if (partsFlightData == null)
                    partsFlightData = new List<PartFlightData>();

                foreach (ConfigNode partNode in node.GetNodes("FLIGHTDATA_PART"))
                {
                    Debug.Log("Loading Flight Data");
                    PartFlightData partData = new PartFlightData();
                    partData.Load(partNode);
                    partsFlightData.Add(partData);
                    partsPackedStrings.Add(partData.ToString());
                    Debug.Log(partData.ToString());
                }
            }
        }

		public override void OnSave(ConfigNode node)
		{
            if (HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("TestFlight: Saving in FLIGHT scene");
                foreach (PartFlightData partData in partsFlightData)
                {
                    ConfigNode partNode = node.AddNode("FLIGHTDATA_PART");
                    partData.Save(partNode);
                }
            }
			Debug.Log("TestFlight: Scenario Saved");
		}

	}
}
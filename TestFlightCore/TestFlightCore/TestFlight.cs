using System;
using System.Collections.Generic;
using System.Linq;


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
//            Debug.Log("PartFlightData: AddFlightData()");
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
//            Debug.Log("PartFlightData: GetFlightData()");
            return flightData;
        }

        public string GetPartName()
        {
//            Debug.Log("PartFlightData: GetPartName()");
            return partName;
        }

        public override string ToString()
        {
//            Debug.Log("PartFlightData: ToString()");
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
//            Debug.Log("PartFlightData: FromString()");
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
            Debug.Log("PartFlightData: Load()");
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
            Debug.Log("PartFlightData: Save()");
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
        public Dictionary<Guid, double> knownVessels;

        [KSPField(isPersistant = true)]
        public float pollingInterval = 5.0f;
        [KSPField(isPersistant = true)]
        public bool processInactiveVessels = true;

        private bool havePartsBeenInitialized = false;

        double currentUTC = 0.0;
        double lastPolledUTC = 0.0;


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
			base.OnAwake();
		}

        private PartFlightData GetFlightDataForPartName(string partName)
        {
//            Debug.Log("TestFlightManagerScenario: GetFlightDataForPartName(" + partName + ")");
            foreach (PartFlightData data in partsFlightData)
            {
                if (data.GetPartName() == partName)
                    return data;
            }
            return null;
        }

        private void InitializeParts()
        {
            Debug.Log("TestFlightManagerScenario: InitializeParts()");
            Vessel vessel = FlightGlobals.ActiveVessel;
            foreach (Part part in vessel.parts)
            {
                foreach (PartModule pm in part.Modules)
                {
                    IFlightDataRecorder fdr = pm as IFlightDataRecorder;
                    if (fdr != null)
                    {
                        Debug.Log("TestFlightManagerScenario: Found FlightDataRecorder");
                        Debug.Log("TestFlightManagerScenario: Looking for flightdata for partname: " + pm.part.name);
                        PartFlightData partData = GetFlightDataForPartName(pm.part.name);
                        if (partData != null)
                        {
                            Debug.Log("Init flight data");
                            fdr.InitializeFlightData(partData.GetFlightData());
                        }
                        else
                        {
                            Debug.Log("Unable to find any flightdata");
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
//                    Debug.Log("TestFlightManagerScenario: Initializing parts");
                    InitializeParts();
                    havePartsBeenInitialized = true;
//                    PerformPreflightTests();
                }
                else
                {
                    currentUTC = Planetarium.GetUniversalTime();
//                    Debug.Log("TestFlightManagerScenario: MET " + currentUTC + ", Last Poll " + lastPolledUTC + "(" + pollingInterval + ")");
                    if (currentUTC > (lastPolledUTC + pollingInterval))
                    {
//                        Debug.Log("TestFlightManagerScenario: Scanning all vessel parts for FlightData");
                        lastPolledUTC = currentUTC;

                        // build a list of vessels to process based on setting
                        if (knownVessels == null)
                            knownVessels = new Dictionary<Guid, double>();

                        // iterate through our cached vessels and delete ones that are no longer valid
                        List<Guid> vesselsToDelete = new List<Guid>();
                        foreach(var entry in knownVessels)
                        {
                            Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == entry.Key);
                            if (vessel == null)
                                vesselsToDelete.Add(entry.Key);
                            else
                            {
                                if (vessel.vesselType == VesselType.Debris)
                                    vesselsToDelete.Add(entry.Key);
                            }
                        }
                        foreach (Guid id in vesselsToDelete)
                        {
                            knownVessels.Remove(id);
                        }

                        // Build our cached list of vessels.  The reason we do this is so that we can store an internal "missionStartTime" for each vessel because the game
                        // doesn't consider a vessel launched, and does not start the mission clock, until the player activates the first stage.  This is fine except it
                        // makes things like engine test stands impossible, so we instead cache the vessel the first time we see it and use that time as the missionStartTime

                        if (!processInactiveVessels)
                        {
                            Debug.Log("TestFlightManagerScenario: Polling active vessel only");
                            if (!knownVessels.ContainsKey(FlightGlobals.ActiveVessel.id))
                                knownVessels.Add(FlightGlobals.ActiveVessel.id, Planetarium.GetUniversalTime());
                        }
                        else
                        {
                            Debug.Log("TestFlightManagerScenario: Polling all vessels");
                            foreach (Vessel vessel in FlightGlobals.Vessels)
                            {
                                if (vessel.vesselType == VesselType.Lander || vessel.vesselType == VesselType.Probe || vessel.vesselType == VesselType.Rover || vessel.vesselType == VesselType.Ship || vessel.vesselType == VesselType.Station)
                                {
                                    if ( !knownVessels.ContainsKey(vessel.id) )
                                    {
                                        knownVessels.Add(vessel.id, Planetarium.GetUniversalTime());
                                    }
                                }
                            }
                        }
                        // process those vessels
                        Debug.Log("Processing " + knownVessels.Count + " vessels");
                        foreach (var entry in knownVessels)
                        {
                            Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == entry.Key);
                            Debug.Log("Processing vessel " + vessel.GetName() + ", launched " + vessel.launchTime);
                            foreach (Part part in vessel.parts)
                            {
                                foreach (PartModule pm in part.Modules)
                                {
//                                    Debug.Log("    Checking part module " + pm.moduleName);
                                    ITestFlightCore core = pm as ITestFlightCore;
                                    if (core != null)
                                    {
                                        // Tell the core to do a flight update
                                        Debug.Log("TestFlightManagerScenario: Updating TestFlightCore");
                                        core.DoFlightUpdate(entry.Value);
                                        // Then grab its flight data
                                        Debug.Log("TestFlightManagerScenario: Getting current flight data");
                                        TestFlightData currentFlightData = core.GetCurrentFlightData();
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
                                            partsPackedStrings.Add(data.ToString());
                                        }
                                        break;
                                    }
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
            Debug.Log("TestFlightManagerScenario: OnLoad()");
            Debug.Log(node);
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
            Debug.Log("TestFlightManagerScenario: OnSave()");
            Debug.Log(node);
            if (HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("TestFlightManagerScenario: Saving in FLIGHT scene");
                foreach (PartFlightData partData in partsFlightData)
                {
                    ConfigNode partNode = node.AddNode("FLIGHTDATA_PART");
                    partData.Save(partNode);
                }
            }
            else
            {
                Debug.Log("TestFlightManagerScenario: Saving in NON-FLIGHT scene");
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
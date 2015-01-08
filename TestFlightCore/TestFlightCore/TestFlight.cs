using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlightCore
{
    internal struct PartStatus
    {
        internal string partName;
        internal uint partID;
        internal int partStatus;
        internal int flightTime;
        internal double flightData;
        internal double reliability;
        internal double momentaryReliability;
        internal ITestFlightCore flightCore;
        internal ITestFlightFailure activeFailure;
        internal bool highlightPart;
        internal string repairRequirements;
    }

    internal struct MasterStatusItem
    {
        internal Guid vesselID;
        internal string vesselName;
        internal List<PartStatus> allPartsStatus;
    }

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
            partName = node.GetValue("partName");
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

    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
	public class TestFlightManager : MonoBehaviour
	{
        public TestFlightManagerScenario tsm;

        public void Start()
        {
            var game = HighLogic.CurrentGame;
            ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(TestFlightManagerScenario).Name);
            if (psm == null)
            {
                GameScenes[] desiredScenes = new GameScenes[4] { GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER };
                psm = game.AddProtoScenarioModule(typeof(TestFlightManagerScenario), desiredScenes);
            }
            psm.Load(ScenarioRunner.fetch);
            tsm = game.scenarios.Select(s => s.moduleRef).OfType<TestFlightManagerScenario>().SingleOrDefault();
        }
    }


	public class TestFlightManagerScenario : ScenarioModule
	{
        public List<PartFlightData> partsFlightData;
        public List<String> partsPackedStrings;
        public Dictionary<Guid, double> knownVessels;

        internal Settings settings = null;
        public double pollingInterval = 5.0f;
        public bool processInactiveVessels = true;

//        private bool havePartsBeenInitialized = false;

        private Dictionary<Guid, MasterStatusItem> masterStatus = null;

        double currentUTC = 0.0;
        double lastDataPoll = 0.0;
        double lastFailurePoll = 0.0;
        double lastMasterStatusUpdate = 0.0;

        public override void OnAwake()
		{
            if (partsFlightData == null)
            {
                partsFlightData = new List<PartFlightData>();
                if (partsPackedStrings != null)
                {
                    foreach (string packedString in partsPackedStrings)
                    {
                        Debug.Log(packedString);
                        PartFlightData data = PartFlightData.FromString(packedString);
                        partsFlightData.Add(data);
                    }
                }
            }
            if (partsPackedStrings == null)
            {
                partsPackedStrings = new List<string>();
            }
            if (settings == null)
            {
                settings = new Settings("../settings.cfg");
            }
            string assemblyPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = System.IO.Path.Combine(assemblyPath, "../settings.cfg").Replace("\\","/");
            Debug.Log("Settings stored in " + filePath);
            if (!System.IO.File.Exists(filePath))
            {
                settings.flightDataEngineerMultiplier = 1.0;
                settings.flightDataMultiplier = 1.0;
                settings.globalReliabilityModifier = 1.0;
                settings.minTimeBetweenDataPoll = 0.5;
                settings.minTimeBetweenFailurePoll = 60;
                settings.processAllVessels = false;
                settings.masterStatusUpdateFrequency = 10;
                settings.displaySettingsWindow = true;

                settings.showFailedPartsOnlyInMSD = false;
                settings.showFlightDataInMSD = true;
                settings.showMomentaryReliabilityInMSD = false;
                settings.showRestingReliabilityInMSD = true;
                settings.showStatusTextInMSD = true;
                settings.shortenPartNameInMSD = false;
                settings.settingsPage = 0;
                settings.mainWindowLocked = true;
                settings.mainWindowPosition = new Rect(0, 0, 0, 0);
                settings.currentMSDSize = 1;

                settings.Save();
            }
            settings.Load();
			base.OnAwake();
		}

        internal Dictionary<Guid, MasterStatusItem> GetMasterStatus()
        {
            return masterStatus;
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

        private void InitializeParts(Vessel vessel)
        {
            Debug.Log("TestFlightManagerScenario: Initializing parts for vessel " + vessel.GetName());
            foreach (Part part in vessel.parts)
            {
                foreach (PartModule pm in part.Modules)
                {
                    ITestFlightCore core = pm as ITestFlightCore;
                    if (core != null)
                    {
                        PartFlightData partData = GetFlightDataForPartName(pm.part.name);
                        if (partData == null)
                        {
                            partData = new PartFlightData();
                        }

                        if (partData != null)
                        {
                            core.InitializeFlightData(partData.GetFlightData(), settings.globalReliabilityModifier);
                        }
                    }
                }
            }
        }

        // This method simply scans through the Master Status list every now and then and removes vessels and parts that no longer exist
        public void VerifyMasterStatus()
        {
            // iterate through our cached vessels and delete ones that are no longer valid
            List<Guid> vesselsToDelete = new List<Guid>();
            foreach(var entry in masterStatus)
            {
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == entry.Key);
                if (vessel == null)
                {
                    Debug.Log("TestFlightManagerScenario: Vessel no longer exists. Marking it for deletion.");
                    vesselsToDelete.Add(entry.Key);
                }
                else
                {
                    if (vessel.vesselType == VesselType.Debris)
                    {
                        Debug.Log("TestFlightManagerScenario: Vessel appears to be debris now. Marking it for deletion.");
                        vesselsToDelete.Add(entry.Key);
                    }
                }
            }
            if (vesselsToDelete.Count > 0)
                Debug.Log("TestFlightManagerScenario: Removing " + vesselsToDelete.Count() + " vessels from Master Status");
            foreach (Guid id in vesselsToDelete)
            {
                masterStatus.Remove(id);
            }
            // iterate through the remaining vessels and check for parts that no longer exist
            List<PartStatus> partsToDelete = new List<PartStatus>();
            foreach (var entry in masterStatus)
            {
                partsToDelete.Clear();
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == entry.Key);
                foreach (PartStatus partStatus in masterStatus[entry.Key].allPartsStatus)
                {
                    Part part = vessel.Parts.Find(p => p.flightID == partStatus.partID);
                    if (part == null)
                    {
                        Debug.Log("TestFlightManagerScenario: Could not find part. " + partStatus.partName + "(" + partStatus.partID + ") Marking it for deletion.");
                        partsToDelete.Add(partStatus);
                    }
                }
                if (partsToDelete.Count > 0)
                    Debug.Log("TestFlightManagerScenario: Deleting " + partsToDelete.Count() + " parts from vessel " + vessel.GetName());
                foreach (PartStatus oldPartStatus in partsToDelete)
                {
                    masterStatus[entry.Key].allPartsStatus.Remove(oldPartStatus);
                }
            }
        }

        public void CacheVessels()
        {
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
            if (vesselsToDelete.Count() > 0)
                Debug.Log("TestFlightManagerScenario: Deleting " + vesselsToDelete.Count() + " vessels from cached vessels");
            foreach (Guid id in vesselsToDelete)
            {
                knownVessels.Remove(id);
            }

            // Build our cached list of vessels.  The reason we do this is so that we can store an internal "missionStartTime" for each vessel because the game
            // doesn't consider a vessel launched, and does not start the mission clock, until the player activates the first stage.  This is fine except it
            // makes things like engine test stands impossible, so we instead cache the vessel the first time we see it and use that time as the missionStartTime

            if (!settings.processAllVessels)
            {
                if (FlightGlobals.ActiveVessel != null && !knownVessels.ContainsKey(FlightGlobals.ActiveVessel.id))
                {
                    Debug.Log("TestFlightManagerScenario: Adding new vessel " + FlightGlobals.ActiveVessel.GetName() + " with launch time " + Planetarium.GetUniversalTime());
                    knownVessels.Add(FlightGlobals.ActiveVessel.id, Planetarium.GetUniversalTime());
                    InitializeParts(FlightGlobals.ActiveVessel);
                }
            }
            else
            {
                foreach (Vessel vessel in FlightGlobals.Vessels)
                {
                    if (vessel.vesselType == VesselType.Lander || vessel.vesselType == VesselType.Probe || vessel.vesselType == VesselType.Rover || vessel.vesselType == VesselType.Ship || vessel.vesselType == VesselType.Station)
                    {
                        if ( !knownVessels.ContainsKey(vessel.id) )
                        {
                            Debug.Log("TestFlightManagerScenario: Adding new vessel " + vessel.GetName() + " with launch time " + Planetarium.GetUniversalTime());
                            knownVessels.Add(vessel.id, Planetarium.GetUniversalTime());
                            InitializeParts(vessel);
                        }
                    }
                }
            }
        }

        public void DoFlightUpdate(ITestFlightCore core, double launchTime)
        {
            // Tell the core to do a flight update
            core.DoFlightUpdate(launchTime, settings.flightDataMultiplier, settings.flightDataEngineerMultiplier, settings.globalReliabilityModifier);
        }

        public TestFlightData DoDataUpdate(ITestFlightCore core, Part part)
        {
            // Then grab its flight data
            return core.GetCurrentFlightData();
        }

        public void DoFailureUpdate(ITestFlightCore core, double launchTime)
        {
            core.DoFailureCheck(launchTime, settings.globalReliabilityModifier);
        }

		public void Update()
		{
            if (masterStatus == null)
                masterStatus = new Dictionary<Guid, MasterStatusItem>();

            currentUTC = Planetarium.GetUniversalTime();
            // ensure out vessel list is up to date
            CacheVessels();
            if (currentUTC >= lastMasterStatusUpdate + settings.masterStatusUpdateFrequency)
            {
                lastMasterStatusUpdate = currentUTC;
                VerifyMasterStatus();
            }
            // process vessels
            foreach (var entry in knownVessels)
            {
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == entry.Key);
                if (vessel.loaded)
                {
                    foreach(Part part in vessel.parts)
                    {
                        foreach (PartModule pm in part.Modules)
                        {
                            ITestFlightCore core = pm as ITestFlightCore;
                            if (core != null)
                            {
                                // Poll for flight data and part status
                                if (currentUTC >= lastDataPoll + settings.minTimeBetweenDataPoll)
                                {
                                    DoFlightUpdate(core, entry.Value);
                                    TestFlightData currentFlightData = DoDataUpdate(core, part);

                                    PartStatus partStatus = new PartStatus();
                                    partStatus.flightCore = core;
                                    partStatus.partName = part.partInfo.title;
                                    partStatus.partID = part.flightID;
                                    partStatus.flightData = currentFlightData.flightData;
                                    partStatus.flightTime = currentFlightData.flightTime;
                                    partStatus.partStatus = core.GetPartStatus();
                                    partStatus.reliability = core.GetCurrentReliability(settings.globalReliabilityModifier);
                                    partStatus.repairRequirements = core.GetRequirementsTooltip();
                                    if (core.GetPartStatus() > 0)
                                    {
                                        partStatus.activeFailure = core.GetFailureModule();
                                    }
                                    else
                                    {
                                        partStatus.activeFailure = null;
                                    }

                                    // Update or Add part status in Master Status
                                    if (masterStatus.ContainsKey(vessel.id))
                                    {
                                        // Vessel is already in the Master Status, so check if part is in there as well
                                        int numItems = masterStatus[vessel.id].allPartsStatus.Count(p => p.partID == part.flightID);
                                        int existingPartIndex;
                                        if (numItems == 1)
                                        {
                                            existingPartIndex = masterStatus[vessel.id].allPartsStatus.FindIndex(p => p.partID == part.flightID);
                                            masterStatus[vessel.id].allPartsStatus[existingPartIndex] = partStatus;
                                        }
                                        else if (numItems == 0)
                                        {
                                            masterStatus[vessel.id].allPartsStatus.Add(partStatus);
                                        }
                                        else
                                        {
                                            existingPartIndex = masterStatus[vessel.id].allPartsStatus.FindIndex(p => p.partID == part.flightID);
                                            masterStatus[vessel.id].allPartsStatus[existingPartIndex] = partStatus;
                                            Debug.Log("[ERROR] TestFlightManagerScenario: Found " + numItems + " matching parts in Master Status Display!");
                                        }
                                    }
                                    else
                                    {
                                        // Vessel is not in the Master Status so create a new entry for it and add this part
                                        MasterStatusItem masterStatusItem = new MasterStatusItem();
                                        masterStatusItem.vesselID = vessel.id;
                                        masterStatusItem.vesselName = vessel.GetName();
                                        masterStatusItem.allPartsStatus = new List<PartStatus>();
                                        masterStatusItem.allPartsStatus.Add(partStatus);
                                        masterStatus.Add(vessel.id, masterStatusItem);
                                    }

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
                                }
                                // Poll for failures
                                if (currentUTC >= lastFailurePoll + settings.minTimeBetweenFailurePoll)
                                {
                                    DoFailureUpdate(core, entry.Value);
                                }
                            }
                        }
                    }
                }
                if (currentUTC >= lastDataPoll + settings.minTimeBetweenDataPoll)
                {
                    lastDataPoll = currentUTC;
                }
                if (currentUTC >= lastFailurePoll + settings.minTimeBetweenFailurePoll)
                {
                    lastFailurePoll = currentUTC;
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasNode("FLIGHTDATA_PART"))
            {
                if (partsFlightData == null)
                    partsFlightData = new List<PartFlightData>();

                foreach (ConfigNode partNode in node.GetNodes("FLIGHTDATA_PART"))
                {
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
                foreach (PartFlightData partData in partsFlightData)
                {
                    ConfigNode partNode = node.AddNode("FLIGHTDATA_PART");
                    partData.Save(partNode);
                }
            }
            else
            {
                foreach (PartFlightData partData in partsFlightData)
                {
                    ConfigNode partNode = node.AddNode("FLIGHTDATA_PART");
                    partData.Save(partNode);
                }
            }
		}

	}
}
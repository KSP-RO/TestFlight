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
        internal float baseFailureRate;
        internal float momentaryFailureRate;
        internal ITestFlightCore flightCore;
        internal ITestFlightFailure activeFailure;
        internal bool highlightPart;
        internal string repairRequirements;
        internal bool acknowledged;
        internal String mtbfString;
        internal float timeToRepair;
        internal float lastSeen;
        internal float flightData;
    }

    internal struct MasterStatusItem
    {
        internal Guid vesselID;
        internal string vesselName;
        internal List<PartStatus> allPartsStatus;
    }
    public struct TestFlightData
    {
        // Scope is a combination of the current SOI and the Situation, always lowercase.
        // EG "kerbin_atmosphere" or "mun_space"
        // The one exception is "deep-space" which applies regardless of the SOI if you are deep enough into space
        public string scope;
        // The total accumulated flight data for the part
        public double flightData;
        // The specific flight time, in seconds, of this part instance
        public double flightTime;
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
            if (node.HasNode("FLIGHTDATA"))
            {
                flightData = new List<TestFlightData>();
                foreach (ConfigNode dataNode in node.GetNodes("FLIGHTDATA"))
                {
                    TestFlightData newData = new TestFlightData();
                    newData.scope = dataNode.GetValue("scope");
                    if (dataNode.HasValue("flightData"))
                        newData.flightData = float.Parse(dataNode.GetValue("flightData"));
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
                dataNode.AddValue("scope", data.scope);
                dataNode.AddValue("flightData", data.flightData);
            }
        }


    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TestFlightManager : MonoBehaviourExtended
    {
        internal TestFlightManagerScenario tfScenario = null;
        public static TestFlightManager Instance = null;
        internal bool isReady = false;

        public Dictionary<Guid, double> knownVessels;

        public float pollingInterval = 5.0f;
        public float partDecayTime = 15f;
        public bool processInactiveVessels = true;

        private Dictionary<Guid, MasterStatusItem> masterStatus = null;

        float currentUTC = 0.0f;
        float lastDataPoll = 0.0f;
        float lastFailurePoll = 0.0f;
        float lastMasterStatusUpdate = 0.0f;

        internal void Log(string message)
        {
            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = "TestFlightManager: " + message;
            TestFlightUtil.Log(message, debug);
        }

        internal override void Start()
        {
            base.Start();
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

        public void Startup()
        {
            isReady = true;
            Instance = this;
        }

        internal Dictionary<Guid, MasterStatusItem> GetMasterStatus()
        {
            return masterStatus;
        }

        private void InitializeParts(Vessel vessel)
        {
            Log("TestFlightManager: Initializing parts for vessel " + vessel.GetName());

            // Launch time is equal to current UT unless we have already cached this vessel's launch time
            double launchTime = Planetarium.GetUniversalTime();
            if (knownVessels.ContainsKey(vessel.id))
            {
                launchTime = knownVessels[vessel.id];
            }
            foreach (Part part in vessel.parts)
            {
                ITestFlightCore core = TestFlightUtil.GetCore(part);
                if (core != null)
                {
                    Log("TestFlightManager: Found core.  Getting part data");
                    TestFlightPartData partData = tfScenario.GetPartDataForPart(TestFlightUtil.GetFullPartName(part));
                    if (partData != null)
                    {
                        core.InitializeFlightData(float.Parse(partData.GetValue("flightData")));
                    }
                    else
                        core.InitializeFlightData(0f);
                }
            }
        }

        // This method simply scans through the Master Status list every now and then and removes vessels and parts that no longer exist
        public void VerifyMasterStatus()
        {
            if (!isReady)
                return;
            // iterate through our cached vessels and delete ones that are no longer valid
            List<Guid> vesselsToDelete = new List<Guid>();
            foreach(var entry in masterStatus)
            {
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == entry.Key);
                if (vessel == null)
                {
                    Log("TestFlightManager: Vessel no longer exists. Marking it for deletion.");
                    vesselsToDelete.Add(entry.Key);
                }
                else
                {
                    if (vessel.vesselType == VesselType.Debris)
                    {
                        Log("TestFlightManager: Vessel appears to be debris now. Marking it for deletion.");
                        vesselsToDelete.Add(entry.Key);
                    }
                }
            }
            if (vesselsToDelete.Count > 0)
                Log("TestFlightManager: Removing " + vesselsToDelete.Count() + " vessels from Master Status");
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
                        Log("TestFlightManager: Could not find part. " + partStatus.partName + "(" + partStatus.partID + ") Marking it for cleanup.");
                        partsToDelete.Add(partStatus);
                    }
                }
                if (partsToDelete.Count > 0)
                    Log("TestFlightManager: Deleting " + partsToDelete.Count() + " parts from vessel " + vessel.GetName());
                foreach (PartStatus oldPartStatus in partsToDelete)
                {
                    if (oldPartStatus.lastSeen < Planetarium.GetUniversalTime() - partDecayTime)
                        masterStatus[entry.Key].allPartsStatus.Remove(oldPartStatus);
                }
            }
        }

        public void CacheVessels()
        {
            if (!isReady)
                return;
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
                Log("TestFlightManager: Deleting " + vesselsToDelete.Count() + " vessels from cached vessels");
            foreach (Guid id in vesselsToDelete)
            {
                knownVessels.Remove(id);
            }

            // Build our cached list of vessels.  The reason we do this is so that we can store an internal "missionStartTime" for each vessel because the game
            // doesn't consider a vessel launched, and does not start the mission clock, until the player activates the first stage.  This is fine except it
            // makes things like engine test stands impossible, so we instead cache the vessel the first time we see it and use that time as the missionStartTime

            if (!tfScenario.userSettings.processAllVessels)
            {
                if (FlightGlobals.ActiveVessel != null && !knownVessels.ContainsKey(FlightGlobals.ActiveVessel.id))
                {
                    Log("TestFlightManager: Adding new vessel " + FlightGlobals.ActiveVessel.GetName() + " with launch time " + Planetarium.GetUniversalTime());
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
                            Log("TestFlightManager: Adding new vessel " + vessel.GetName() + " with launch time " + Planetarium.GetUniversalTime());
                            knownVessels.Add(vessel.id, Planetarium.GetUniversalTime());
                            InitializeParts(vessel);
                        }
                    }
                }
            }
        }

        internal override void Update()
        {
            if (!isReady)
                return;

            if (masterStatus == null)
                masterStatus = new Dictionary<Guid, MasterStatusItem>();

            currentUTC = (float)Planetarium.GetUniversalTime();
            // ensure out vessel list is up to date
            CacheVessels();
            if (currentUTC >= lastMasterStatusUpdate + tfScenario.userSettings.masterStatusUpdateFrequency)
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
                        ITestFlightCore core = TestFlightUtil.GetCore(part);
                        if (core != null)
                        {
                            // Poll for flight data and part status
                            if (currentUTC >= lastDataPoll + tfScenario.userSettings.masterStatusUpdateFrequency)
                            {

                                // Old data structure deprecated v1.3
                                PartStatus partStatus = new PartStatus();
                                partStatus.lastSeen = currentUTC;
                                partStatus.flightCore = core;
                                partStatus.partName = TestFlightUtil.GetPartTitle(part);
                                partStatus.partID = part.flightID;
                                partStatus.partStatus = core.GetPartStatus();
                                partStatus.timeToRepair = core.GetRepairTime();
                                partStatus.flightData = core.GetFlightData();
                                float failureRate = core.GetBaseFailureRate();
                                MomentaryFailureRate momentaryFailureRate = core.GetWorstMomentaryFailureRate();
                                if (momentaryFailureRate.valid && momentaryFailureRate.failureRate > failureRate)
                                    failureRate = momentaryFailureRate.failureRate;
                                partStatus.momentaryFailureRate = failureRate;
                                partStatus.repairRequirements = core.GetRequirementsTooltip();
                                partStatus.acknowledged = core.IsFailureAcknowledged();
                                partStatus.activeFailure = core.GetFailureModule();
                                partStatus.mtbfString = core.FailureRateToMTBFString(failureRate, TestFlightUtil.MTBFUnits.SECONDS, 999);

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
                                        Log("[ERROR] TestFlightManager: Found " + numItems + " matching parts in Master Status Display!");
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
                                string partName = TestFlightUtil.GetFullPartName(part);
                                tfScenario.SetFlightDataForPartName(partName, partStatus.flightData);
                            }
                        }
                    }
                }
                if (currentUTC >= lastDataPoll + tfScenario.userSettings.minTimeBetweenDataPoll)
                {
                    lastDataPoll = currentUTC;
                }
                if (currentUTC >= lastFailurePoll + tfScenario.userSettings.minTimeBetweenFailurePoll)
                {
                    lastFailurePoll = currentUTC;
                }
            }
        }
    
    }
        

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, 
        new GameScenes[] 
        { 
            GameScenes.FLIGHT,
            GameScenes.EDITOR,
            GameScenes.SPACECENTER
        }
    )]
	public class TestFlightManagerScenario : ScenarioModule
	{
        internal UserSettings userSettings = null;
        internal BodySettings bodySettings = null;
        public static TestFlightManagerScenario Instance { get; private set; }
        public System.Random RandomGenerator { get; private set; }
        public bool isReady = false;

        // New noscope
        public Dictionary<string, TestFlightPartData> partData;

        internal void Log(string message)
        {
            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = "TestFlightManagerScenario: " + message;
            TestFlightUtil.Log(message, debug);
        }

        public override void OnAwake()
        {
            Instance = this;
            if (userSettings == null)
                userSettings = new UserSettings("../settings.cfg");
            if (bodySettings == null)
                bodySettings = new BodySettings("../settings_bodies.cfg");

            if (userSettings.FileExists)
                userSettings.Load();
            else
                userSettings.Save();

            // TODO
            // The bodySettings don't currently work anyway, so commenting this out for now
//            if (bodySettings.FileExists)
//                bodySettings.Load();
//            else
//            {
//                bodySettings.bodyAliases.Add("moho", "Moho");
//                bodySettings.bodyAliases.Add("eve", "Eve");
//                bodySettings.bodyAliases.Add("gilly", "Gilly");
//                bodySettings.bodyAliases.Add("kerbin", "Kerbin");
//                bodySettings.bodyAliases.Add("mun", "Mun");
//                bodySettings.bodyAliases.Add("minmus", "Minmus");
//                bodySettings.bodyAliases.Add("duna", "Duna");
//                bodySettings.bodyAliases.Add("ike", "Ike");
//                bodySettings.bodyAliases.Add("dres", "Dres");
//                bodySettings.bodyAliases.Add("jool", "Jool");
//                bodySettings.bodyAliases.Add("laythe", "Laythe");
//                bodySettings.bodyAliases.Add("vall", "Vall");
//                bodySettings.bodyAliases.Add("tylo", "Tylo");
//                bodySettings.bodyAliases.Add("bop", "Bop");
//                bodySettings.bodyAliases.Add("pol", "Pol");
//                bodySettings.bodyAliases.Add("eeloo", "Eeloo");
//                bodySettings.Save();
//            }

            base.OnAwake();
        }

        public void Start()
        {
            Log("Scenario Start");
            RandomGenerator = new System.Random();
            isReady = true;
        }

        public string PartWithMostData()
        {
            return "";
        }
        public string PartWithLeastData()
        {
            return "";
        }
        public string PartWithNoData(string partList)
        {
            return "";
        }

        // Get access to a part's data store for further set/get
        public TestFlightPartData GetPartDataForPart(string partName)
        {
            TestFlightPartData returnValue = null;

            if (partData.ContainsKey(partName))
                returnValue = partData[partName];

            return returnValue;
        }

        // Sets the existing partData for this part, or adds a new one
        public void SetPartDataForPart(string partName, TestFlightPartData newData)
        {
            if (partData.ContainsKey(partName))
                partData[partName] = newData;
            else
                partData.Add(partName, newData);
        }

        // New noscope Format
        // This is a utility method that will return the "flightData" value directly or -1 if not found
        public float GetFlightDataForPartName(string partName)
        {
            if (partData.ContainsKey(partName))
                return float.Parse(partData[partName].GetValue("flightData"));
            else
                return -1;
        }

        // New noscope Format
        // This is a utility method that sets the "flightData" value directly
        public void SetFlightDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].AddValue("flightData", data.ToString());
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.AddValue("flightData", data.ToString());
                partData.Add(partName, newData);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (userSettings != null)
            {
                userSettings.Load();
            }
            if (bodySettings != null)
                bodySettings.Load();
            if (partData == null)
                partData = new Dictionary<String, TestFlightPartData>();
            else
                partData.Clear();
            // TODO: This old method of storing scope specific data is deprecated and needs to be removed in the next major release (Probably 1.4)
            if (node.HasNode("FLIGHTDATA_PART"))
            {
                foreach (ConfigNode partNode in node.GetNodes("FLIGHTDATA_PART"))
                {
                    PartFlightData partFlightData = new PartFlightData();
                    partFlightData.Load(partNode);

                    // migrates old data into new noscope layout
                    TestFlightPartData storedPartData = new TestFlightPartData();
                    storedPartData.PartName = partFlightData.GetPartName();
                    // Add up all the data and time from the old system for each scope, and then save that as the new migrated vales
                    double totalData = 0;
                    double totalTime = 0;
                    List<TestFlightData> allData = partFlightData.GetFlightData();
                    foreach (TestFlightData data in allData)
                    {
                        totalData += data.flightData;
                        totalTime += data.flightTime;
                    }
                    storedPartData.AddValue("flightData", totalData.ToString());
                    storedPartData.AddValue("flightTime", totalTime.ToString());
                    partData.Add(storedPartData.PartName,storedPartData);
                }
            }
            // new noscope
            if (node.HasNode("partData"))
            {
                foreach (ConfigNode partDataNode in node.GetNodes("partData"))
                {
                    TestFlightPartData storedPartData = new TestFlightPartData();
                    storedPartData.Load(partDataNode);
                    partData.Add(storedPartData.PartName,storedPartData);
                }
            }
        }

		public override void OnSave(ConfigNode node)
		{
            base.OnSave(node);
            if (userSettings != null)
            {
                userSettings.Save();
            }
            if (bodySettings != null)
                bodySettings.Save();
            // new noscope format
            foreach (TestFlightPartData storedPartData in partData.Values)
            {
                ConfigNode partNode = node.AddNode("partData");
                storedPartData.Save(partNode);
            }
		}
	}
}
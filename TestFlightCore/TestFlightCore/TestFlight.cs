using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


using UnityEngine;
using UnityEngine.Profiling;
using TestFlightCore.KSPPluginFramework;
using TestFlightAPI;

namespace TestFlightCore
{
    internal struct PartStatus
    {
        internal string partName;
        internal uint partID;
        internal int partStatus;
        internal double baseFailureRate;
        internal double momentaryFailureRate;
        internal string runningTime;
        internal string continuousRunningTime;
        internal ITestFlightCore flightCore;
        internal bool highlightPart;
        internal bool acknowledged;
        internal String mtbfString;
        internal double lastSeen;
        internal float flightData;
        internal List<ITestFlightFailure> failures;
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
                            tfData.scope = dataMembers[0];
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
        Dictionary<Guid, double>.Enumerator knownVesselsEnumerator;
        List<string> cores = null;
        List<Guid> vesselsToDelete = null;

        public float pollingInterval = 5.0f;
        public float partDecayTime = 15f;
        public bool processInactiveVessels = true;

        Dictionary<Guid, MasterStatusItem> masterStatus = null;
        Dictionary<Guid, MasterStatusItem>.Enumerator masterStatusEnumerator;
        List<PartStatus> partsToDelete = null;

        double currentUTC = 0.0f;
        double lastDataPoll = 0.0f;
        double lastFailurePoll = 0.0f;
        double lastMasterStatusUpdate = 0.0f;

        // EventData<Vessel>.OnEvent OnVesselModifiedEvent;


        internal void Log(string message)
        {
            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = "TestFlightManager: " + message;
            TestFlightUtil.Log(message, debug);
        }

        internal override void Awake()
        {
            isReady = false;
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(Instance.gameObject);
            }

            Instance = this;
            base.Awake();
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
            cores = new List<string>();
            vesselsToDelete = new List<Guid>();
            partsToDelete = new List<PartStatus>();

            // register to be notified of changes to any vessel
            GameEvents.onVesselWasModified.Add(VesselWasModified);
            GameEvents.onVesselCreate.Add(VesselCreated);
            GameEvents.onVesselDestroy.Add(VesselDestroyed);
            // Build an initial list of vessels and part data
            // This will then be updated from the above event
            if (masterStatus == null)
                masterStatus = new Dictionary<Guid, MasterStatusItem>();

            currentUTC = Planetarium.GetUniversalTime();
        }

        internal override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            GameEvents.onVesselWasModified.Remove(VesselWasModified);
            GameEvents.onVesselCreate.Remove(VesselCreated);
            GameEvents.onVesselDestroy.Remove(VesselDestroyed);
        }

        void VesselDestroyed(Vessel vessel)
        {
            Log($"Vessel Destroyed {vessel.vesselName}");
            masterStatus.Remove(vessel.id);
        }

        void VesselCreated(Vessel vessel)
        {
            Log($"Vessel Created {vessel.vesselName}");
            if (masterStatus.ContainsKey(vessel.id)) return;

            InitializeParts(vessel);
            AddVesselToMasterStatusDisplay(vessel);
        }

        void VesselWasModified(Vessel vessel)
        {
            Log($"Vessel Modified {vessel.vesselName}");
            masterStatus.Remove(vessel.id);
            AddVesselToMasterStatusDisplay(vessel);
        }

        void AddVesselToMasterStatusDisplay(Vessel vessel)
        {
            MasterStatusItem masterStatusItem = new MasterStatusItem();
            masterStatusItem.vesselID = vessel.id;
            masterStatusItem.vesselName = vessel.GetName();
            masterStatusItem.allPartsStatus = new List<PartStatus>();
            masterStatus.Add(vessel.id, masterStatusItem);

            var parts = vessel.Parts;
            for (var j = 0; j < parts.Count; j++)
            {
                var partCores = parts[j].gameObject.GetComponents<TestFlightCore>();
                foreach (var core in partCores)
                {
                    if (!core.TestFlightEnabled) continue;

                    PartStatus partStatus = new PartStatus();
                    partStatus.lastSeen = currentUTC;
                    partStatus.flightCore = core;
                    partStatus.partName = core.Title;
                    partStatus.partID = vessel.parts[j].flightID;
                    partStatus.partStatus = core.GetPartStatus();
                    // get any failures
                    partStatus.failures = core.GetActiveFailures();
                    partStatus.flightData = core.GetFlightData();
                    double failureRate = core.GetBaseFailureRate();
                    MomentaryFailureRate momentaryFailureRate = core.GetWorstMomentaryFailureRate();
                    if (momentaryFailureRate.valid && momentaryFailureRate.failureRate > failureRate)
                        failureRate = momentaryFailureRate.failureRate;
                    partStatus.momentaryFailureRate = failureRate;
                    partStatus.acknowledged = false;
                    partStatus.mtbfString = core.FailureRateToMTBFString(failureRate, TestFlightUtil.MTBFUnits.SECONDS, 999);
                    partStatus.runningTime = TestFlightUtil.FormatTime(core.GetRunTime(RatingScope.Cumulative), TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false);
                    partStatus.continuousRunningTime = TestFlightUtil.FormatTime(core.GetRunTime(RatingScope.Continuous), TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false);
                    masterStatus[vessel.id].allPartsStatus.Add(partStatus);
                }
            }
        }

        void UpdateVesselInMasterStatusDisplay(Vessel vessel)
        {
            if (!masterStatus.ContainsKey(vessel.id)) return;

            var allPartsStatus = masterStatus[vessel.id].allPartsStatus;

            for (var i = 0; i < allPartsStatus.Count; i++)
            {
                var status = allPartsStatus[i];
                ITestFlightCore core = status.flightCore;

                // Update the part status
                status.partStatus = core.GetPartStatus();
                status.failures = core.GetActiveFailures();
                status.flightData = core.GetFlightData();
                double failureRate = core.GetBaseFailureRate();
                MomentaryFailureRate momentaryFailureRate = core.GetWorstMomentaryFailureRate();
                if (momentaryFailureRate.valid && momentaryFailureRate.failureRate > failureRate)
                    failureRate = momentaryFailureRate.failureRate;
                status.momentaryFailureRate = failureRate;
                status.mtbfString = core.FailureRateToMTBFString(failureRate, TestFlightUtil.MTBFUnits.SECONDS, 999);
                status.runningTime = TestFlightUtil.FormatTime(core.GetRunTime(RatingScope.Cumulative), TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false);
                status.continuousRunningTime = TestFlightUtil.FormatTime(core.GetRunTime(RatingScope.Continuous), TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false);
                allPartsStatus[i] = status;
            }
        }

        internal Dictionary<Guid, MasterStatusItem> GetMasterStatus()
        {
            return masterStatus;
        }

        private void InitializeParts(Vessel vessel)
        {
            foreach (Part part in vessel.parts)
            {
                // Each KSP part can be composed of N virtual parts
                List<string> cores = TestFlightInterface.GetActiveCores(part);
                if (cores == null || cores.Count <= 0)
                    continue;
                foreach (string activeCore in cores)
                {
                    ITestFlightCore core = TestFlightUtil.GetCore(part, activeCore);
                    if (core != null)
                    {
                        if (TestFlightManagerScenario.Instance.SettingsAlwaysMaxData)
                        {
                            core.InitializeFlightData(core.GetMaximumData());
                        }
                        else
                        {
                            var flightData = Mathf.Max(0f,TestFlightManagerScenario.Instance.GetFlightDataForPartName(activeCore));
                            core.InitializeFlightData(flightData);
                        }
                    }
                }
            }
        }

        internal override void Update()
        {
            if (!isReady)
                return;

            if (!tfScenario.SettingsEnabled)
                return;

            if (!masterStatus.ContainsKey(FlightGlobals.ActiveVessel.id))
            {
                InitializeParts(FlightGlobals.ActiveVessel);
                AddVesselToMasterStatusDisplay(FlightGlobals.ActiveVessel);
            }

            UpdateVesselInMasterStatusDisplay(FlightGlobals.ActiveVessel);
        }

    }


    [KSPScenario(ScenarioCreationOptions.AddToAllGames,
        new GameScenes[]
        {
            GameScenes.FLIGHT,
            GameScenes.EDITOR,
            GameScenes.SPACECENTER,
            GameScenes.TRACKSTATION
        }
    )]
    public class TestFlightManagerScenario : ScenarioModule
    {
        internal UserSettings userSettings = null;
        internal BodySettings bodySettings = null;

        public static TestFlightManagerScenario Instance { get; private set; }

        public static System.Random RandomGenerator { get; private set; }

        public bool isReady = false;
        // For storing save specific arbitrary data
        private string rawSaveData = "";
        private Dictionary<string, string> saveData;

        // New noscope
        public Dictionary<string, TestFlightPartData> partData = null;

        public bool SettingsEnabled
        {
            get { return GetBool("settingsenabled", true); }
            set { SetValue("settingsenabled", value); }
        }

        public bool SettingsAlwaysMaxData
        {
            get { return GetBool("settingsalwaysmaxdata", false); }
            set { SetValue("settingsalwaysmaxdata", value); }
        }

        internal void Log(string message)
        {
            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = "[TestFlightManagerScenario] " + message;
            TestFlightUtil.Log(message, debug);
        }

        private void InitDataStore()
        {
            Log("Init data store");
            if (saveData == null)
            {
                Log("Creating new dictionary instance for data store");
                saveData = new Dictionary<string, string>();
            }
            else
                saveData.Clear();
        }

        public string GetValue(string key)
        {
            key = key.ToLowerInvariant();
            if (saveData.ContainsKey(key))
                return saveData[key];
            else
                return "";
        }

        public string GetString(string key)
        {
            return GetString(key, "");
        }

        public string GetString(string key, string defaultValue)
        {
            if (!String.IsNullOrEmpty(GetValue(key)))
                return GetValue(key);
            else
                return defaultValue;
        }

        public double GetDouble(string key)
        {
            return GetDouble(key, 0);
        }

        public double GetDouble(string key, double defaultValue)
        {
            double returnValue = defaultValue;

            string value = GetValue(key);
            if (!double.TryParse(value, out returnValue))
                return defaultValue;

            return returnValue;
        }

        public float GetFloat(string key)
        {
            return GetFloat(key, 0f);
        }

        public float GetFloat(string key, float defaultValue)
        {
            float returnValue = defaultValue;

            string value = GetValue(key);
            if (!float.TryParse(value, out returnValue))
                return defaultValue;

            return returnValue;
        }

        public bool GetBool(string key)
        {
            return GetBool(key, false);
        }

        public bool GetBool(string key, bool defaultValue)
        {
            bool returnValue = defaultValue;
            key = key.ToLowerInvariant();
            if (saveData.ContainsKey(key))
            {
                if (!bool.TryParse(saveData[key], out returnValue))
                    return defaultValue;
            }
            else
                return defaultValue;

            return returnValue;
        }

        public int GetInt(string key)
        {
            return GetInt(key, 0);
        }

        public int GetInt(string key, int defaultValue)
        {
            int returnValue = defaultValue;

            string value = GetValue(key);
            if (!int.TryParse(value, out returnValue))
                return defaultValue;

            return returnValue;
        }

        public void SetValue(string key, float value)
        {
            SetValue(key, value.ToString());
        }

        public void SetValue(string key, int value)
        {
            SetValue(key, value.ToString());
        }

        public void SetValue(string key, bool value)
        {
            SetValue(key, value.ToString());
        }

        public void SetValue(string key, double value)
        {
            SetValue(key, value.ToString());
        }

        public void SetValue(string key, string value)
        {
            key = key.ToLowerInvariant();
            if (saveData.ContainsKey(key))
                saveData[key] = value;
            else
                saveData.Add(key, value);
        }

        public void AddValue(string key, float value)
        {
            key = key.ToLowerInvariant();
            float newValue = value;
            if (saveData.ContainsKey(key))
            {
                float existingValue;
                if (float.TryParse(saveData[key], out existingValue))
                    newValue = existingValue + value;
            }
            SetValue(key, newValue.ToString());
        }

        public void AddValue(string key, int value)
        {
            key = key.ToLowerInvariant();
            int newValue = value;
            if (saveData.ContainsKey(key))
            {
                int existingValue;
                if (int.TryParse(saveData[key], out existingValue))
                    newValue = existingValue + value;
            }
            SetValue(key, newValue.ToString());
        }

        public void ToggleValue(string key, bool defaultValue)
        {
            key = key.ToLowerInvariant();
            bool newValue = defaultValue;
            if (saveData.ContainsKey(key))
            {
                bool existingValue;
                if (bool.TryParse(saveData[key], out existingValue))
                    newValue = !existingValue;
            }
            SetValue(key, newValue.ToString());
        }

        public void AddValue(string key, double value)
        {
            key = key.ToLowerInvariant();
            double newValue = value;
            if (saveData.ContainsKey(key))
            {
                double existingValue;
                if (double.TryParse(saveData[key], out existingValue))
                    newValue = existingValue + value;
            }
            SetValue(key, newValue.ToString());
        }

        private void decodeRawSaveData()
        {
            if (String.IsNullOrEmpty(rawSaveData))
                return;

            string[] propertyGroups = rawSaveData.Split(new char[1]{ ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string propertyGroup in propertyGroups)
            {
                string[] keyValuePair = propertyGroup.Split(new char[1]{ ':' });
                SetValue(keyValuePair[0], keyValuePair[1]);
            }
        }

        private void encodeRawSaveData()
        {
            rawSaveData = "";
            foreach (var entry in saveData)
            {
                rawSaveData += String.Format("{0}:{1},", entry.Key, entry.Value);
            }
        }

        #region Assembly/Class Information
        /// <summary>
        /// Name of the Assembly that is running this MonoBehaviour
        /// </summary>
        internal static String _AssemblyName
        { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        /// <summary>
        /// Full Path of the executing Assembly
        /// </summary>
        internal static String _AssemblyLocation
        { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }

        /// <summary>
        /// Folder containing the executing Assembly
        /// </summary>
        internal static String _AssemblyFolder
        { get { return System.IO.Path.GetDirectoryName(_AssemblyLocation); } }

        #endregion


        public override void OnAwake()
        {
            isReady = false;
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(Instance.gameObject);
            }
            Instance = this;

            // v1.5.4 moved settings to PluginData but to avoid screwing over existing installs we want to migrate existing settings
            string pdSettingsFile = System.IO.Path.Combine(_AssemblyFolder, "PluginData/settings.cfg");
            string settingsFile = System.IO.Path.Combine(_AssemblyFolder, "../settings.cfg");
            string pdDir = System.IO.Path.Combine(_AssemblyFolder, "PluginData");
            if (!System.IO.File.Exists(pdSettingsFile) && System.IO.File.Exists(settingsFile))
            {
                userSettings = new UserSettings("../settings.cfg");
                userSettings.Load();
                System.IO.Directory.CreateDirectory(pdDir);
                userSettings.Save(pdSettingsFile);
                System.IO.File.Delete(settingsFile);
            }
            if (!System.IO.Directory.Exists(pdDir))
            {
                System.IO.Directory.CreateDirectory(pdDir);
            }

            if (userSettings == null)
                userSettings = new UserSettings("PluginData/settings.cfg");

            if (userSettings.FileExists)
                userSettings.Load();
            else
                userSettings.Save();

            InitDataStore();
            base.OnAwake();
        }

        public void Start()
        {
            Log("Scenario Start");
            if (RandomGenerator == null)
            {
                RandomGenerator = new System.Random();
            }

            isReady = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public string PartWithMostData()
        {
            if (partData == null)
                return "";

            float flightData = 0f;
            string returnPart = "";
            foreach (TestFlightPartData part in partData.Values)
            {
                float partFlightData = float.Parse(part.GetValue("flightData"));
                if (partFlightData > flightData)
                {
                    flightData = partFlightData;
                    returnPart = part.PartName;
                }
            }
            return returnPart;
        }

        public string PartWithLeastData()
        {
            if (partData == null)
                return "";

            float flightData = float.MaxValue;
            string returnPart = "";
            foreach (TestFlightPartData part in partData.Values)
            {
                float partFlightData = float.Parse(part.GetValue("flightData"));
                if (partFlightData < flightData)
                {
                    flightData = partFlightData;
                    returnPart = part.PartName;
                }
            }
            return returnPart;
        }

        public string PartWithNoData(string partList)
        {
            string[] parts = partList.Split(new char[1]{ ',' });
            foreach (string partName in parts)
            {
                float partFlightData = GetFlightDataForPartName(partName);
                if (partFlightData < 0f)
                    return partName;
            }
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
            var stack = UnityEngine.StackTraceUtility.ExtractStackTrace();
            Log(stack);
            if (partData.ContainsKey(partName))
            {
                Log($"Get flight data for {partName}: {partData[partName].GetFloat("flightData")}");
                return partData[partName].GetFloat("flightData");
            }
            else
                return -1f;
        }
        // This is a utility method that will return the "transferData" value directly or -1 if not found
        public float GetTransferDataForPartName(string partName)
        {
            if (partData.ContainsKey(partName))
                return partData[partName].GetFloat("transferData");
            else
                return -1f;
        }
        // This is a utility method that will return the "researchData" value directly or -1 if not found
        public float GetResearchDataForPartName(string partName)
        {
            if (partData.ContainsKey(partName))
                return partData[partName].GetFloat("researchData");
            else
                return -1f;
        }

        // New noscope Format
        // This is a utility method that sets the "flightData" value directly
        public void SetFlightDataForPartName(string partName, float data)
        {
            var stack = UnityEngine.StackTraceUtility.ExtractStackTrace();
            Log(stack);
            Log($"Set flight data for {partName} to {data}");
            if (partData.ContainsKey(partName))
                partData[partName].SetValue("flightData", data.ToString());
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.SetValue("flightData", data.ToString());
                partData.Add(partName, newData);
            }
        }
        // This is a utility method that sets the "transferData" value directly
        public void SetTransferDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].SetValue("transferData", data.ToString());
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.SetValue("transferData", data.ToString());
                partData.Add(partName, newData);
            }
        }
        // This is a utility method that sets the "researchData" value directly
        public void SetResearchDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].SetValue("researchData", data.ToString());
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.SetValue("researchData", data.ToString());
                partData.Add(partName, newData);
            }
        }
        // This is a utility method that adds the "flightData" value directly
        public void AddFlightDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].AddValue("flightData", data);
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.SetValue("flightData", data);
                partData.Add(partName, newData);
            }
        }
        // This is a utility method that adds the "transferData" value directly
        public void AddTransferDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].AddValue("transferData", data);
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.SetValue("transferData", data);
                partData.Add(partName, newData);
            }
        }
        // This is a utility method that adds the "researchData" value directly
        public void AddResearchDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].AddValue("researchData", data);
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.SetValue("researchData", data);
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

            InitDataStore();
            if (node.HasValue("saveData"))
                rawSaveData = node.GetValue("saveData");
            else
                rawSaveData = "";
            decodeRawSaveData();

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
                    storedPartData.SetValue("flightData", totalData.ToString());
                    storedPartData.SetValue("flightTime", totalTime.ToString());
                    partData.Add(storedPartData.PartName, storedPartData);
                }
            }
            // new noscope
            if (node.HasNode("partData"))
            {
                foreach (ConfigNode partDataNode in node.GetNodes("partData"))
                {
                    TestFlightPartData storedPartData = new TestFlightPartData();
                    storedPartData.Load(partDataNode);
                    partData.Add(storedPartData.PartName, storedPartData);
                    Log($"Loaded Part Data for {storedPartData.PartName}: {storedPartData.GetFloat("flightData")}");
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

            encodeRawSaveData();
            node.AddValue("saveData", rawSaveData);

            // new noscope format
            foreach (TestFlightPartData storedPartData in partData.Values)
            {
                ConfigNode partNode = node.AddNode("partData");
                storedPartData.Save(partNode);
            }
        }
    }
}

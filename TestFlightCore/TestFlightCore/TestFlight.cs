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
    internal class PartStatus
    {
        internal string partName;
        internal uint partID;
        internal int partStatus;
        internal double baseFailureRate;
        internal double momentaryFailureRate;
        internal ITestFlightCore flightCore;
        internal bool highlightPart;
        internal bool acknowledged;
        internal double lastSeen;
        internal float flightData;
        internal List<ITestFlightFailure> failures = new List<ITestFlightFailure>();

        internal string MTBFString => flightCore != null ? flightCore.FailureRateToMTBFString(momentaryFailureRate, TestFlightUtil.MTBFUnits.SECONDS, 999) : string.Empty;
        internal string RunningTime => flightCore != null ? TestFlightUtil.FormatTime(flightCore.GetRunTime(RatingScope.Cumulative), TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false) : string.Empty;
        internal string ContinuousRunningTime => flightCore != null ? TestFlightUtil.FormatTime(flightCore.GetRunTime(RatingScope.Continuous), TestFlightUtil.TIMEFORMAT.SHORT_IDENTIFIER, false) : string.Empty;
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
                baseString += $"{data.scope},{data.flightData},0 ";
            return baseString;
        }

        public static PartFlightData FromString(string str)
        {
            // String format is
            // partName:scope,data,0 scope,data scope,data,0 scope,data,0
            PartFlightData newData = null;
            if (str.IndexOf(':') > -1)
            {
                var colonSep = new char[1] { ':' };
                var spaceSep = new char[1] { ' ' };
                var commaSep = new char[1] { ',' };
                newData = new PartFlightData();
                string[] baseString = str.Split(colonSep);
                newData.partName = baseString[0];
                string[] dataStrings = baseString[1].Split(spaceSep);
                foreach (string dataString in dataStrings)
                {
                    if (newData.flightData == null)
                        newData.flightData = new List<TestFlightData>();

                    if (dataString.Trim().Length > 0)
                    {
                        string[] dataMembers = dataString.Split(commaSep);
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

        public float pollingInterval = 5.0f;
        public float partDecayTime = 15f;
        public bool processInactiveVessels = true;
        private readonly Dictionary<Guid, MasterStatusItem> masterStatus = new Dictionary<Guid, MasterStatusItem>(32);

        double currentUTC = 0.0f;

        internal void Log(string message)
        {
            TestFlightUtil.Log($"TestFlightManager: {message}", TestFlightManagerScenario.Instance?.userSettings.debugLog ?? false);
        }

        internal override void Awake()
        {
            if (Instance != null && Instance != this)
                DestroyImmediate(Instance.gameObject);

            Instance = this;
            base.Awake();
        }

        internal override void Start()
        {
            base.Start();
            StartCoroutine(ConnectToScenario());
        }

        IEnumerator ConnectToScenario()
        {
            while (TestFlightManagerScenario.Instance == null)
                yield return null;

            tfScenario = TestFlightManagerScenario.Instance;
            while (!tfScenario.isReady)
                yield return null;
            Startup();
        }

        public void Startup()
        {
            isReady = true;

            // register to be notified of changes to any vessel
            GameEvents.onVesselWasModified.Add(VesselWasModified);
            GameEvents.onVesselCreate.Add(VesselCreated);
            GameEvents.onVesselDestroy.Add(VesselDestroyed);
            masterStatus.Clear();
            currentUTC = Planetarium.GetUniversalTime();
        }

        internal override void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
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

            foreach (Part p in vessel.Parts)
            {
                var partCores = p.gameObject.GetComponents<TestFlightCore>();
                foreach (var core in partCores)
                {
                    if (!core.TestFlightEnabled) continue;

                    double failureRate = core.GetBaseFailureRate();
                    MomentaryFailureRate momentaryFailureRate = core.GetWorstMomentaryFailureRate();
                    if (momentaryFailureRate.valid && momentaryFailureRate.failureRate > failureRate)
                        failureRate = momentaryFailureRate.failureRate;

                    PartStatus partStatus = new PartStatus
                    {
                        lastSeen = currentUTC,
                        flightCore = core,
                        partName = core.Title,
                        partID = p.flightID,
                        partStatus = core.GetPartStatus(),
                        failures = core.GetActiveFailures(),
                        flightData = core.GetFlightData(),
                        momentaryFailureRate = failureRate,
                        acknowledged = false,
                    };
                    masterStatus[vessel.id].allPartsStatus.Add(partStatus);
                }
            }
        }

        void UpdateVesselInMasterStatusDisplay(Vessel vessel)
        {
            MasterStatusItem item;
            if (!masterStatus.TryGetValue(vessel.id, out item)) return;

            foreach (var status in item.allPartsStatus)
            {
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
        private readonly Dictionary<string, string> saveData = new Dictionary<string, string>();

        // New noscope
        public Dictionary<string, TestFlightPartData> partData = null;

        [KSPField(isPersistant = true)] public bool settingsEnabled = true;
        [KSPField(isPersistant = true)] public bool settingsAlwaysMaxData = false;
        public bool SettingsEnabled { get { return settingsEnabled; } set { settingsEnabled = value; } }
        public bool SettingsAlwaysMaxData { get { return settingsAlwaysMaxData; } set { settingsAlwaysMaxData = value; } }

        private bool rp1Available = false;
        private bool careerLogging = false;
        private PropertyInfo careerLoggingInstanceProperty;
        private MethodInfo careerLogFailureMethod;

        internal void Log(string message)
        {
            TestFlightUtil.Log($"[TestFlightManagerScenario] {message}", Instance.userSettings.debugLog);
        }

        internal void LogCareerFailure(Vessel vessel, string part, string failureType)
        {
            if (!rp1Available || !careerLogging)
            {
                Log("Unable to log career failure.  RP1 or Career Logging is unavailable");
                return;
            }
            
            var instance = careerLoggingInstanceProperty.GetValue(null);
            Log("Logging career failure");
            careerLogFailureMethod.Invoke(instance, new object[] { vessel, part, failureType });
        }
        
        private void InitRP1Connection()
        {
            Assembly a = AssemblyLoader.loadedAssemblies.FirstOrDefault(la => string.Equals(la.name, "RP-0", StringComparison.OrdinalIgnoreCase))?.assembly;
            if (a == null)
            {
                Log("RP1 Assembly not found");
                rp1Available = false;
                return;
            }
            rp1Available = true;
            Type t = a.GetType("RP0.CareerLog");
            
            careerLogFailureMethod =
                t?.GetMethod("AddFailureEvent", new[] { typeof(Vessel), typeof(string), typeof(string) });
            if (careerLogFailureMethod == null)
            {
                careerLogging = false;
                return;
            }
            careerLoggingInstanceProperty = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (careerLoggingInstanceProperty != null)
            {
                careerLogging = true;
                Log("RP1 Career Logging enabled");
            }
            else
            {
                careerLogging = false;
            }
        }

        private void InitDataStore()
        {
            saveData.Clear();
        }

        public string GetValue(string key)
        {
            string data;
            if (saveData.TryGetValue(key.ToLowerInvariant(), out data))
                return data;
            return "";
        }

        public void SetValue(string key, string value)
        {
            key = key.ToLowerInvariant();
            if (saveData.ContainsKey(key))
                saveData[key] = value;
            else
                saveData.Add(key, value);
        }

        private void DecodeRawSaveData()
        {
            if (string.IsNullOrEmpty(rawSaveData))
                return;

            string[] propertyGroups = rawSaveData.Split(new char[1]{ ',' }, StringSplitOptions.RemoveEmptyEntries);
            var colonSep = new char[1] { ':' };
            foreach (string propertyGroup in propertyGroups)
            {
                string[] keyValuePair = propertyGroup.Split(colonSep);
                SetValue(keyValuePair[0], keyValuePair[1]);
            }
        }

        private void EncodeRawSaveData()
        {
            rawSaveData = "";
            foreach (var entry in saveData)
                rawSaveData += $"{entry.Key}:{entry.Value},";
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
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(Instance.gameObject);
            }
            Instance = this;

            string pdDir = System.IO.Path.Combine(_AssemblyFolder, "PluginData");
            if (!System.IO.Directory.Exists(pdDir))
                System.IO.Directory.CreateDirectory(pdDir);

            if (userSettings == null)
                userSettings = new UserSettings("PluginData/settings.cfg");

            if (userSettings.FileExists)
                userSettings.Load();
            else
                userSettings.Save();

            InitDataStore();
            InitRP1Connection();
            base.OnAwake();

            if (RandomGenerator == null)
                RandomGenerator = new System.Random();
            isReady = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public string PartWithMostData()
        {
            if (partData == null)
                return "";

            float flightData = 0f;
            string returnPart = "";
            foreach (TestFlightPartData part in partData.Values)
            {
                if (part.flightData > flightData)
                {
                    flightData = part.flightData;
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
                if (part.flightData < flightData)
                {
                    flightData = part.flightData;
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
            if (partData.ContainsKey(partName))
            {
                return partData[partName].flightData;
            }
            else
                return -1f;
        }
        // This is a utility method that will return the "transferData" value directly or -1 if not found
        public float GetTransferDataForPartName(string partName)
        {
            if (partData.ContainsKey(partName))
                return partData[partName].transferData;
            else
                return -1f;
        }
        // This is a utility method that will return the "researchData" value directly or -1 if not found
        public float GetResearchDataForPartName(string partName)
        {
            if (partData.ContainsKey(partName))
                return partData[partName].researchData;
            else
                return -1f;
        }

        // New noscope Format
        // This is a utility method that sets the "flightData" value directly
        public void SetFlightDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].flightData = data;
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.flightData = data;
                partData.Add(partName, newData);
            }
        }
        // This is a utility method that sets the "transferData" value directly
        public void SetTransferDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].transferData = data;
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.transferData = data;
                partData.Add(partName, newData);
            }
        }
        // This is a utility method that sets the "researchData" value directly
        public void SetResearchDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].researchData = data;
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.researchData = data;
                partData.Add(partName, newData);
            }
        }
        // This is a utility method that adds the "flightData" value directly
        public void AddFlightDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].flightData += data;
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.flightData = data;
                partData.Add(partName, newData);
            }
        }
        // This is a utility method that adds the "transferData" value directly
        public void AddTransferDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].transferData += data;
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.transferData = data;
                partData.Add(partName, newData);
            }
        }
        // This is a utility method that adds the "researchData" value directly
        public void AddResearchDataForPartName(string partName, float data)
        {
            if (partData.ContainsKey(partName))
                partData[partName].researchData += data;
            else
            {
                TestFlightPartData newData = new TestFlightPartData();
                newData.PartName = partName;
                newData.researchData = data;
                partData.Add(partName, newData);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (userSettings != null)
                userSettings.Load();
            if (bodySettings != null)
                bodySettings.Load();

            InitDataStore();
            if (node.HasValue("saveData"))
                rawSaveData = node.GetValue("saveData");
            else
                rawSaveData = "";
            DecodeRawSaveData();

            if (partData == null)
                partData = new Dictionary<String, TestFlightPartData>();
            else
                partData.Clear();
            // new noscope
            if (node.HasNode("partData"))
            {
                foreach (ConfigNode partDataNode in node.GetNodes("partData"))
                {
                    TestFlightPartData storedPartData = new TestFlightPartData();
                    storedPartData.Load(partDataNode);
                    partData.Add(storedPartData.PartName, storedPartData);
                    Log($"Loaded Part Data for {storedPartData.PartName}: {storedPartData.flightData}");
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            if (userSettings != null)
                userSettings.Save();
            if (bodySettings != null)
                bodySettings.Save();

            EncodeRawSaveData();
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

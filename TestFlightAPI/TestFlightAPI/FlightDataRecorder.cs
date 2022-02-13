using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace TestFlightAPI
{
    public class FlightDataRecorderBase : PartModule, IFlightDataRecorder
    {
        private ITestFlightCore core = null;
        #region KSPFields
        [KSPField(isPersistant = true)]
        public float lastRecordedMet = 0;
        [KSPField]
        public float flightDataMultiplier = 10.0f;
        [KSPField]
        public float flightDataEngineerModifier = 0.25f;
        [KSPField]
        public string configuration = "";
        #endregion

        public List<ConfigNode> configs = new List<ConfigNode>();
        public ConfigNode currentConfig;
        public string configNodeData;
        

        protected void Log(string message)
        {
            TestFlightUtil.Log($"FlightDataRecorder({Configuration}[{Configuration}]): {message}", this.part);
        }


        public bool TestFlightEnabled
        {
            get
            {
                return core != null && core.TestFlightEnabled;
            }
        }
        public string Configuration
        {
            get 
            { 
                if (string.IsNullOrEmpty(configuration))
                    configuration = TestFlightUtil.GetPartName(this.part);

                return configuration; 
            }
            set 
            { 
                configuration = value; 
            }
        }

        public void SetActiveConfig(string alias)
        {
            foreach (var configNode in configs)
            {
                if (!configNode.HasValue("configuration")) continue;

                var nodeConfiguration = configNode.GetValue("configuration");

                if (string.Equals(nodeConfiguration, alias, StringComparison.InvariantCultureIgnoreCase))
                {
                    currentConfig = configNode;
                }
            }

            if (currentConfig == null) return;

            // update current values with those from the current config node
            currentConfig.TryGetValue("flightDataMultiplier", ref flightDataMultiplier);
            currentConfig.TryGetValue("configuration", ref configuration);
            currentConfig.TryGetValue("flightDataEngineerModifier", ref flightDataEngineerModifier);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (node.HasNode("MODULE"))
                node = node.GetNode("MODULE");

            ConfigNode[] cNodes = node.GetNodes("CONFIG");
            if (cNodes != null && cNodes.Length > 0)
            {
                configs.Clear();

                foreach (ConfigNode subNode in cNodes) {
                    var newNode = new ConfigNode("CONFIG");
                    subNode.CopyTo(newNode);
                    configs.Add(newNode);
                }
            }

            configNodeData = node.ToString();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (core == null)
                core = TestFlightUtil.GetCore(this.part, Configuration);
        }

        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;
            Profiler.BeginSample("TestFlight.FlightDataRecorder.OnUpdate");

            base.OnUpdate();

            float currentMet = core.GetOperatingTime();
            if (IsRecordingFlightData())
            {
                float flightData = (currentMet - lastRecordedMet) * flightDataMultiplier;
                float engineerBonus = core.GetEngineerDataBonus(flightDataEngineerModifier);
                flightData *= engineerBonus;
                if (flightData >= 0)
                    core.ModifyFlightData(flightData, true);
                core.ModifyFlightTime(currentMet - lastRecordedMet, true);
            }
            lastRecordedMet = currentMet;
            Profiler.EndSample();
        }

        public virtual bool IsPartOperating()
        {
            if (!TestFlightEnabled)
                return false;

            return true;
        }

        public virtual bool IsRecordingFlightData()
        {
            if (!TestFlightEnabled)
                return false;

            if (!IsPartOperating())
                return false;

            if (this.part.vessel.situation == Vessel.Situations.PRELAUNCH)
                return false;

            if (!isEnabled)
                return false;
				
            return true;
        }

        public override void OnAwake()
        {
            if (!string.IsNullOrEmpty(configNodeData))
            {
                var node = ConfigNode.Parse(configNodeData);
                OnLoad(node);
            }

            base.OnAwake();
        }

        public virtual List<string> GetTestFlightInfo()
        {
            return new List<string>();
        }
    }
}


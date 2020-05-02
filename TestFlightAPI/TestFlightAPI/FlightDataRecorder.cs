using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

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

        public List<ConfigNode> configs;
        public ConfigNode currentConfig;
        public string configNodeData;
        

        protected void Log(string message)
        {
            message = String.Format("FlightDataRecorder({0}[{1}]): {2}", Configuration, Configuration, message);
            TestFlightUtil.Log(message, this.part);
        }


        public bool TestFlightEnabled
        {
            get
            {
                // Verify we have a valid core attached
                if (core == null)
                    return false;
                // Our enabled status is the same as our bound core
                return core.TestFlightEnabled;
            }
        }
        public string Configuration
        {
            get 
            { 
                if (configuration.Equals(string.Empty))
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
            if (configs == null)
                configs = new List<ConfigNode>();
            
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

            if (configs == null)
                configs = new List<ConfigNode>();

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

        public void OnEnable()
        {
            if (core == null)
                core = TestFlightUtil.GetCore(this.part, Configuration);
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

            base.OnUpdate();

            float currentMet = core.GetOperatingTime();
            if (!IsRecordingFlightData())
            {
                lastRecordedMet = currentMet;
                return;
            }

            if (IsRecordingFlightData())
            {
                float flightData = (currentMet - lastRecordedMet) * flightDataMultiplier;
                float engineerBonus = core.GetEngineerDataBonus(flightDataEngineerModifier);
                flightData *= engineerBonus;
                if (flightData >= 0)
                    core.ModifyFlightData(flightData, true);
            }

            core.ModifyFlightTime(currentMet - lastRecordedMet, true);

            lastRecordedMet = currentMet;
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
            var node = ConfigNode.Parse(configNodeData);
            OnLoad(node);

            base.OnAwake();
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }

        public virtual List<string> GetTestFlightInfo()
        {
            return null;
        }
    }
}


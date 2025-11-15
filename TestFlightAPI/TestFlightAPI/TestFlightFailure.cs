using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestFlightAPI
{
    /// <summary>
    /// This part module provides a specific failure for the TestFlight system
    /// </summary>
    public class TestFlightFailureBase : PartModule, ITestFlightFailure
    {
        [KSPField]
        public string failureType;
        [KSPField]
        public string severity;
        [KSPField]
        public int weight;
        [KSPField]
        public string failureTitle = "Failure";
        [KSPField]
        public string configuration = "";
        [KSPField]
        public float duFail = 0f;
        [KSPField]
        public float duSucceed = 0f;
        [KSPField]
        public float duRepair = 0f;
        [KSPField]
        public bool oneShot = false;
        [KSPField]
        public bool awardDuInPreLaunch = true;

        [KSPField(isPersistant=true)]
        public bool failed;

        public List<ConfigNode> configs;
        public ConfigNode currentConfig;
        public string configNodeData;

        public bool Failed
        {
            get { return failed; }
            set { failed = value; }
        }

        public bool TestFlightEnabled
        {
            get
            {
                ITestFlightCore core = TestFlightUtil.GetCore(this.part, Configuration);
                if (core != null)
                    return core.TestFlightEnabled;
                return false;
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

        protected void Log(string message)
        {
            message = String.Format("TestFlightFailure({0}[{1}]): {2}", Configuration, Configuration, message);
            TestFlightUtil.Log(message, this.part);
        }

        /// <summary>
        /// Gets the details of the failure encapsulated by this module.  In most cases you can let the base class take care of this unless you need to do something special
        /// </summary>
        /// <returns>The failure details.</returns>
        public virtual TestFlightFailureDetails GetFailureDetails()
        {
            TestFlightFailureDetails details = new TestFlightFailureDetails();

            details.weight = weight;
            details.failureType = failureType;
            details.severity = severity;
            details.failureTitle = failureTitle;

            return details;
        }

        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public virtual void DoFailure()
        {
            bool previouslyFailed = Failed;
            Failed = true;
            ITestFlightCore core = TestFlightUtil.GetCore(this.part, Configuration);
            if (core != null)
            {
                string failMessage;
                var failTime = KSPUtil.PrintTimeCompact((int)Math.Floor(this.vessel.missionTime), false);
                if (!previouslyFailed && (awardDuInPreLaunch || vessel.situation != Vessel.Situations.PRELAUNCH))
                {
                    core.ModifyFlightData(duFail, true);
                    failMessage = $"[{failTime}] {core.Title} has failed with {failureTitle}, but this failure has given us valuable data";
                }
                else
                {
                    failMessage = $"[{failTime}] {core.Title} has failed with {failureTitle}";

                }
                FlightLogger.eventLog.Add(failMessage);
                if (!previouslyFailed)
                {
                    core.LogCareerFailure(vessel, failureTitle);
                }
            }
        }
        
        /// <summary>
        /// Forces the repair.  This should instantly repair the part, regardless of whether or not a normal repair can be done.  IOW if at all possible the failure should fixed after this call.
        /// This is made available as an API method to allow things like failure simulations.
        /// </summary>
        /// <returns><c>true</c>, if failure was repaired, <c>false</c> otherwise.</returns>
        public virtual float ForceRepair()
        {
            return DoRepair();
        }

        public virtual float DoRepair()
        {
            Failed = false;
            ITestFlightCore core = TestFlightUtil.GetCore(this.part, Configuration);
            if (core != null && (awardDuInPreLaunch || vessel.situation != Vessel.Situations.PRELAUNCH))
                core.ModifyFlightData(duRepair, true);
            return 0;
        }

        public virtual void SetActiveConfig(string alias)
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
            currentConfig.TryGetValue("configuration", ref configuration);
            currentConfig.TryGetValue("failureType", ref failureType);
            currentConfig.TryGetValue("severity", ref severity);
            currentConfig.TryGetValue("weight", ref weight);
            currentConfig.TryGetValue("failureTitle", ref failureTitle);
            currentConfig.TryGetValue("duFail", ref duFail);
            currentConfig.TryGetValue("duSucceed", ref duSucceed);
            currentConfig.TryGetValue("duRepair", ref duRepair);
            currentConfig.TryGetValue("oneShot", ref oneShot);
            currentConfig.TryGetValue("awardDuInPreLaunch", ref awardDuInPreLaunch);
        }

        public virtual List<string> GetTestFlightInfo() => new List<string>();

        public override void OnAwake()
        {
            if (!string.IsNullOrEmpty(configNodeData))
            {
                var node = ConfigNode.Parse(configNodeData);
                OnLoad(node);
            }

            Failed = false;
            base.OnAwake();
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
        
        public virtual float GetRepairTime()
        {
            return 0f;
        }
        public virtual float AttemptRepair()
        {
            return 0f;
        }
        public virtual bool CanAttemptRepair()
        {
            return true;
        }

        public virtual string GetModuleInfo(string configuration)
        {
            return string.Empty;
        }
    }
}

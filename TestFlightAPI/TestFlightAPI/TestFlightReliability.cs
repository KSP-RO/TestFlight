using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TestFlightAPI
{
    /// <summary>
    /// This part module determines the part's current reliability and passes that on to the TestFlight core.
    /// </summary>
    public class TestFlightReliabilityBase : PartModule, ITestFlightReliability
    {
        protected ITestFlightCore core = null;

        [KSPField]
        public string configuration = "";
        [KSPField]
        public FloatCurve reliabilityCurve;
        [KSPField(isPersistant=true)]
        public float lastCheck = 0;
        [KSPField(isPersistant=true)]
        public float lastReliability = 1.0f;

    
        public List<ConfigNode> configs;
        public ConfigNode currentConfig;
        public string configNodeData;

        public bool TestFlightEnabled
        {
            get
            {
                // Verify we have a valid core attached
                if (core == null)
                    return false;
                Log("TestFlightEnabled");
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

        protected bool verboseDebugging;

        protected void Log(string message)
        {
            message = String.Format("TestFlightReliability({0}[{1}]): {2}", Configuration, Configuration, message);
            TestFlightUtil.Log(message, this.part);
        }

        // New API
        // Get the base or static failure rate for the given scope
        // !! IMPORTANT: Only ONE Reliability module may return a Base Failure Rate.  Additional modules can exist only to supply Momentary rates
        // If this Reliability module's purpose is to supply Momentary Fialure Rates, then it MUST return 0 when asked for the Base Failure Rate
        // If it dosn't, then the Base Failure Rate of the part will not be correct.
        //
        public virtual double GetBaseFailureRate(float flightData)
        {
            if (reliabilityCurve != null)
            {
                Log(String.Format("{0:F2} data evaluates to {1:F2} failure rate", flightData, reliabilityCurve.Evaluate(flightData)));
                return reliabilityCurve.Evaluate(flightData);
            }
            else
            {
                Log("No reliability curve. Returning min failure rate.");
                return TestFlightUtil.MIN_FAILURE_RATE;
            }
        }
        public FloatCurve GetReliabilityCurve()
        {
            return reliabilityCurve;
        }

        // INTERNAL methods

        protected void Startup()
        {
        }

        // PARTMODULE Implementation
        public override void OnAwake()
        {
            var node = ConfigNode.Parse(configNodeData);
            OnLoad(node);
            
            if (reliabilityCurve == null)
            {
                reliabilityCurve = new FloatCurve();
                reliabilityCurve.Add(0f, 1f);
            }
        }

        public void OnEnable()
        {
            if (core == null)
                core = TestFlightUtil.GetCore(this.part, Configuration);
            if (core != null)
                Startup();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (core == null)
                core = TestFlightUtil.GetCore(this.part, Configuration);
            if (core != null)
                Startup();
            verboseDebugging = core.DebugEnabled;
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
            if (currentConfig.HasNode("reliabilityCurve"))
            {
                reliabilityCurve = new FloatCurve();
                reliabilityCurve.Load(currentConfig.GetNode("reliabilityCurve"));
            }
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

        // public override void OnLoad(ConfigNode node)
        // {
        //     // As of v1.3 we dont' need to worry about reliability bodies anymore, just a simple FloatCurve
        //     if (node.HasNode("reliabilityCurve"))
        //     {
        //         reliabilityCurve = new FloatCurve();
        //         reliabilityCurve.Load(node.GetNode("reliabilityCurve"));
        //     }
        //     base.OnLoad(node);
        // }


        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!TestFlightEnabled)
                return;

            // NEW RELIABILITY CODE
            float operatingTime = core.GetOperatingTime();

            if (operatingTime < lastCheck + 1f)
                return;

            lastCheck = operatingTime;
            double baseFailureRate = core.GetBaseFailureRate();
            MomentaryFailureRate momentaryFailureRate = core.GetWorstMomentaryFailureRate();
            double currentFailureRate;

            if (momentaryFailureRate.valid && momentaryFailureRate.failureRate > baseFailureRate)
                currentFailureRate = momentaryFailureRate.failureRate;
            else
                currentFailureRate = baseFailureRate;

            // Given we have been operating for a given number of seconds, calculate our chance of survival to that time based on currentFailureRate
            // This is *not* an exact science, as the real calculations are much more complex than this, plus technically the momentary rate for
            // *each* second should be accounted for, but this is a simplification of the system.  It provides decent enough numbers for fun gameplay
            // with chance of failure increasing exponentially over time as it approaches the *current* MTBF
            // S() is survival chance, f is currentFailureRate
            // S(t) = e^(-f*t)

            float reliability = Mathf.Exp((float)-currentFailureRate * (float)operatingTime);
//            double survivalChance = Mathf.Pow(Mathf.Exp(1), (float)currentFailureRate * (float)operatingTime * -0.693f);
            double survivalChance = reliability / lastReliability;
            lastReliability = reliability;
//            float failureRoll = Mathf.Min(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
//            float failureRoll = UnityEngine.Random.Range(0f, 1f);
            double failureRoll = core.RandomGenerator.NextDouble();
            if (verboseDebugging)
            {
                Log(String.Format("Survival Chance at Time {0:F2} is {1:f4}.  Rolled {2:f4}", (float)operatingTime, survivalChance, failureRoll));
            }
            if (failureRoll > survivalChance)
            {
//                Debug.Log(String.Format("TestFlightReliability: Survival Chance at Time {0:F2} is {1:f4} -- {2:f4}^({3:f4}*{0:f2}*-1.0)", (float)operatingTime, survivalChance, Mathf.Exp(1), (float)currentFailureRate));
                if (verboseDebugging)
                {
                    Log(String.Format("Part has failed after {1:F1} secodns of operation at MET T+{2:F2} seconds with roll of {0:F4}", failureRoll, operatingTime, this.vessel.missionTime));
                }
                core.TriggerFailure();
            }
        }

        public virtual List<string> GetTestFlightInfo()
        {
            List<string> infoStrings = new List<string>();

            if (core == null)
            {
                Log("Core is null");
                return infoStrings;
            }
            if (reliabilityCurve == null)
            {
                Log("Curve is null");
                return infoStrings;
            }

            infoStrings.Add("<b>Base Reliability</b>");
            infoStrings.Add(String.Format("<b>Current Reliability</b>: {0} <b>MTBF</b>", core.FailureRateToMTBFString(core.GetBaseFailureRate(), TestFlightUtil.MTBFUnits.SECONDS, 999)));
            infoStrings.Add(String.Format("<b>Maximum Reliability</b>: {0} <b>MTBF</b>", core.FailureRateToMTBFString(GetBaseFailureRate(reliabilityCurve.maxTime), TestFlightUtil.MTBFUnits.SECONDS, 999)));

            return infoStrings;
        }

        public virtual string GetModuleInfo()
        {
            return string.Empty;
        }
    }
}


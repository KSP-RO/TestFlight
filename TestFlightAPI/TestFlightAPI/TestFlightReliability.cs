using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TestFlightAPI
{
    public class ReliabilityBodyConfig : IConfigNode
    {
        public string scope = "NONE";
        public FloatCurve reliabilityCurve;

        public void Load(ConfigNode node)
        {
            if (node.HasValue("scope"))
                scope = node.GetValue("scope").ToLower();
            if (node.HasNode("reliabilityCurve"))
            {
                reliabilityCurve = new FloatCurve();
                reliabilityCurve.Load(node.GetNode("reliabilityCurve"));
            }
        }

        public void Save(ConfigNode node)
        {
        }

        public override string ToString()
        {
            string stringRepresentation = "";

            return stringRepresentation;
        }

    }

    /// <summary>
    /// This part module determines the part's current reliability and passes that on to the TestFlight core.
    /// </summary>
    public class TestFlightReliabilityBase : PartModule, ITestFlightReliability
    {
        protected ITestFlightCore core = null;
        protected List<ReliabilityBodyConfig> reliabilityBodies = null;
        protected double lastCheck = 0;
        protected double lastReliability = 1.0;

        [KSPField(isPersistant=true)]
        public string configuration = "";

        public bool TestFlightEnabled
        {
            get
            {
                bool enabled = true;
                // Verify we have a valid core attached
                if (core == null)
                    enabled = false;
                // If this part has a ModuleEngineConfig then we need to verify we are assigned to the active configuration
                if (this.part.Modules.Contains("ModuleEngineConfigs"))
                {
                    string currentConfig = (string)(part.Modules["ModuleEngineConfigs"].GetType().GetField("configuration").GetValue(part.Modules["ModuleEngineConfigs"]));
                    if (currentConfig != configuration)
                        enabled = false;
                }
                return enabled;
            }
        }

        public string Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }

        // New API
        // Get the base or static failure rate for the given scope
        // !! IMPORTANT: Only ONE Reliability module may return a Base Failure Rate.  Additional modules can exist only to supply Momentary rates
        // If this Reliability module's purpose is to supply Momentary Fialure Rates, then it MUST return 0 when asked for the Base Failure Rate
        // If it dosn't, then the Base Failure Rate of the part will not be correct.
        //
        public virtual double GetBaseFailureRateForScope(double flightData, String scope)
        {
            if (!TestFlightEnabled)
                return 0;
                
            ReliabilityBodyConfig body = GetConfigForScope(scope);
            if (body == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            FloatCurve curve = body.reliabilityCurve;
            if (curve == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            double reliability = curve.Evaluate((float)flightData);
            Debug.Log(String.Format("TestFlightFailure: {0}|{1} reporting BFR of {2:F4}", TestFlightUtil.GetFullPartName(this.part), configuration, reliability));
            return reliability;
        }

        public FloatCurve GetReliabilityCurveForScope(String scope)
        {
            if (!TestFlightEnabled)
                return null;

            ReliabilityBodyConfig body = GetConfigForScope(scope);
            if (body == null)
                return null;

            FloatCurve curve = body.reliabilityCurve;
            return curve;
        }


        // INTERNAL methods
        private ReliabilityBodyConfig GetConfigForScope(String scope)
        {
            if (reliabilityBodies == null)
                return null;

            ReliabilityBodyConfig returnBody = reliabilityBodies.Find(s => s.scope == scope);
            if (returnBody == null)
                returnBody = reliabilityBodies.Find(s => s.scope == "default");
            return returnBody;
        }

        IEnumerator Attach()
        {
            while (this.part == null || this.part.partInfo == null || this.part.partInfo.partPrefab == null || this.part.Modules == null)
            {
                yield return null;
            }

            while (core == null)
            {
                core = TestFlightUtil.GetCore(this.part);
                yield return null;
            }

            Startup();
        }

        protected void LoadDataFromPrefab()
        {
            Part prefab = this.part.partInfo.partPrefab;
            foreach (PartModule pm in prefab.Modules)
            {
                TestFlightReliabilityBase modulePrefab = pm as TestFlightReliabilityBase;
                if (modulePrefab != null && modulePrefab.Configuration == configuration)
                    reliabilityBodies = modulePrefab.reliabilityBodies;
            }
        }

        protected void Startup()
        {
            LoadDataFromPrefab();
//            UnityEngine.Random.seed = (int)Time.time;
        }

        // PARTMODULE Implementation
        public override void OnAwake()
        {
            StartCoroutine("Attach");
        }


        public override void OnLoad(ConfigNode node)
        {
            if (node.HasNode("RELIABILITY_BODY"))
            {
                if (reliabilityBodies == null)
                    reliabilityBodies = new List<ReliabilityBodyConfig>();
                foreach (ConfigNode bodyNode in node.GetNodes("RELIABILITY_BODY"))
                {
                    ReliabilityBodyConfig reliabilityBody = new ReliabilityBodyConfig();
                    reliabilityBody.Load(bodyNode);
                    reliabilityBodies.Add(reliabilityBody);
                }
            }
            base.OnLoad(node);
        }


        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!TestFlightEnabled)
                return;

            // NEW RELIABILITY CODE
            double operatingTime = core.GetOperatingTime();
//            Debug.Log(String.Format("TestFlightReliability: Operating Time = {0:F2}", operatingTime));
            if (operatingTime == -1)
                lastCheck = 0;

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

            double reliability = Mathf.Exp((float)-currentFailureRate * (float)operatingTime);
//            double survivalChance = Mathf.Pow(Mathf.Exp(1), (float)currentFailureRate * (float)operatingTime * -0.693f);
            double survivalChance = reliability / lastReliability;
            lastReliability = reliability;
//            float failureRoll = Mathf.Min(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
//            float failureRoll = UnityEngine.Random.Range(0f, 1f);
            double failureRoll = TestFlightUtil.GetCore(this.part).RandomGenerator.NextDouble();
            Debug.Log(String.Format("TestFlightReliability: Survival Chance at Time {0:F2} is {1:f4}.  Rolled {2:f4}", (float)operatingTime, survivalChance, failureRoll));
            if (failureRoll > survivalChance)
            {
//                Debug.Log(String.Format("TestFlightReliability: Survival Chance at Time {0:F2} is {1:f4} -- {2:f4}^({3:f4}*{0:f2}*-1.0)", (float)operatingTime, survivalChance, Mathf.Exp(1), (float)currentFailureRate));
                Debug.Log(String.Format("TestFlightReliability: Part has failed after {1:F1} secodns of operation at MET T+{2:F2} seconds with roll of {0:F4}", failureRoll, operatingTime, this.vessel.missionTime));
                core.TriggerFailure();
            }
        }
    }
}


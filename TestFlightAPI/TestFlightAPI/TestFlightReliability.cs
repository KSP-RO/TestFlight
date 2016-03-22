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
        // As of v1.3 we no longer have scopes, and therefore don't need eliabilityBodies at all
        // We simply store a base curve
//        protected List<ReliabilityBodyConfig> reliabilityBodies = null;
        protected FloatCurve reliabilityCurve = null;

        [KSPField]
        public string configuration = "";
        [KSPField(isPersistant=true)]
        public float lastCheck = 0;
        [KSPField(isPersistant=true)]
        public float lastReliability = 1.0f;

        public bool TestFlightEnabled
        {
            get
            {
                // Verify we have a valid core attached
                if (core == null)
                    return false;
                if (string.IsNullOrEmpty(Configuration))
                    return true;
                return TestFlightUtil.EvaluateQuery(Configuration, this.part);
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
            message = String.Format("TestFlightReliability({0}[{1}]): {2}", TestFlightUtil.GetFullPartName(this.part), Configuration, message);
            TestFlightUtil.Log(message, this.part);
        }

        // New API
        // Get the base or static failure rate for the given scope
        // !! IMPORTANT: Only ONE Reliability module may return a Base Failure Rate.  Additional modules can exist only to supply Momentary rates
        // If this Reliability module's purpose is to supply Momentary Fialure Rates, then it MUST return 0 when asked for the Base Failure Rate
        // If it dosn't, then the Base Failure Rate of the part will not be correct.
        //
        // As of v1.3 we no longer have scopes!
//        public virtual double GetBaseFailureRateForScope(double flightData, String scope)
//        {
//            if (!TestFlightEnabled)
//                return 0;
//                
//            ReliabilityBodyConfig body = GetConfigForScope(scope);
//            if (body == null)
//                return TestFlightUtil.MIN_FAILURE_RATE;
//
//            FloatCurve curve = body.reliabilityCurve;
//            if (curve == null)
//                return TestFlightUtil.MIN_FAILURE_RATE;
//
//            double reliability = curve.Evaluate((float)flightData);
//            Log(String.Format("Reporting BFR of {0:F4}", reliability));
//            return reliability;
//        }
        // New v1.3 noscope implementation
        public virtual double GetBaseFailureRate(float flightData)
        {
            if (reliabilityCurve != null)
                return reliabilityCurve.Evaluate(flightData);
            else
                return TestFlightUtil.MIN_FAILURE_RATE;
        }
        // As of v1.3 we no longer have scopes
//        public FloatCurve GetReliabilityCurveForScope(String scope)
//        {
//            if (!TestFlightEnabled)
//                return null;
//
//            ReliabilityBodyConfig body = GetConfigForScope(scope);
//            if (body == null)
//                return null;
//
//            FloatCurve curve = body.reliabilityCurve;
//            return curve;
//        }
        public FloatCurve GetReliabilityCurve()
        {
            return reliabilityCurve;
        }

        // INTERNAL methods
        // As of v1.3 we don't need this anymore.  The removal of scopes means we don't need this complex data store
//        private ReliabilityBodyConfig GetConfigForScope(String scope)
//        {
//            if (reliabilityBodies == null)
//                return null;
//
//            ReliabilityBodyConfig returnBody = reliabilityBodies.Find(s => s.scope == scope);
//            if (returnBody == null)
//                returnBody = reliabilityBodies.Find(s => s.scope == "default");
//            return returnBody;
//        }

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
            Log("Loading data from prefab");
            Part prefab = this.part.partInfo.partPrefab;
            foreach (PartModule pm in prefab.Modules)
            {
                TestFlightReliabilityBase modulePrefab = pm as TestFlightReliabilityBase;
                // As of v1.3 this is simpler because we don't have scope or reliability bodies
                if (modulePrefab != null && TestFlightUtil.EvaluateQuery(modulePrefab.Configuration, this.part))
                {
                    Log("Found matching prefab");
                    if (modulePrefab.reliabilityCurve != null && modulePrefab.reliabilityCurve.maxTime > 0)
                    {
                        Log(String.Format("Found reliabilityCurve with data point between {0:F2} and {1:F2}.  Loading curve from prefab", modulePrefab.reliabilityCurve.minTime, modulePrefab.reliabilityCurve.maxTime));
                        reliabilityCurve = modulePrefab.reliabilityCurve;
                        return;
                    }
                }
            }
        }

        protected void Startup()
        {
            Log("Startup");
            LoadDataFromPrefab();
            Log("Startup::DONE");
        }

        // PARTMODULE Implementation
        public override void OnAwake()
        {
            StartCoroutine("Attach");
        }


        public override void OnLoad(ConfigNode node)
        {
            // As of v1.3 we dont' need to worry about reliability bodies anymore, just a simple FloatCurve
            if (node.HasNode("reliabilityCurve"))
            {
                reliabilityCurve = new FloatCurve();
                reliabilityCurve.Load(node.GetNode("reliabilityCurve"));
            }
//            if (node.HasNode("RELIABILITY_BODY"))
//            {
//                if (reliabilityBodies == null)
//                    reliabilityBodies = new List<ReliabilityBodyConfig>();
//                foreach (ConfigNode bodyNode in node.GetNodes("RELIABILITY_BODY"))
//                {
//                    ReliabilityBodyConfig reliabilityBody = new ReliabilityBodyConfig();
//                    reliabilityBody.Load(bodyNode);
//                    reliabilityBodies.Add(reliabilityBody);
//                }
//            }
            base.OnLoad(node);
        }


        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!TestFlightEnabled)
                return;

            // NEW RELIABILITY CODE
            float operatingTime = core.GetOperatingTime();
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

            float reliability = Mathf.Exp((float)-currentFailureRate * (float)operatingTime);
//            double survivalChance = Mathf.Pow(Mathf.Exp(1), (float)currentFailureRate * (float)operatingTime * -0.693f);
            double survivalChance = reliability / lastReliability;
            lastReliability = reliability;
//            float failureRoll = Mathf.Min(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
//            float failureRoll = UnityEngine.Random.Range(0f, 1f);
            double failureRoll = core.RandomGenerator.NextDouble();
            Log(String.Format("Survival Chance at Time {0:F2} is {1:f4}.  Rolled {2:f4}", (float)operatingTime, survivalChance, failureRoll));
            if (failureRoll > survivalChance)
            {
//                Debug.Log(String.Format("TestFlightReliability: Survival Chance at Time {0:F2} is {1:f4} -- {2:f4}^({3:f4}*{0:f2}*-1.0)", (float)operatingTime, survivalChance, Mathf.Exp(1), (float)currentFailureRate));
                Log(String.Format("Part has failed after {1:F1} secodns of operation at MET T+{2:F2} seconds with roll of {0:F4}", failureRoll, operatingTime, this.vessel.missionTime));
                core.TriggerFailure();
            }
        }
    }
}


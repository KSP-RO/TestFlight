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
        private double lastCheck = 0;

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
        private ITestFlightCore core = null;
        public List<ReliabilityBodyConfig> reliabilityBodies = null;
        double lastCheck = 0;
        protected bool isReady = false;

        // New API
        // Get the base or static failure rate for the given scope
        // !! IMPORTANT: Only ONE Reliability module may return a Base Failure Rate.  Additional modules can exist only to supply Momentary rates
        // If this Reliability module's purpose is to supply Momentary Fialure Rates, then it MUST return 0 when asked for the Base Failure Rate
        // If it dosn't, then the Base Failure Rate of the part will not be correct.
        //
        public double GetBaseFailureRateForScope(double flightData, String scope)
        {
            Debug.Log(String.Format("TestFlightReliabilityBase: GetBaseFailureRateForScope({0:F2}, {1})", flightData, scope));
            if (core == null)
            {
                Debug.Log(String.Format("TestFlightReliabilityBase: core is invalid"));
                return 0;
            }

            ReliabilityBodyConfig body = GetConfigForScope(scope);
            if (body == null)
            {
                Debug.Log(String.Format("TestFlightReliabilityBase: No bodyConfig found"));
                return 0;
            }

            FloatCurve curve = body.reliabilityCurve;
            if (curve == null)
            {
                Debug.Log(String.Format("TestFlightReliabilityBase: reliabilityCurve is invalid"));
                return 0;
            }

            double reliability = curve.Evaluate((float)flightData);
            Debug.Log(String.Format("TestFlightReliability: reliability is {0:F2} with {1:F2} data units", reliability, flightData));
            return reliability;
        }
        // Get the momentary (IE current dynamic) failure modifier
        // The reliability module should only return its MODIFIER for the current time at the given scope (or current scope if not given).  The Core will calculate the final failure rate.
        // !! IF NOT USED THEN THE MODULE MUST RETURN A VALUE OF 1 SO AS TO NOT MODIFY THE RATE !!
        public double GetMomentaryFailureModifierForScope(String scope)
        {
            // We don't have a momemntary modifier so we return 1, since its a multipler
            return 1;
        }
        public FloatCurve GetReliabilityCurveForScope(String scope)
        {
            if (core == null)
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
            Debug.Log(String.Format("TestFlightReliabilityBase: GetConfigForScope({0})", scope));
            if (reliabilityBodies == null)
            {
                Debug.Log(String.Format("TestFlightReliabilityBase: reliabilityBodies is invalid"));
                return null;
            }
            foreach (ReliabilityBodyConfig config in reliabilityBodies)
            {
                Debug.Log(String.Format("TestFlightReliabilityBase: found entry {0}", config.scope));
            }
            return reliabilityBodies.Find(s => s.scope == scope);
        }

        IEnumerator Attach()
        {
            while (this.part == null || this.part.partInfo == null || this.part.partInfo.partPrefab == null || this.part.Modules == null)
            {
                yield return null;
            }

            Part prefab = this.part.partInfo.partPrefab;
            foreach (PartModule pm in prefab.Modules)
            {
                TestFlightReliabilityBase modulePrefab = pm as TestFlightReliabilityBase;
                if (modulePrefab != null)
                {
                    Debug.Log("TestFlightReliabilityBase: Reloading data from Prefab");
                    reliabilityBodies = modulePrefab.reliabilityBodies;
                    Debug.Log("TestFlightReliabilityBase: " + reliabilityBodies.Count + " scopes in reliability data");
                }
            }

            while (core == null)
            {
                foreach (PartModule pm in this.part.Modules)
                {
                    core = pm as ITestFlightCore;
                    if (core != null)
                    {
                        Debug.Log("TestFlightReliabilityBase: Attaching to core");
                        break;
                    }
                }
                yield return null;
            }
            if (this.part.started)
                isReady = true;
        }

        // PARTMODULE Implementation
        public override void OnAwake()
        {
            String partName;
            if (this.part != null)
                partName = this.part.name;
            else
                partName = "unknown";
            Debug.Log("TestFlightReliabilityBase: OnAwake(" + partName + ")");

            if (this.part == null || this.part.Modules == null)
            {
                Debug.Log("TestFlightReliabilityBase: Starting Coroutine to setup when part is available");
                StartCoroutine("Attach");
                return;
            }

            foreach (PartModule pm in this.part.Modules)
            {
                core = pm as ITestFlightCore;
                if (core != null)
                    break;
            }

            if (core == null)
            {
                Debug.Log("Starting Coroutine to find core module");
                StartCoroutine("Attach");
                return;
            }
            if (this.part.partInfo == null || this.part.partInfo.partPrefab == null)
            {
                Debug.Log("Can't find partInfo or partPrefab.  Starting Coroutine to attach later.");
                StartCoroutine("Attach");
                return;
            }
            Part prefab = this.part.partInfo.partPrefab;
            foreach (PartModule pm in prefab.Modules)
            {
                TestFlightReliabilityBase modulePrefab = pm as TestFlightReliabilityBase;
                if (modulePrefab != null)
                {
                    Debug.Log("TestFlightReliabilityBase: Reloading data from Prefab");
                    reliabilityBodies = modulePrefab.reliabilityBodies;
                    if (reliabilityBodies == null)
                    {
                        Debug.Log("TestFlightReliabilityBase: Prefab data is invalid!");
                    }
                    else
                        Debug.Log("TestFlightReliabilityBase: " + reliabilityBodies.Count + " scopes in reliability data");
                }
            }
            Debug.Log("TestFlightReliabilityBase: OnAwake(" + partName + "):DONE");
        }

        public void Start()
        {
            String partName;
            if (this.part != null)
                partName = this.part.name;
            else
                partName = "unknown";
            Debug.Log("TestFlightReliabilityBase: Start(" + partName + ")");

            Part prefab = this.part.partInfo.partPrefab;
            foreach (PartModule pm in prefab.Modules)
            {
                TestFlightReliabilityBase modulePrefab = pm as TestFlightReliabilityBase;
                if (modulePrefab != null)
                {
                    Debug.Log("TestFlightReliabilityBase: Reloading data from Prefab");
                    reliabilityBodies = modulePrefab.reliabilityBodies;
                    if (reliabilityBodies == null)
                    {
                        Debug.Log("TestFlightReliabilityBase: Prefab data is invalid!");
                    }
                    else
                        Debug.Log("TestFlightReliabilityBase: " + reliabilityBodies.Count + " scopes in reliability data");
                }
            }

            UnityEngine.Random.seed = (int)Time.time;
            Debug.Log("TestFlightReliabilityBase: Start(" + partName + "):DONE");
        }

        public override void OnStart(StartState state)
        {
            String partName;
            if (this.part != null)
                partName = this.part.name;
            else
                partName = "unknown";
            Debug.Log("TestFlightReliabilityBase: OnStart(" + partName + ")");

            base.OnStart(state);

            Part prefab = this.part.partInfo.partPrefab;
            foreach (PartModule pm in prefab.Modules)
            {
                TestFlightReliabilityBase modulePrefab = pm as TestFlightReliabilityBase;
                if (modulePrefab != null)
                {
                    Debug.Log("TestFlightReliabilityBase: Reloading data from Prefab");
                    reliabilityBodies = modulePrefab.reliabilityBodies;
                    if (reliabilityBodies == null)
                    {
                        Debug.Log("TestFlightReliabilityBase: Prefab data is invalid!");
                    }
                    else
                        Debug.Log("TestFlightReliabilityBase: " + reliabilityBodies.Count + " scopes in reliability data");
                }
            }


            isReady = true;
            Debug.Log("TestFlightReliabilityBase: OnStart(" + partName + "):DONE");
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
                Debug.Log("TestFlightReliabilityBase: Loaded " + reliabilityBodies.Count + " reliability bodies from config");
            }
            else
            {
                Part prefab = this.part.partInfo.partPrefab;
                foreach (PartModule pm in prefab.Modules)
                {
                    TestFlightReliabilityBase modulePrefab = pm as TestFlightReliabilityBase;
                    if (modulePrefab != null)
                    {
                        Debug.Log("TestFlightReliabilityBase: Reloading data from Prefab");
                        reliabilityBodies = modulePrefab.reliabilityBodies;
                        if (reliabilityBodies == null)
                        {
                            Debug.Log("TestFlightReliabilityBase: Prefab data is invalid!");
                        }
                        else
                            Debug.Log("TestFlightReliabilityBase: " + reliabilityBodies.Count + " scopes in reliability data");
                    }
                }
            }
            base.OnLoad(node);
        }


        public override void OnUpdate()
        {
            base.OnUpdate();

            if (core == null)
                return;

            if (!isReady)
                return;

            // NEW RELIABILITY CODE
            double operatingTime = core.GetOperatingTime();
//            Debug.Log(String.Format("TestFlightReliability: Operating Time = {0:F2}", operatingTime));
            if (operatingTime < lastCheck + 5f)
                return;

            lastCheck = operatingTime;
            double baseFailureRate = core.GetBaseFailureRate();
            MomentaryFailureRate momentaryFailureRate = core.GetWorstMomentaryFailureRate();
            double currentFailureRate;

            if (momentaryFailureRate.valid)
                currentFailureRate = momentaryFailureRate.failureRate;
            else
                currentFailureRate = baseFailureRate;

            double mtbf = core.FailureRateToMTBF(currentFailureRate, TestFlightUtil.MTBFUnits.SECONDS);
            if (operatingTime > mtbf)
                operatingTime = mtbf;

            // Given we have been operating for a given number of seconds, calculate our chance of survival to that time based on currentFailureRate
            // This is *not* an exact science, as the real calculations are much more complex than this, plus technically the momentary rate for
            // *each* second should be accounted for, but this is a simplification of the system.  It provides decent enough numbers for fun gameplay
            // with chance of failure increasing exponentially over time as it approaches the *current* MTBF
            // S() is survival chance, f is currentFailureRate
            // S(t) = e^(-f*t)

            double survivalChance = Mathf.Pow(Mathf.Exp(1), (float)currentFailureRate * (float)operatingTime * -0.693f);
//            Debug.Log(String.Format("TestFlightReliability: Survival Chance at Time {0:F2} is {1:f4} -- {2:f4}^({3:f4}*{0:f2}*-1.0)", (float)operatingTime, survivalChance, Mathf.Exp(1), (float)currentFailureRate));
            float failureRoll = Mathf.Min(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
            if (failureRoll > survivalChance)
            {
//                Debug.Log(String.Format("TestFlightReliability: Survival Chance at Time {0:F2} is {1:f4} -- {2:f4}^({3:f4}*{0:f2}*-1.0)", (float)operatingTime, survivalChance, Mathf.Exp(1), (float)currentFailureRate));
                Debug.Log(String.Format("TestFlightReliability: Part has failed after {1:F1} secodns of operation at MET T+{2:F2} seconds with roll of {0:F4}", failureRoll, operatingTime, this.vessel.missionTime));
                core.TriggerFailure();
            }
        }
    }
}


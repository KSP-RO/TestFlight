using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestFlightAPI
{
    public class ReliabilityBodyConfig : IConfigNode
    {
        [KSPField(isPersistant = true)]
        public string scope = "NONE";
        [KSPField(isPersistant = true)]
        public FloatCurve reliabilityCurve;

        public void Load(ConfigNode node)
        {
            if (node.HasValue("scope"))
                scope = node.GetValue("scope").ToLower();
        }

        public void Save(ConfigNode node)
        {
        }

        public override string ToString()
        {
            string stringRepresentation = "";

            stringRepresentation = scope;
            stringRepresentation += "\n" + reliabilityCurve.ToString();
            return stringRepresentation;
        }

    }

    /// <summary>
    /// This part module determines the part's current reliability and passes that on to the TestFlight core.
    /// </summary>
    public class TestFlightReliabilityBase : PartModule, ITestFlightReliability
    {
        private ITestFlightCore core = null;
        public List<ReliabilityBodyConfig> reliabilityBodies;

        // New API
        // Get the base or static failure rate for the given scope
        // !! IMPORTANT: Only ONE Reliability module may return a Base Failure Rate.  Additional modules can exist only to supply Momentary rates
        // If this Reliability module's purpose is to supply Momentary Fialure Rates, then it MUST return 0 when asked for the Base Failure Rate
        // If it dosn't, then the Base Failure Rate of the part will not be correct.
        public double GetBaseFailureRateForScope(double flightData, String scope)
        {
            if (core == null)
                return 0;

            ReliabilityBodyConfig body = GetConfigForScope(scope);
            if (body == null)
                return 0;
            FloatCurve curve = body.reliabilityCurve;
            if (curve == null)
            {
                Debug.Log("TestFlightReliability: reliabilityCurve is not valid!");
                return 0;
            }
            double reliability = curve.Evaluate((float)flightData);
            Debug.Log(String.Format("TestFlightReliability: reliability is {0:F2) with {1:F2} data units", reliability, flightData));
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

        // INTERNAL methods
        private ReliabilityBodyConfig GetConfigForScope(String scope)
        {
            return reliabilityBodies.Find(s => s.scope == scope);
        }

        // PARTMODULE Implementation
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            foreach (PartModule pm in this.part.Modules)
            {
                core = pm as ITestFlightCore;
                if (core != null)
                    break;
            }
            if (reliabilityBodies == null)
                reliabilityBodies = new List<ReliabilityBodyConfig>();

        }
        public override void OnLoad(ConfigNode node)
        {
            Debug.Log("TestFlightReliability: OnLoad()");
            if (reliabilityBodies == null)
                reliabilityBodies = new List<ReliabilityBodyConfig>();
            foreach (ConfigNode bodyNode in node.GetNodes("RELIABILITY_BODY"))
            {
                ReliabilityBodyConfig reliabilityBody = new ReliabilityBodyConfig();
                reliabilityBody.Load(bodyNode);
                reliabilityBodies.Add(reliabilityBody);
                Debug.Log("TestFlightReliability: ReliabilityBody " + bodyNode.ToString());
            }
            base.OnLoad(node);
        }


        public override void OnUpdate()
        {
            base.OnUpdate();

            if (core == null)
                return;

            if (!isEnabled)
                return;

            // TODO
            // This code is just placeholder as we migrate to the new system.  This reimplements the old failure check for now.
            if (UnityEngine.Random.Range(0f, 100f) > core.GetBaseFailureRate())
            {
                core.TriggerFailure();
            }
            return;







            // NEW RELIABILITY CODE
            // Not implemented yet!
            // TODO
            double operatingTime = core.GetOperatingTime();
            double baseFailureRate = core.GetBaseFailureRate();
            MomentaryFailureRate momentaryFailureRate = core.GetWorstMomentaryFailureRate();
            double currentFailureRate;

            if (momentaryFailureRate.valid)
                currentFailureRate = momentaryFailureRate.failureRate;
            else
                currentFailureRate = baseFailureRate;

            double mtbf = core.FailureRateToMTBF(currentFailureRate, "seconds");
            if (operatingTime > mtbf)
                operatingTime = mtbf;

            // Given we have been operating for a given number of seconds, calculate our chance of survival to that time based on currentFailureRate
            // This is *not* an exact science, as the real calculations are much more complex than this, plus technically the momentary rate for
            // *each* second should be accounted for, but this is a simplification of the system.  It provides decent enough numbers for fun gameplay
            // with chance of failure increasing exponentially over time as it approaches the *current* MTBF
            // S() is survival chance, f is currentFailureRate
            // S(t) = e^(-f*t)

            double survivalChance = Mathf.Pow(Mathf.Epsilon, (float)currentFailureRate * (float)operatingTime * -1f);
            Debug.Log(String.Format("TestFlightReliability: Survival Chance at Time {0:F2} is {1:f4}", operatingTime, survivalChance));
            float failureRoll = UnityEngine.Random.Range(0f, 1.001f); // we allow for an extremely slim chance of a part failing right out of the game, but it should be damned rare
            if (failureRoll > survivalChance)
            {
                Debug.Log(String.Format("TestFlightReliability: Part has failed with roll of {0:F4}", failureRoll));
                core.TriggerFailure();
            }
        }
    }
}


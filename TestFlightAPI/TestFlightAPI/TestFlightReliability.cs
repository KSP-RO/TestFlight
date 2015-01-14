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

//            stringRepresentation = String.Format("{0},{1:F2},{2:F2}", scope, minReliability, maxReliability);
            stringRepresentation = scope;
            stringRepresentation += "\n" + reliabilityCurve.ToString();
            return stringRepresentation;
        }

//        public static ReliabilityBodyConfig FromString(string s)
//        {
//            ReliabilityBodyConfig bodyConfig = null;
//            string[] sections = s.Split(new char[1] { ',' });
//            if (sections.Length == 3)
//            {
//                bodyConfig = new ReliabilityBodyConfig();
//                bodyConfig.scope = sections[0].ToLower();
//                bodyConfig.minReliability = float.Parse(sections[1]);
//                bodyConfig.maxReliability = float.Parse(sections[2]);
//            }
//
//            return bodyConfig;
//        }

    }

    /// <summary>
    /// This part module determines the part's current reliability and passes that on to the TestFlight core.
    /// </summary>
    public class TestFlightReliabilityBase : PartModule, ITestFlightReliability
    {
        private ITestFlightCore core = null;
        public List<ReliabilityBodyConfig> reliabilityBodies;


        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            foreach (PartModule pm in this.part.Modules)
            {
                core = pm as ITestFlightCore;
                if (core != null)
                    break;
            }
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

        // New API
        // Get the base or static failure rate
        public double GetBaseFailureRate(double flightData)
        {
            return GetBaseFailureRateForScope(core.GetScope());
        }
        public double GetBaseFailureRateForScope(double flightData, String scope)
        {
            if (core == null)
                return 0;

            ReliabilityBodyConfig body = GetConfigForScope(scope);
            if (body == null)
                return 0;

            return (double)body.reliabilityCurve.Evaluate((float)flightData);
        }
        // Get the momentary (IE current dynamic) failure modifier
        // The reliability module should only return its MODIFIER for the current time at the given scope (or current scope if not given).  The Core will calculate the final failure rate.
        public double GetMomentaryFailureModifier()
        {
            // We don't have a momemntary modifier so we return 1, since its a multipler
            return 1;
        }
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

//        [KSPField(isPersistant = true)]
//        public float reliabilityFactor = 3;
//        [KSPField(isPersistant = true)]
//        public float reliabilityMultiplier = 2;
//
//        public List<string> reliabilityBodiesPackedString;

//        public ReliabilityBodyConfig GetReliabilityBody(string scope)
//        {
//            return reliabilityBodies.Find(s => s.scope == scope);
//        }
//
//        public float GetCurrentReliability(TestFlightData flightData)
//        {
//            // Get the flight data for the currently active body and situation
//            double currentFlightData = flightData.flightData;
//            // Determine current situation
//            string scope = flightData.scope;
//            // Determine raw reliability
//            float rawReliability = (float)Math.Sqrt(currentFlightData * reliabilityMultiplier);
//            //float rawReliability = (float)Math.Pow(currentFlightData * reliabilityMultiplier, 1.0 / reliabilityFactor);
//            // Now adjust if needed based on situation
//            ReliabilityBodyConfig body = GetReliabilityBody(scope);
//            if (body != null)
//            {
//                if (rawReliability < body.minReliability)
//                    return body.minReliability;
//                if (rawReliability > body.maxReliability)
//                    return body.maxReliability;
//                return rawReliability;
//            }
//            return rawReliability;
//        }
//
//        public override void OnAwake()
//        {
//            if (reliabilityBodies == null)
//                reliabilityBodies = new List<ReliabilityBodyConfig>();
//            if (reliabilityBodiesPackedString == null)
//                reliabilityBodiesPackedString = new List<string>();
//        }

//        public override void OnStart(StartState state)
//        {
//            // when starting we need to re-load our data from the packed strings
//            // because for some reason KSP/Unity will dump the more complex datastructures from memory
//            if (reliabilityBodies == null || reliabilityBodies.Count == 0)
//            {
//                foreach (string packedString in reliabilityBodiesPackedString)
//                {
//                    ReliabilityBodyConfig reliabilityBody = ReliabilityBodyConfig.FromString(packedString);
//                    reliabilityBodies.Add(reliabilityBody);
//                }
//            }
//            else
//            {
//                Debug.Log("TestFlightReliabilityBase: " + reliabilityBodies.Count + " bodies in memory");
//            }
//        }

    }
}


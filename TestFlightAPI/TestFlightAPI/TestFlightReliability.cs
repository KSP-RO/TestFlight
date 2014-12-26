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
        public float minReliability = 0;
        [KSPField(isPersistant = true)]
        public float maxReliability = 100;

        public void Load(ConfigNode node)
        {
            if (node.HasValue("scope"))
                scope = node.GetValue("scope").ToLower();
            if (node.HasValue("minReliability"))
                minReliability = float.Parse(node.GetValue("minReliability"));
            if (node.HasValue("maxReliability"))
                maxReliability = float.Parse(node.GetValue("maxReliability"));
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("scope", scope.ToLower());
            node.AddValue("minReliability", minReliability);
            node.AddValue("maxReliability", maxReliability);
        }

        public override string ToString()
        {
            string stringRepresentation = "";

            stringRepresentation = String.Format("{0},{1:F2},{2:F2}", scope, minReliability, maxReliability);

            return stringRepresentation;
        }

        public static ReliabilityBodyConfig FromString(string s)
        {
            ReliabilityBodyConfig bodyConfig = null;
            string[] sections = s.Split(new char[1] { ',' });
            if (sections.Length == 3)
            {
                bodyConfig = new ReliabilityBodyConfig();
                bodyConfig.scope = sections[0].ToLower();
                bodyConfig.minReliability = float.Parse(sections[1]);
                bodyConfig.maxReliability = float.Parse(sections[2]);
            }

            return bodyConfig;
        }

    }

    /// <summary>
    /// This part module determines the part's current reliability and passes that on to the TestFlight core.
    /// </summary>
    public class TestFlightReliabilityBase : PartModule, ITestFlightReliability
    {
        //		MODULE
        //		{
        //			name = TestFlight_Reliability
        //				reliabilityFactor = 3
        //				reliabilityMultiplier = 2
        //				RELIABILITY_BODY
        //				{
        //					scope = kerbin_atmosphere
        //					minReliability = 30
        //					maxReliability = 99
        //				}
        //              RELIABILITY_BODY
        //              {
        //                  scope = kerbin_space
        //                  minReliability = 20
        //                  maxReliability = 95
        //              }
        //              RELIABILITY_BODY
        //              {
        //                  scope = none_deep-space
        //                  minReliability = 10
        //                  maxReliability = 90
        //              }
        //		}

        [KSPField(isPersistant = true)]
        public float reliabilityFactor = 3;
        [KSPField(isPersistant = true)]
        public float reliabilityMultiplier = 2;

        public List<ReliabilityBodyConfig> reliabilityBodies;
        public List<string> reliabilityBodiesPackedString;

        public ReliabilityBodyConfig GetReliabilityBody(string scope)
        {
            return reliabilityBodies.Find(s => s.scope == scope);
        }

        public float GetCurrentReliability(TestFlightData flightData)
        {
            // Get the flight data for the currently active body and situation
            float currentFlightData = flightData.flightData;
            // Determine current situation
            string scope = flightData.scope;
            // Determine raw reliability
            float rawReliability = (float)Math.Pow(currentFlightData * reliabilityMultiplier, 1.0 / reliabilityFactor);
            // Now adjust if needed based on situation
            ReliabilityBodyConfig body = GetReliabilityBody(scope);
            if (body != null)
            {
                if (rawReliability < body.minReliability)
                    return body.minReliability;
                if (rawReliability > body.maxReliability)
                    return body.maxReliability;
                return rawReliability;
            }
            return rawReliability;
        }

        public override void OnAwake()
        {
            if (reliabilityBodies == null)
                reliabilityBodies = new List<ReliabilityBodyConfig>();
            if (reliabilityBodiesPackedString == null)
                reliabilityBodiesPackedString = new List<string>();
        }

        public override void OnStart(StartState state)
        {
            // when starting we need to re-load our data from the packed strings
            // because for some reason KSP/Unity will dump the more complex datastructures from memory
            if (reliabilityBodies == null || reliabilityBodies.Count == 0)
            {
                foreach (string packedString in reliabilityBodiesPackedString)
                {
                    ReliabilityBodyConfig reliabilityBody = ReliabilityBodyConfig.FromString(packedString);
                    reliabilityBodies.Add(reliabilityBody);
                }
            }
            else
            {
                Debug.Log("TestFlightReliabilityBase: " + reliabilityBodies.Count + " bodies in memory");
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            foreach (ConfigNode bodyNode in node.GetNodes("RELIABILITY_BODY"))
            {
                ReliabilityBodyConfig reliabilityBody = new ReliabilityBodyConfig();
                reliabilityBody.Load(bodyNode);
                reliabilityBodies.Add(reliabilityBody);
                reliabilityBodiesPackedString.Add(reliabilityBody.ToString());
            }
            base.OnLoad(node);
        }

        public override void OnSave(ConfigNode node)
        {
            foreach (ReliabilityBodyConfig reliabilityBody in reliabilityBodies)
            {
                reliabilityBody.Save(node.AddNode("RELIABILITY_BODY"));
            }
            base.OnLoad(node);
        }
    }
}


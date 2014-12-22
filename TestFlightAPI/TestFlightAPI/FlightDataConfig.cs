using System;
using System.Collections.Generic;


using UnityEngine;

namespace TestFlightAPI
{
    /// <summary>
    /// Represents a distinct scope of Body & Situation that contains flight data and flight time
    /// </summary>
    public class FlightDataBody : IConfigNode
    {
        [KSPField(isPersistant = true)]
        public string scope;
        [KSPField(isPersistant = true)]
        public float flightData;
        [KSPField(isPersistant = true)]
        public int flightTime;

        public void Load(ConfigNode node)
        {
            if (node.HasValue("scope"))
                scope = node.GetValue("scope");
            else
                scope = "NONE";

            if (node.HasValue("flightData"))
                flightData = float.Parse(node.GetValue("flightData"));
            else
                flightData = 0.0f;

            if (node.HasValue("flightTime"))
                flightTime = int.Parse(node.GetValue("flightTime"));
            else
                flightTime = 0;
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("scope", scope);
            node.AddValue("flightData", flightData);
            node.AddValue("flightTime", flightTime);
        }

        public override string ToString()
        {
            string stringRepresentation = "";

            stringRepresentation = String.Format("{0},{1:F4},{2:D}", scope, flightData, flightTime);

            return stringRepresentation;
        }

        public static FlightDataBody FromString(string s)
        {
            FlightDataBody bodyData = null;

            string[] sections = s.Split(new char[1] { ',' });
            if (sections.Length == 3)
            {
                bodyData = new FlightDataBody();
                bodyData.scope = sections[0];
                bodyData.flightData = float.Parse(sections[1]);
                bodyData.flightTime = int.Parse(sections[2]);
            }

            return bodyData;
        }
    }

    public class FlightDataConfig : IConfigNode
    {
        List<FlightDataBody> dataBodies;

        /// <summary>
        /// Returns the ConfigNode containing flight data for the given scope
        /// </summary>
        /// <param name="name">The requested ConfigNode</param>
        /// <param name="name">Name of the situation</param>
        /// <returns></returns>
        public FlightDataBody GetFlightData(string scope)
        {
            if (dataBodies == null)
                return null;

            return dataBodies.Find(s => s.scope == scope);
        }

        /// <summary>
        /// Adds flight data for the given body & situation scope.  Creates new entry if one doesn't yet exist, or updates one if it exists
        /// </summary>
        /// <param name="name">Name of the body as given by Vessel.mainBody.name</param>
        /// <param name="name">Name of the situation</param>
        /// <param name="flightData">Flight data value for the given scope</param>
        /// <param name="flightTime">Flight time for the given scope</param>
        public void AddFlightData(string scope, float flightData, int flightTime)
        {
            if (dataBodies == null)
            {
                dataBodies = new List<FlightDataBody>();
                FlightDataBody bodyData = new FlightDataBody();
                bodyData.scope = scope;
                bodyData.flightData = flightData;
                bodyData.flightTime = flightTime;
                dataBodies.Add(bodyData);
            }
            else
            {
                FlightDataBody bodyData = dataBodies.Find(s => s.scope == scope);
                if (bodyData != null)
                {
                    bodyData.flightData = flightData;
                    bodyData.flightTime = flightTime;
                    return;
                }
                else
                {
                    FlightDataBody body = new FlightDataBody();
                    body.scope = scope;
                    body.flightData = flightData;
                    body.flightTime = flightTime;
                    dataBodies.Add(body);
                }
            }
        }

        public void Load(ConfigNode node)
        {
            if (node.HasNode("bodyData"))
            {
                if (dataBodies == null)
                    dataBodies = new List<FlightDataBody>();
                else
                    dataBodies.Clear();
                foreach (ConfigNode bodyNode in node.GetNodes("bodyData"))
                {
                    FlightDataBody body = new FlightDataBody();
                    body.Load(bodyNode);
                    dataBodies.Add(body);
                }
            }
        }

        public void Save(ConfigNode node)
        {
            if (dataBodies != null)
            {
                foreach (FlightDataBody body in dataBodies)
                {
                    ConfigNode bodyNode = node.AddNode("bodyData");
                    body.Save(bodyNode);
                }
            }
        }
    }
}

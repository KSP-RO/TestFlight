using System;
using System.Collections.Generic;


using UnityEngine;
using KSPPluginFramework;

namespace TestFlightCore
{

    public class FlightDataBody : IConfigNode
    {
        [KSPField(isPersistant = true)]
        public string bodyName;
        [KSPField(isPersistant = true)]
        public float dataAtmosphere;
        [KSPField(isPersistant = true)]
        public float dataSpace;
        [KSPField(isPersistant = true)]
        public int flightTimeAtmosphere;
        [KSPField(isPersistant = true)]
        public int flightTimeSpace;

        public void Load(ConfigNode node)
        {
            if (node.HasValue("bodyName"))
                bodyName = node.GetValue("bodyName");
            else
                bodyName = "DEFAULTBODY";

            if (node.HasValue("dataAtmosphere"))
                dataAtmosphere = float.Parse(node.GetValue("dataAtmosphere"));
            else
                dataAtmosphere = 0.0f;

            if (node.HasValue("dataSpace"))
                dataSpace = float.Parse(node.GetValue("dataSpace"));
            else
                dataSpace = 0.0f;

            if (node.HasValue("flightTimeAtmosphere"))
                flightTimeAtmosphere = int.Parse(node.GetValue("flightTimeAtmosphere"));
            else
                flightTimeAtmosphere = 0;

            if (node.HasValue("flightTimeSpace"))
                flightTimeSpace = int.Parse(node.GetValue("flightTimeSpace"));
            else
                flightTimeSpace = 0;
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("bodyName", bodyName);
            node.AddValue("dataAtmosphere", dataAtmosphere);
            node.AddValue("dataSpace", dataSpace);
            node.AddValue("flightTimeAtmosphere", flightTimeAtmosphere);
            node.AddValue("flightTimeSpace", flightTimeSpace);
        }

        public override string ToString()
        {
            string stringRepresentation = "";

            stringRepresentation = String.Format("{0},{1:F4},{2:F4},{3:D},{4:D}", bodyName, dataSpace, dataAtmosphere, flightTimeAtmosphere, flightTimeSpace);

            return stringRepresentation;
        }

        public static FlightDataBody FromString(string s)
        {
            FlightDataBody bodyData = null;

            string[] sections = s.Split(new char[1] { ',' });
            if (sections.Length == 5)
            {
                bodyData = new FlightDataBody();
                bodyData.bodyName = sections[0];
                bodyData.dataSpace = float.Parse(sections[1]);
                bodyData.dataAtmosphere = float.Parse(sections[2]);
                bodyData.flightTimeAtmosphere = int.Parse(sections[3]);
                bodyData.flightTimeSpace = int.Parse(sections[4]);
            }

            return bodyData;
        }
    }

    public class FlightData : IConfigNode
    {
        [KSPField(isPersistant = true)]
        public float dataDeepSpace;
        [KSPField(isPersistant = true)]
        public int flightTimeDeepSpace;

        List<FlightDataBody> dataBodies;

        /// <summary>
        /// Returns the ConfigNode containing flight data for the given body
        /// </summary>
        /// <param name="name">The requested ConfigNode</param>
        /// <returns></returns>
        public FlightDataBody GetBodyData(string name)
        {
            if (dataBodies == null)
                return null;

            return dataBodies.Find(s => s.bodyName.ToLower() == name.ToLower());
        }

        /// <summary>
        /// Adds flight data for the given body.  Creates new entry if one doesn't yet exist, or updates one if it exists
        /// </summary>
        /// <param name="name">Name of the body as given by Vessel.mainBody.name</param>
        /// <param name="atmosphere">Flight data value for Atmosphere</param>
        /// <param name="space">Flight data value for Space</param>
        public void AddBodyData(string name, float dataAtmosphere, float dataSpace, int timeAtmosphere, int timeSpace)
        {
            if (dataBodies == null)
            {
                dataBodies = new List<FlightDataBody>();
                FlightDataBody body = new FlightDataBody();
                body.bodyName = name;
                body.dataAtmosphere = dataAtmosphere;
                body.dataSpace = dataSpace;
                body.flightTimeAtmosphere = timeAtmosphere;
                body.flightTimeSpace = timeSpace;
                dataBodies.Add(body);
            }
            else
            {
                FlightDataBody body = dataBodies.Find(s => s.bodyName == name);
                if (body != null)
                {
                    body.dataAtmosphere = dataAtmosphere;
                    body.dataSpace = dataSpace;
                    body.flightTimeAtmosphere = timeAtmosphere;
                    body.flightTimeSpace = timeSpace;
                }
                else
                {
                    body = new FlightDataBody();
                    body.bodyName = name;
                    body.dataAtmosphere = dataAtmosphere;
                    body.dataSpace = dataSpace;
                    body.flightTimeAtmosphere = timeAtmosphere;
                    body.flightTimeSpace = timeSpace;
                    dataBodies.Add(body);
                }
            }
        }

        public void Load(ConfigNode node)
        {
            Debug.Log("FlightData Load");
            Debug.Log(node.ToString());
            if (node.HasValue("dataDeepSpace"))
                dataDeepSpace = float.Parse(node.GetValue("dataDeepSpace"));
            else
                dataDeepSpace = 0.0f;

            if (node.HasValue("flightTimeDeepSpace"))
                flightTimeDeepSpace = int.Parse(node.GetValue("flightTimeDeepSpace"));
            else
                flightTimeDeepSpace = 0;

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
            Debug.Log("FlightData Save");
            Debug.Log(node.ToString());
            node.AddValue("dataDeepSpace", dataDeepSpace);
            node.AddValue("flightTimeDeepSpace", flightTimeDeepSpace);
            if (dataBodies != null)
            {
                foreach (FlightDataBody body in dataBodies)
                {
                    ConfigNode bodyNode = node.AddNode("bodyData");
                    body.Save(bodyNode);
                }
            }
            Debug.Log(node.ToString());
        }
    }
}

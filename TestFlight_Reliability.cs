using System;
using System.Collections.Generic;
using UnityEngine;
using KSPPluginFramework;

namespace TestFlight
{

	public class ReliabilityBody : IConfigNode
	{
		[KSPField(isPersistant = true)]
		public string bodyName = "DEFAULT_BODY";
		[KSPField(isPersistant = true)]
		public float minReliabilitySpace = 0;
		[KSPField(isPersistant = true)]
		public float maxReliabilitySpace = 100;
		[KSPField(isPersistant = true)]
		public float minReliabilityAtmosphere = 0;
		[KSPField(isPersistant = true)]
		public float maxReliabilityAtmosphere = 100;

		public void Load(ConfigNode node)
		{
		}

		public void Save(ConfigNode node)
		{
		}

		public override string ToString()
		{
			string stringRepresentation = "";

			stringRepresentation = String.Format ("{0} {1:F2},{2:F2} {3:F2},{4:F2}", bodyName, minReliabilitySpace, maxReliabilitySpace, minReliabilityAtmosphere, maxReliabilityAtmosphere);

			return stringRepresentation;
		}

		public static ReliabilityBody FromString(string s)
		{
			ReliabilityBody bodyConfig = null;
			string[] sections = s.Split(new char[1] {' '});
			if (sections.Length == 3) 
			{
				bodyConfig = new ReliabilityBody ();
				bodyConfig.bodyName = sections [0].ToLower ();
				string reliabilitySpace = sections [1];
				string reliabilityAtmosphere = sections [2];

				string[] spaceSettings = reliabilitySpace.Split (new char[1]{ ',' });
				if (spaceSettings.Length == 2) {
					bodyConfig.minReliabilitySpace = float.Parse(spaceSettings [0]);
					bodyConfig.maxReliabilitySpace = float.Parse(spaceSettings [1]);
				}
				string[] atmosphereSettings = reliabilityAtmosphere.Split (new char[1]{ ',' });
				if (atmosphereSettings.Length == 2) {
					bodyConfig.minReliabilityAtmosphere = float.Parse(atmosphereSettings [0]);
					bodyConfig.maxReliabilityAtmosphere = float.Parse(atmosphereSettings [1]);
				}
			}

			return bodyConfig;
		}

	}

	/// <summary>
	/// This part module handles the reliability side of TestFlight and is responsible for determing if something fails or not
	/// </summary>
	public class TestFlight_Reliability : PartModuleWindow
	{
		//		MODULE
		//		{
		//			name = TestFlight_Reliability
		//				reliabilityFactor = 3
		//				reliabilityMultiplier = 2
		//				minReliabilityDeepSpace = 10
		//				maxReliabilityDeepSpace = 90
		//				RELIABILITY_BODY
		//				{
		//					bodyName = Kerbin
		//					minReliabilityAtmosphere = 30
		//					maxReliabilityAtmosphere = 99
		//					minReliabilitySpace = 20
		//					maxReliabilitySpace = 95
		//				}
		//		}

		[KSPField(isPersistant = true)]
		public float reliabilityFactor = 3;
		[KSPField(isPersistant = true)]
		public float reliabilityMultiplier = 2;

		[KSPField(isPersistant = true)]
		public float minReliabilityDeepSpace = 0;
		[KSPField(isPersistant = true)]
		public float maxReliabilityDeepSpace = 100;

		public List<ReliabilityBody> reliabilityBodies;
		public List<string> reliabilityBodiesPackedString;

		public override void OnAwake()
		{
			if (reliabilityBodies == null)
				reliabilityBodies = new List<ReliabilityBody> ();
			if (reliabilityBodiesPackedString == null)
				reliabilityBodiesPackedString = new List<string> ();
		}

		public override void OnStart(StartState state)
		{
			// when starting we need to re-load our data from the packed strings
			// because for some reason KSP/Unity will dump the more complex datastructures from memory
			reliabilityBodies.Clear ();
			foreach (string packedString in reliabilityBodiesPackedString)
			{
				ReliabilityBody reliabilityBody = ReliabilityBody.FromString(packedString);
				reliabilityBodies.Add(reliabilityBody);
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad (node);
			foreach (ConfigNode bodyNode in node.GetNodes("RELIABILITY_BODY")) {
				ReliabilityBody reliabilityBody = new ReliabilityBody ();
				reliabilityBody.Load (bodyNode);
				reliabilityBodies.Add (reliabilityBody);
				reliabilityBodiesPackedString.Add (reliabilityBody.ToString ());
			}
		}

		public override void OnSave(ConfigNode node)
		{
			foreach (ReliabilityBody reliabilityBody in reliabilityBodies) {
				reliabilityBody.Save (node.AddNode("RELIABILITY_BODY"));
			}
		}

		/// <summary>
		/// KSP:
		///  Per-physx-frame update
		///  Called ONLY when Part is ACTIVE!
		/// 
		/// This is where we will perform the meat of the work, getting the data from the FlightDataRecorder module,
		/// determining reliability, checking for a failure, and then if neecesary determing which failure module
		/// to use and passing off to it
		/// </summary>
		public override void OnFixedUpdate()
		{
			// Find the PartModule instance for our FlightDataRecorder
			PartModule pm = this.part.Modules ["FlightDataRecorder"];
			if (pm != null) {
				FlightDataRecorder dataRecorder = (FlightDataRecorder)pm;
				// Get the flight data for the currently active body and situation
				float currentFlightData = dataRecorder.GetCurrentData ();
			}
		}

		internal override void DrawWindow (int id)
		{

		}
	}
}


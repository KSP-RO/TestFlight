using System;
using System.Collections.Generic;
using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlight
{
    // Method for determing distance from kerbal to part
    // float kerbalDistanceToPart = Vector3.Distance(kerbal.transform.position, targetPart.collider.ClosestPointOnBounds(kerbal.transform.position));

    public class FlightDataRecorder : FlightDataRecorderBase
    {
        public override void OnAwake()
        {
            Debug.Log("FlightDataRecorder: OnAwake()");
            base.OnAwake();
        }
    }
}


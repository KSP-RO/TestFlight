using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using TestFlightAPI;

namespace TestFlight
{

    // Method for determing distance from kerbal to part
    // float kerbalDistanceToPart = Vector3.Distance(kerbal.transform.position, targetPart.collider.ClosestPointOnBounds(kerbal.transform.position));
    public class FlightDataRecorder_Resources : FlightDataRecorderBase
    {
        [KSPField]
        public double emptyThreshold = 0.1;

        public override void OnAwake()
        {
            base.OnAwake();
        }

        public override bool IsRecordingFlightData()
        {
            bool isRecording = false;

            if (!isEnabled)
                return false;

            if (this.part.vessel.situation == Vessel.Situations.PRELAUNCH)
                return false;

            List<PartResource> partResources = this.part.Resources.ToList();
            foreach (PartResource resource in partResources)
            {
                if (resource.amount > emptyThreshold)
                    return true;
            }

            return isRecording;
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSPPluginFramework;

using TestFlightAPI;

namespace TestFlight
{

    // Method for determing distance from kerbal to part
    // float kerbalDistanceToPart = Vector3.Distance(kerbal.transform.position, targetPart.collider.ClosestPointOnBounds(kerbal.transform.position));
    public class FlightDataRecorder_Resources : FlightDataRecorderBase
    {
        [KSPField(isPersistant = true)]
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

            List<PartResource> partResources = this.part.Resources.list;
            foreach (PartResource resource in partResources)
            {
                if (resource.amount > emptyThreshold)
                    return true;
            }

            return isRecording;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("emptyThreshold"))
                emptyThreshold = Double.Parse(node.GetValue("emptyThreshold"));
        }
    }
}


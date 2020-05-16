using System;
using System.Collections.Generic;
using UnityEngine;
using TestFlightAPI;
using TestFlightCore;

namespace TestFlight
{
    /// <summary>
    /// This part module determines the part's current reliability and passes that on to the TestFlight core.
    /// </summary>
    public class TestFlightReliability : TestFlightReliabilityBase
    {
        public override string GetModuleInfo(string configuration, float reliabilityAtTime)
        {
            foreach (var configNode in configs)
            {
                if (!configNode.HasValue("configuration"))
                    continue;

                var nodeConfiguration = configNode.GetValue("configuration");

                if (string.Equals(nodeConfiguration, configuration, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (configNode.HasNode("reliabilityCurve"))
                    {
                        var nodeReliability = new FloatCurve();
                        nodeReliability.Load(configNode.GetNode("reliabilityCurve"));

                        // core is not yet available here
                        float reliabilityMin = TestFlightUtil.FailureRateToReliability(nodeReliability.Evaluate(nodeReliability.minTime), reliabilityAtTime);
                        float reliabilityMax = TestFlightUtil.FailureRateToReliability(nodeReliability.Evaluate(nodeReliability.maxTime), reliabilityAtTime);
                        return $"  Reliability at 0 data: <color=#b1cc00ff>{reliabilityMin:P1}</color>\n  Reliability at max data: <color=#b1cc00ff>{reliabilityMax:p1}</color>";
                    }
                }
            }

            return base.GetModuleInfo(configuration, reliabilityAtTime);
        }

        public override List<string> GetTestFlightInfo(float reliabilityAtTime)
        {
            List<string> infoStrings = new List<string>();

            if (core == null)
            {
                Log("Core is null");
                return infoStrings;
            }
            if (reliabilityCurve == null)
            {
                Log("Curve is null");
                return infoStrings;
            }

            float flightData = TestFlightManagerScenario.Instance.GetFlightDataForPartName(Configuration);
            if (flightData < 0f)
                flightData = 0f;

            double currentFailRate = GetBaseFailureRate(flightData);
            double maxFailRate = GetBaseFailureRate(reliabilityCurve.maxTime);

            double currentReliability = TestFlightUtil.FailureRateToReliability(currentFailRate, reliabilityAtTime);
            double maxReliability = TestFlightUtil.FailureRateToReliability(maxFailRate, reliabilityAtTime);

            string currentMTBF = core.FailureRateToMTBFString(currentFailRate, TestFlightUtil.MTBFUnits.SECONDS, 999);
            string maxMTBF = core.FailureRateToMTBFString(maxFailRate, TestFlightUtil.MTBFUnits.SECONDS, 999);

            infoStrings.Add("<b>Base Reliability</b>");
            infoStrings.Add($"<b>Current Reliability</b>: {currentReliability:P1} at full burn, {currentMTBF} <b>MTBF</b>");
            infoStrings.Add($"<b>Maximum Reliability</b>: {maxReliability:P1} at full burn, {maxMTBF} <b>MTBF</b>");

            return infoStrings;
        }
    }
}


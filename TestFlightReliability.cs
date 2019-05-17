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
        public override string GetModuleInfo()
        {
            if (reliabilityCurve != null)
            {
                // core is not yet available here
                string mtbfMin = TestFlightUtil.FailureRateToMTBFString(GetBaseFailureRate(reliabilityCurve.minTime), TestFlightUtil.MTBFUnits.SECONDS, false, 999);
                string mtbfMax = TestFlightUtil.FailureRateToMTBFString(GetBaseFailureRate(reliabilityCurve.maxTime), TestFlightUtil.MTBFUnits.SECONDS, false, 999);
                return String.Format("MTBF at 0 data: <color=#859900ff>{0}</color>\nMTBF at max data: <color=#859900ff>{1}</color>", mtbfMin, mtbfMax);
            }
            return base.GetModuleInfo();
        }

        public override List<string> GetTestFlightInfo()
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

            infoStrings.Add("<b>Base Reliability</b>");
            infoStrings.Add(String.Format("<b>Current Reliability</b>: {0} <b>MTBF</b>", core.FailureRateToMTBFString(GetBaseFailureRate(flightData), TestFlightUtil.MTBFUnits.SECONDS, 999)));
            infoStrings.Add(String.Format("<b>Maximum Reliability</b>: {0} <b>MTBF</b>", core.FailureRateToMTBFString(GetBaseFailureRate(reliabilityCurve.maxTime), TestFlightUtil.MTBFUnits.SECONDS, 999)));

            return infoStrings;
        }
    }
}


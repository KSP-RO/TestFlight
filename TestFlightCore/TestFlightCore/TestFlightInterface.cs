using System;
using System.Collections.Generic;

using UnityEngine;
using TestFlightAPI;

namespace TestFlightCore
{
    public class TestFlightInterface : MonoBehaviour
    {
        public static bool TestFlightInstalled()
        {
            return true;
        }

        public static bool TestFlightAvailable()
        {
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
                if (TestFlightManagerScenario.Instance != null)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }


        // PART methods
        // These API methods all operate on a specific part, and therefore the first parameter is always the Part to which it should be applied

        /// <summary>
        /// 0 = OK, 1 = Minor Failure, 2 = Failure, 3 = Major Failure, -1 = Could not find TestFlight Core on Part
        /// </summary>
        /// <returns>The part status.</returns>
        public static int GetPartStatus(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            return core.GetPartStatus();
        }
        double AttemptRepair(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            return core.AttemptRepair();
        }
        double GetRepairTime(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            return core.GetRepairTime();
        }

        string GetRequirementsTooltip(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return "";

            return core.GetRequirementsTooltip();
        }
        // Get a proper scope string for use in other parts of the API
        String GetScope();
        String GetScopeForSituation(String situation);
        String GetScopeForSituation(Vessel.Situations situation);
        String GetScopeForSituationAndBody(String situation, String body);
        String GetScopeForSituationAndBody(String situation, CelestialBody body);
        String GetScopeForSituationAndBody(Vessel.Situations situation, String body);
        String GetScopeForSituationAndBody(Vessel.Situations situation, CelestialBody body);
        String PrettyStringForScope(String scope);
        // Get the base or static failure rate
        double GetBaseFailureRate();
        double GetBaseFailureRateForScope(String scope);
        // Get the Reliability Curve for the part
        FloatCurve GetBaseReliabilityCurve();
        FloatCurve GetBaseReliabilityCurveForScope(String scope);
        // Get the momentary (IE current dynamic) failure rates (Can vary per reliability/failure modules)
        // These  methods will let you get a list of all momentary rates or you can get the best (lowest chance of failure)/worst (highest chance of failure) rates
        // Note that the return value is alwasy a dictionary.  The key is the name of the trigger, always in lowercase.  The value is the failure rate.
        // The dictionary will be a single entry in the case of Worst/Best calls, and will be the length of total triggers in the case of askign for All momentary rates.
//        MomentaryFailureRate GetWorstMomentaryFailureRate();
//        MomentaryFailureRate GetBestMomentaryFailureRate();
//        List<MomentaryFailureRate> GetAllMomentaryFailureRates();
//        MomentaryFailureRate GetWorstMomentaryFailureRateForScope(String scope);
//        MomentaryFailureRate GetBestMomentaryFailureRateForScope(String scope);
//        List<MomentaryFailureRate> GetAllMomentaryFailureRatesForScope(String scope);
        double GetMomentaryFailureRateForTrigger(String trigger);
        double GetMomentaryFailureRateForTriggerForScope(String trigger, String scope);
        // The momentary failure rate is tracked per named "trigger" which allows multiple Reliability or FailureTrigger modules to cooperate
        // Returns the total modified failure rate back to the caller for convenience
        double SetTriggerMomentaryFailureModifier(String trigger, double multiplier, PartModule owner);
        double SetTriggerMomentaryFailureModifierForScope(String trigger, double multiplier, PartModule owner, String scope);
        // simply converts the failure rate into a MTBF string.  Convenience method
        // Returned string will be of the format "123.00 units"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will 
        // automaticly be converted into progressively higher units until the returned value is <= maximum
//        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units);
//        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, int maximum);
        // Short version of MTBFString uses a single letter to denote (s)econds, (m)inutes, (h)ours, (d)ays, (y)ears
        // So the returned string will be EF "12.00s" or "0.20d"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will 
        // automaticly be converted into progressively higher units until the returned value is <= maximum
//        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm);
//        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm, int maximum);
        // Simply converts the failure rate to a MTBF number, without any string formatting
//        double FailureRateToMTBF(double failureRate, TestFlightUtil.MTBFUnits units);
        // Get the FlightData or FlightTime for the part
        double GetFlightData();
        double GetFlightDataForScope(String scope);
        double GetFlightTime();
        double GetFlightTimeForScope(String scope);
        // Set the FlightData for FlightTime or the part - this is an absolute set and replaces the previous FlightData/Time
        // This is generally NOT recommended.  Use ModifyFlightData instead so that the Core can ensure your modifications cooperate with others
        // These functions are currently NOT implemented!
        void SetFlightData(double data);
        void SetFlightTime(double seconds);
        void SetFlightDataForScope(double data, String scope);
        void SetFlightTimeForScope(double seconds, String scope);
        // Modify the FlightData or FlightTime for the part
        // The given modifier is multiplied against the current FlightData unless additive is true
        double ModifyFlightData(double modifier);
        double ModifyFlightTime(double modifier);
        double ModifyFlightData(double modifier, bool additive);
        double ModifyFlightTime(double modifier, bool additive);
        double ModifyFlightDataForScope(double modifier, String scope);
        double ModifyFlightTimeForScope(double modifier, String scope);
        double ModifyFlightDataForScope(double modifier, String scope, bool additive);
        double ModifyFlightTimeForScope(double modifier, String scope, bool additive);
        // Returns the total engineer bonus for the current vessel's current crew based on the given part's desired per engineer level bonus
        double GetEngineerDataBonus(double partEngineerBonus);
        // Cause a failure to occur, either a random failure or a specific one
        // If fallbackToRandom is true, then if the specified failure can't be found or can't be triggered, a random failure will be triggered instead
//        ITestFlightFailure TriggerFailure();
//        ITestFlightFailure TriggerNamedFailure(String failureModuleName);
//        ITestFlightFailure TriggerNamedFailure(String failureModuleName, bool fallbackToRandom);
        // Returns the Operational Time or the time, in MET, since the last time the part was fully functional. 
        // If a part is currently in a failure state, return will be -1 and the part should not fail again
        // This counts from mission start time until a failure, at which point it is reset to the time the
        // failure is repaired.  It is important to understand this is NOT the total flight time of the part.
        double GetOperatingTime();


        private static ITestFlightCore GetCore(Part corePart)
        {
            if (corePart == null || corePart.Modules == null)
                return null;

            foreach (PartModule pm in corePart.Modules)
            {
                ITestFlightCore core = pm as ITestFlightCore;
                if (core != null)
                    return core;
            }

            return null;
        }
    }
}


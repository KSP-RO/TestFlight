using System;
using System.Collections.Generic;

using UnityEngine;
using TestFlightAPI;

namespace TestFlightCore
{
    public class TestFlightInterface : MonoBehaviour
    {
        // TODO
        // Move this into the new "Settings" class that needs to be created *AFTER* merging in with master (or else it will be a really nasty merge)
        public static double DeepSpaceThreshold
        {
            get { return 10000000; }
        }


        public static bool TestFlightInstalled()
        {
            return true;
        }

        public static bool TestFlightReady()
        {
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
                if (TestFlightManagerScenario.Instance != null && TestFlightManagerScenario.Instance.isReady)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        // Get a proper scope string for use in other parts of the API
        public static String GetScope()
        {
            if (FlightGlobals.ActiveVessel == null)
                return "none";

            String body = FlightGlobals.ActiveVessel.mainBody.GetName();
            String situation = FlightGlobals.ActiveVessel.situation.ToString();

            return TestFlightInterface.GetScopeForSituationAndBody(situation, body);
        }
        public static String GetScopeForSituation(String situation)
        {
            if (FlightGlobals.ActiveVessel == null)
                return "none";

            String body = FlightGlobals.ActiveVessel.mainBody.GetName();
            return TestFlightInterface.GetScopeForSituationAndBody(situation, body);
        }
        public static String GetScopeForSituation(Vessel.Situations situation)
        {
            if (FlightGlobals.ActiveVessel == null)
                return "none";
            String body = FlightGlobals.ActiveVessel.mainBody.GetName();
            String situationStr = situation.ToString();
            return TestFlightInterface.GetScopeForSituationAndBody(situationStr, body);
        }
        public static String GetScopeForSituationAndBody(String situation, CelestialBody body)
        {
            if (FlightGlobals.ActiveVessel == null)
                return "none";
            String bodyStr = body.GetName();
            return TestFlightInterface.GetScopeForSituationAndBody(situation, bodyStr);
        }
        public static String GetScopeForSituationAndBody(Vessel.Situations situation, String body)
        {
            if (FlightGlobals.ActiveVessel == null)
                return "none";
            String situationStr = situation.ToString();
            return TestFlightInterface.GetScopeForSituationAndBody(situationStr, body);
        }
        public static String GetScopeForSituationAndBody(Vessel.Situations situation, CelestialBody body)
        {
            if (FlightGlobals.ActiveVessel == null)
                return "none";
            String situationStr = situation.ToString();
            String bodyStr = body.GetName();
            return TestFlightInterface.GetScopeForSituationAndBody(situationStr, bodyStr);
        }
        public static String GetScopeForSituationAndBody(String situation, String body)
        {
            if (FlightGlobals.ActiveVessel == null)
                return "none";
            // Determine if we are recording data in SPACE or ATMOSHPHERE
            situation = situation.ToLower().Trim();
            body = body.ToLower().Trim();
            if (situation == "sub_orbital" || situation == "orbiting" || situation == "escaping" || situation == "docked")
            {
                if (FlightGlobals.ActiveVessel.altitude > DeepSpaceThreshold)
                {
                    situation = "deep-space";
                    body = "none";
                }
                else
                {
                    situation = "space";
                }
            }
            else if (situation == "flying" || situation == "landed" || situation == "splashed" || situation == "prelaunch")
            {
                situation = "atmosphere";
            }
            else
            {
                situation = "default";
            }

            return String.Format("{0}_{1}", body.ToLower(), situation.ToLower());
        }
        public static String PrettyStringForScope(String scope)
        {
            string body;
            string situation;
            string[] split = scope.Split(new char[] { '_' });

            // fall out if we have unexpected input
            if (split.Length != 2)
                return scope;

            body = split[0].Substring(0,1).ToUpper() + split[0].Substring(1).ToLower();
            situation = split[1].Substring(0,1).ToUpper() + split[1].Substring(1).ToLower();

            // Try to get the alias for this body if at all possible
            if (TestFlightManagerScenario.Instance != null)
            {
                if (TestFlightManagerScenario.Instance.bodySettings.bodyAliases.ContainsKey(body.ToLower()))
                    body = TestFlightManagerScenario.Instance.bodySettings.bodyAliases[body.ToLower()];
            }

            return body + " " + situation;
        }


        // PART methods
        // These API methods all operate on a specific part, and therefore the first parameter is always the Part to which it should be applied

        public static bool TestFlightAvailable(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return false;
            else
                return true;
        }
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
        public static double AttemptRepair(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            return core.AttemptRepair();
        }
        public static double GetRepairTime(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            return core.GetRepairTime();
        }

        public static string GetRequirementsTooltip(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return "";

            return core.GetRequirementsTooltip();
        }
        // Get the base or static failure rate
        public static double GetBaseFailureRate(Part part)
        {
            return TestFlightInterface.GetBaseFailureRateForScope(part, TestFlightInterface.GetScope());
        }

        public static double GetBaseFailureRateForScope(Part part, String scope)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            return core.GetBaseFailureRateForScope(scope);
        }

        // Get the Reliability Curve for the part
        public static FloatCurve GetBaseReliabilityCurve(Part part)
        {
            return TestFlightInterface.GetBaseReliabilityCurveForScope(part, TestFlightInterface.GetScope());
        }
        public static FloatCurve GetBaseReliabilityCurveForScope(Part part, String scope)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return null;

            return core.GetBaseReliabilityCurveForScope(scope);
        }
        // Get the momentary (IE current dynamic) failure rates (Can vary per reliability/failure modules)
        // These  methods will let you get a list of all momentary rates or you can get the best (lowest chance of failure)/worst (highest chance of failure) rates
        // Note that the return value is alwasy a dictionary.  The key is the name of the trigger, always in lowercase.  The value is the failure rate.
        // The dictionary will be a single entry in the case of Worst/Best calls, and will be the length of total triggers in the case of askign for All momentary rates.
        public static double GetWorstMomentaryFailureRateValue(Part part)
        {
            return TestFlightInterface.GetWorstMomentaryFailureRateForScope(part, TestFlightInterface.GetScope());
        }
        public static double GetBestMomentaryFailureRate(Part part)
        {
            return TestFlightInterface.GetBestMomentaryFailureRateForScope(part, TestFlightInterface.GetScope());
        }
        public static double GetWorstMomentaryFailureRateForScope(Part part, String scope)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            MomentaryFailureRate mfr;
            mfr = core.GetWorstMomentaryFailureRateForScope(scope);
            if (mfr.valid)
                return mfr.failureRate;
            else
                return -1;
        }
        public static double GetBestMomentaryFailureRateForScope(Part part, String scope)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            MomentaryFailureRate mfr = core.GetBestMomentaryFailureRateForScope(scope);
            if (mfr.valid)
                return mfr.failureRate;
            else
                return -1;
        }
        public static double GetMomentaryFailureRateForTrigger(Part part, String trigger)
        {
            return TestFlightInterface.GetMomentaryFailureRateForTriggerForScope(part, trigger, TestFlightInterface.GetScope());
        }
        public static double GetMomentaryFailureRateForTriggerForScope(Part part, String trigger, String scope)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            return core.GetMomentaryFailureRateForTriggerForScope(trigger, scope);
        }
        // The momentary failure rate is tracked per named "trigger" which allows multiple Reliability or FailureTrigger modules to cooperate
        // Returns the total modified failure rate back to the caller for convenience
        public static double SetTriggerMomentaryFailureModifier(Part part, String trigger, double multiplier, PartModule owner)
        {
            return TestFlightInterface.SetTriggerMomentaryFailureModifierForScope(part, trigger, multiplier, owner, TestFlightInterface.GetScope());
        }
        public static double SetTriggerMomentaryFailureModifierForScope(Part part, String trigger, double multiplier, PartModule owner, String scope)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            return core.SetTriggerMomentaryFailureModifierForScope(trigger, multiplier, owner, scope);
        }
        // simply converts the failure rate into a MTBF string.  Convenience method
        // Returned string will be of the format "123.00 units"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will 
        // automaticly be converted into progressively higher units until the returned value is <= maximum
        public static String FailureRateToMTBFString(Part part, double failureRate, int units)
        {
            return TestFlightInterface.FailureRateToMTBFString(part, failureRate, units, false, int.MaxValue);
        }
        public static String FailureRateToMTBFString(Part part, double failureRate, int units, int maximum)
        {
            return TestFlightInterface.FailureRateToMTBFString(part, failureRate, units, false, maximum);
        }
        // Short version of MTBFString uses a single letter to denote (s)econds, (m)inutes, (h)ours, (d)ays, (y)ears
        // So the returned string will be EF "12.00s" or "0.20d"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will 
        // automaticly be converted into progressively higher units until the returned value is <= maximum
        public static String FailureRateToMTBFString(Part part, double failureRate, int units, bool shortForm)
        {
            return TestFlightInterface.FailureRateToMTBFString(part, failureRate, units, shortForm, int.MaxValue);
        }
        public static String FailureRateToMTBFString(Part part, double failureRate, int units, bool shortForm, int maximum)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return "";
            return core.FailureRateToMTBFString(failureRate, (TestFlightUtil.MTBFUnits)units, shortForm, maximum);
        }
        // Simply converts the failure rate to a MTBF number, without any string formatting
        public static double FailureRateToMTBF(Part part, double failureRate, int units)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
            {
                failureRate = Mathf.Max((float)failureRate, (float)TestFlightUtil.MIN_FAILURE_RATE);
                double mtbfSeconds = 1.0 / failureRate;
                return mtbfSeconds;
            }

            return core.FailureRateToMTBF(failureRate, (TestFlightUtil.MTBFUnits)units);
        }
        // Get the FlightData or FlightTime for the part
        public static double GetFlightData(Part part)
        {
            return TestFlightInterface.GetFlightDataForScope(part, TestFlightInterface.GetScope());
        }
        public static double GetFlightDataForScope(Part part, String scope)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 0;

            return core.GetFlightDataForScope(scope);
        }
        public static double GetFlightTime(Part part)
        {
            return TestFlightInterface.GetFlightTimeForScope(part, TestFlightInterface.GetScope());
        }
        public static double GetFlightTimeForScope(Part part, String scope)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 0;

            return core.GetFlightTimeForScope(scope);
        }
        // Set the FlightData for FlightTime or the part - this is an absolute set and replaces the previous FlightData/Time
        // This is generally NOT recommended.  Use ModifyFlightData instead so that the Core can ensure your modifications cooperate with others
        // These functions are currently NOT implemented!
//        public static void SetFlightData(double data)
//        {
//        }
//        public static void SetFlightTime(double seconds)
//        {
//        }
//        public static void SetFlightDataForScope(double data, String scope)
//        {
//        }
//        public static void SetFlightTimeForScope(double seconds, String scope)
//        {
//        }
        // Modify the FlightData or FlightTime for the part
        // The given modifier is multiplied against the current FlightData unless additive is true
        public static double ModifyFlightData(Part part, double modifier)
        {
            return TestFlightInterface.ModifyFlightDataForScope(part, modifier, TestFlightInterface.GetScope(), false);
        }
        public static double ModifyFlightTime(Part part, double modifier)
        {
            return TestFlightInterface.ModifyFlightTimeForScope(part, modifier, TestFlightInterface.GetScope(), false);
        }
        public static double ModifyFlightData(Part part, double modifier, bool additive)
        {
            return TestFlightInterface.ModifyFlightDataForScope(part, modifier, TestFlightInterface.GetScope(), additive);
        }
        public static double ModifyFlightTime(Part part, double modifier, bool additive)
        {
            return TestFlightInterface.ModifyFlightTimeForScope(part, modifier, TestFlightInterface.GetScope(), additive);
        }
        public static double ModifyFlightDataForScope(Part part, double modifier, String scope)
        {
            return TestFlightInterface.ModifyFlightDataForScope(part, modifier, scope, false);
        }
        public static double ModifyFlightTimeForScope(Part part, double modifier, String scope)
        {
            return TestFlightInterface.ModifyFlightTimeForScope(part, modifier, scope, false);
        }
        public static double ModifyFlightDataForScope(Part part, double modifier, String scope, bool additive)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 0;

            return core.ModifyFlightDataForScope(modifier, scope, additive);

        }
        public static double ModifyFlightTimeForScope(Part part, double modifier, String scope, bool additive)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 0;

            return core.ModifyFlightTimeForScope(modifier, scope, additive);
        }
        // Returns the total engineer bonus for the current vessel's current crew based on the given part's desired per engineer level bonus
        public static double GetEngineerDataBonus(Part part, double partEngineerBonus)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 1;

            return core.GetEngineerDataBonus(partEngineerBonus);
        }
        // Cause a failure to occur, either a random failure or a specific one
        // If fallbackToRandom is true, then if the specified failure can't be found or can't be triggered, a random failure will be triggered instead
        public static void TriggerFailure(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return;

            core.TriggerFailure();
        }
        public static void TriggerNamedFailure(Part part, String failureModuleName)
        {
            TestFlightInterface.TriggerNamedFailure(part, failureModuleName, false);
        }
        public static void TriggerNamedFailure(Part part, String failureModuleName, bool fallbackToRandom)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return;
            core.TriggerNamedFailure(failureModuleName, fallbackToRandom);
        }
        public static List<String> GetAvailableFailures(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return null;
            return core.GetAvailableFailures();
        }
        // Returns the Operational Time or the time, in MET, since the last time the part was fully functional. 
        // If a part is currently in a failure state, return will be -1 and the part should not fail again
        // This counts from mission start time until a failure, at which point it is reset to the time the
        // failure is repaired.  It is important to understand this is NOT the total flight time of the part.
        public static double GetOperatingTime(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 0;

            return core.GetOperatingTime();
        }


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


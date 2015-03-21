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
        public static float DeepSpaceThreshold
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

        public static string PartWithMostData()
        {
            if (TestFlightManagerScenario.Instance == null || !TestFlightManagerScenario.Instance.isReady)
                return "";

            return TestFlightManagerScenario.Instance.PartWithMostData();
        }
        public static string PartWithLeastData()
        {
            if (TestFlightManagerScenario.Instance == null || !TestFlightManagerScenario.Instance.isReady)
                return "";

            return TestFlightManagerScenario.Instance.PartWithLeastData();
        }
        public static string PartWithNoData(string partList)
        {
            if (TestFlightManagerScenario.Instance == null || !TestFlightManagerScenario.Instance.isReady)
                return "";

            return TestFlightManagerScenario.Instance.PartWithNoData(partList);
        }
        public static TestFlightPartData GetPartDataForPart(string partName)
        {
            if (TestFlightManagerScenario.Instance == null || !TestFlightManagerScenario.Instance.isReady)
                return null;

            return TestFlightManagerScenario.Instance.GetPartDataForPart(partName);                       
        }
        // Get a proper scope string for use in other parts of the API
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
        public static float AttemptRepair(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            return core.AttemptRepair();
        }
        public static float GetRepairTime(Part part)
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
        public static float GetBaseFailureRate(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            return core.GetBaseFailureRate();
        }
        // Get the Reliability Curve for the part
        public static FloatCurve GetBaseReliabilityCurve(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return null;

            return core.GetBaseReliabilityCurve();
        }
        // Get the momentary (IE current dynamic) failure rates (Can vary per reliability/failure modules)
        // These  methods will let you get a list of all momentary rates or you can get the best (lowest chance of failure)/worst (highest chance of failure) rates
        // Note that the return value is alwasy a dictionary.  The key is the name of the trigger, always in lowercase.  The value is the failure rate.
        // The dictionary will be a single entry in the case of Worst/Best calls, and will be the length of total triggers in the case of askign for All momentary rates.
        public static float GetWorstMomentaryFailureRateValue(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            MomentaryFailureRate mfr;
            mfr = core.GetWorstMomentaryFailureRate();
            if (mfr.valid)
                return mfr.failureRate;
            else
                return -1;
        }
        public static float GetBestMomentaryFailureRate(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            MomentaryFailureRate mfr = core.GetBestMomentaryFailureRate();
            if (mfr.valid)
                return mfr.failureRate;
            else
                return -1;
        }
        public static float GetMomentaryFailureRateForTrigger(Part part, String trigger)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            return core.GetMomentaryFailureRateForTrigger(trigger);
        }
        // The momentary failure rate is tracked per named "trigger" which allows multiple Reliability or FailureTrigger modules to cooperate
        // Returns the total modified failure rate back to the caller for convenience
        public static float SetTriggerMomentaryFailureModifier(Part part, String trigger, float multiplier, PartModule owner)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            return core.SetTriggerMomentaryFailureModifier(trigger, multiplier, owner);
        }
        // simply converts the failure rate into a MTBF string.  Convenience method
        // Returned string will be of the format "123.00 units"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will 
        // automaticly be converted into progressively higher units until the returned value is <= maximum
        public static String FailureRateToMTBFString(Part part, float failureRate, int units)
        {
            return TestFlightInterface.FailureRateToMTBFString(part, failureRate, units, false, int.MaxValue);
        }
        public static String FailureRateToMTBFString(Part part, float failureRate, int units, int maximum)
        {
            return TestFlightInterface.FailureRateToMTBFString(part, failureRate, units, false, maximum);
        }
        // Short version of MTBFString uses a single letter to denote (s)econds, (m)inutes, (h)ours, (d)ays, (y)ears
        // So the returned string will be EF "12.00s" or "0.20d"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will 
        // automaticly be converted into progressively higher units until the returned value is <= maximum
        public static String FailureRateToMTBFString(Part part, float failureRate, int units, bool shortForm)
        {
            return TestFlightInterface.FailureRateToMTBFString(part, failureRate, units, shortForm, int.MaxValue);
        }
        public static String FailureRateToMTBFString(Part part, float failureRate, int units, bool shortForm, int maximum)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return "";
            return core.FailureRateToMTBFString(failureRate, (TestFlightUtil.MTBFUnits)units, shortForm, maximum);
        }
        // Simply converts the failure rate to a MTBF number, without any string formatting
        public static float FailureRateToMTBF(Part part, float failureRate, int units)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
            {
                failureRate = Mathf.Max((float)failureRate, (float)TestFlightUtil.MIN_FAILURE_RATE);
                float mtbfSeconds = 1.0f / failureRate;
                return mtbfSeconds;
            }

            return core.FailureRateToMTBF(failureRate, (TestFlightUtil.MTBFUnits)units);
        }
        // Get the FlightData or FlightTime for the part
        public static float GetFlightData(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 0;

            return core.GetFlightData();
        }
        public static float GetFlightTime(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 0;

            return core.GetFlightTime();
        }
        public static float SetDataRateLimit(Part part, float limit)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 1;

            return core.SetDataRateLimit(limit);
        }
        public static float SetDataCap(Part part, float cap)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return float.MaxValue;

            return core.SetDataCap(cap);
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
        public static float ModifyFlightData(Part part, float modifier)
        {
            return TestFlightInterface.ModifyFlightData(part, modifier, false);
        }
        public static float ModifyFlightData(Part part, float modifier, bool additive)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 0;

            return core.ModifyFlightData(modifier, additive);

        }
        public static float ModifyFlightTime(Part part, float modifier)
        {
            return TestFlightInterface.ModifyFlightTime(part, modifier, false);
        }
        public static float ModifyFlightTime(Part part, float modifier, bool additive)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 0;

            return core.ModifyFlightTime(modifier, additive);
        }
        // Returns the total engineer bonus for the current vessel's current crew based on the given part's desired per engineer level bonus
        public static float GetEngineerDataBonus(Part part, float partEngineerBonus)
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
        public static void EnableFailure(Part part, String failureModuleName)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return;

            core.EnableFailure(failureModuleName);
        }
        public static void DisableFailure(Part part, String failureModuleName)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return;

            core.DisableFailure(failureModuleName);
        }
        // Returns the Operational Time or the time, in MET, since the last time the part was fully functional. 
        // If a part is currently in a failure state, return will be -1 and the part should not fail again
        // This counts from mission start time until a failure, at which point it is reset to the time the
        // failure is repaired.  It is important to understand this is NOT the total flight time of the part.
        public static float GetOperatingTime(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return 0;

            return core.GetOperatingTime();
        }
        public static float ForceRepair(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return -1;

            return core.ForceRepair();
        }
        public static bool IsPartOperating(Part part)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part);
            if (core == null)
                return false;

            return core.IsPartOperating();
        }
        // Methods for accessing the TestFlight modules on a given part

        // Get the active Core Module - can only ever be one.
        public static ITestFlightCore GetCore(Part part)
        {
            return TestFlightUtil.GetCore(part);
        }
        // Get the Data Recorder Module - can only ever be one.
        public static IFlightDataRecorder GetDataRecorder(Part part)
        {
            return TestFlightUtil.GetDataRecorder(part);
        }
        // Get all Reliability Modules - can be more than one.
        public static List<ITestFlightReliability> GetReliabilityModules(Part part)
        {
            return TestFlightUtil.GetReliabilityModules(part);
        }
        // Get all Failure Modules - can be more than one.
        public static List<ITestFlightFailure> GetFailureModules(Part part)
        {
            return TestFlightUtil.GetFailureModules(part);
        }
        public static ITestFlightInterop GetInterop(Part part)
        {
            if (part == null)
            {
                Debug.Log("TestFlightInterface: part is null");
                return null;
            }

            if (part.Modules == null)
            {
                Debug.Log("TestFlightInterface: part.Modules is null");
                return null;
            }

            if (part.Modules.Contains("TestFlightInterop"))
            {
                return part.Modules["TestFlightInterop"] as ITestFlightInterop;
            }
            Debug.Log("TestFlightInterface: Could not find TestFlightInterop module");
            return null;
        }
        public static bool AddInteropValue(Part part, string name, string value, string owner)
        {
            ITestFlightInterop op = TestFlightInterface.GetInterop(part);
            if (op == null)
                return false;

            return op.AddInteropValue(name, value, owner);
        }
        public static bool AddInteropValue(Part part, string name, int value, string owner)
        {
            ITestFlightInterop op = TestFlightInterface.GetInterop(part);
            if (op == null)
                return false;

            return op.AddInteropValue(name, value, owner);
        }
        public static bool AddInteropValue(Part part, string name, float value, string owner)
        {
            ITestFlightInterop op = TestFlightInterface.GetInterop(part);
            if (op == null)
                return false;

            return op.AddInteropValue(name, value, owner);
        }
        public static bool AddInteropValue(Part part, string name, bool value, string owner)
        {
            ITestFlightInterop op = TestFlightInterface.GetInterop(part);
            if (op == null)
                return false;

            return op.AddInteropValue(name, value, owner);
        }

    }
}


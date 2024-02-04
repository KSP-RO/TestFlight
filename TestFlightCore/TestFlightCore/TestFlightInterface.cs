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

        public static bool TestFlightReady()
        {
            if (TestFlightManagerScenario.Instance != null && TestFlightManagerScenario.Instance.isReady)
                return true;
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
        // PART methods
        // These API methods all operate on a specific part, and therefore the first parameter is always the Part to which it should be applied
        // Any given part an have 1..N number of Cores.  Each core represents essentially a distinct component in the part.  For example a Probe part
        // might have a Core for the Probe itsef, plus a Core for an antenna, and another Core for a science experiment.
        // GetActiveCores() will give you a List<string> back of all the active cores on a part.  With that and the part reference, you can
        // then call most of the API functions to interface with that Core.
      
        public static List<string> GetActiveCores(Part part)
        {
            if (part == null || part.Modules == null)
                return null;
            List<string> cores = new List<string>();
            foreach (PartModule pm in part.Modules)
            {
                ITestFlightCore core = pm as ITestFlightCore;
                if (core != null && core.TestFlightEnabled)
                    cores.Add(core.Alias);
            }
            return cores;
        }

        public static bool TestFlightAvailable(Part part)
        {
            ITestFlightCore core = TestFlightUtil.GetCore(part);
            if (core == null)
                return false;
            else
                return true;
        }

        public static void ResetAllFailuresOnVessel(Vessel vessel)
        {
            foreach (Part part in vessel.parts)
            {
                ITestFlightCore core = GetCore(part);
                if (core == null) continue;

                List<ITestFlightFailure> failures = core.GetActiveFailures();
                for (int i = failures.Count - 1; i >= 0; i--)
                {
                    core.ForceRepair(failures[i]);
                }
            }
        }

        public static void ResetAllRunTimesOnVessel(Vessel vessel)
        {
            foreach (Part part in vessel.parts)
            {
                ITestFlightCore core = GetCore(part);
                if (core == null) continue;

                core.ResetRunTime();
            }
        }

        /// <summary>
        /// 0 = OK, 1 = Has failure, -1 = Could not find TestFlight Core on Part
        /// </summary>
        /// <returns>The part status.</returns>
        public static int GetVesselStatus(Vessel vessel)
        {
            int retVal = -1;
            foreach (Part part in vessel.parts)
            {
                int statusForPart;
                ITestFlightCore core = GetCore(part);
                if (core == null)
                    statusForPart = - 1;
                else
                    statusForPart = core.GetPartStatus();
                retVal = Math.Max(retVal, statusForPart);
            }

            return retVal;
        }

        /// <summary>
        /// 0 = OK, 1 = Has failure, -1 = Could not find TestFlight Core on Part
        /// </summary>
        /// <returns>The part status.</returns>
        public static int GetPartStatus(Part part, string alias)
        {
            ITestFlightCore core = GetCore(part, alias);
            if (core == null)
                return -1;

            return core.GetPartStatus();
        }
        // Get the base or static failure rate
        public static double GetBaseFailureRate(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            return core.GetBaseFailureRate();
        }
        // Get the Reliability Curve for the part
        public static FloatCurve GetBaseReliabilityCurve(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return null;

            return core.GetBaseReliabilityCurve();
        }
        // Get the momentary (IE current dynamic) failure rates (Can vary per reliability/failure modules)
        // These  methods will let you get a list of all momentary rates or you can get the best (lowest chance of failure)/worst (highest chance of failure) rates
        // Note that the return value is alwasy a dictionary.  The key is the name of the trigger, always in lowercase.  The value is the failure rate.
        // The dictionary will be a single entry in the case of Worst/Best calls, and will be the length of total triggers in the case of askign for All momentary rates.
        public static double GetWorstMomentaryFailureRateValue(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return -1;

            MomentaryFailureRate mfr;
            mfr = core.GetWorstMomentaryFailureRate();
            if (mfr.valid)
                return mfr.failureRate;
            else
                return -1;
        }
        public static double GetBestMomentaryFailureRate(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return -1;

            MomentaryFailureRate mfr = core.GetBestMomentaryFailureRate();
            if (mfr.valid)
                return mfr.failureRate;
            else
                return -1;
        }
        public static double GetMomentaryFailureRateForTrigger(Part part, string alias, String trigger)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            return core.GetMomentaryFailureRateForTrigger(trigger);
        }
        // The momentary failure rate is tracked per named "trigger" which allows multiple Reliability or FailureTrigger modules to cooperate
        // Returns the total modified failure rate back to the caller for convenience
        public static double SetTriggerMomentaryFailureModifier(Part part, string alias, String trigger, double multiplier, PartModule owner)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return TestFlightUtil.MIN_FAILURE_RATE;

            return core.SetTriggerMomentaryFailureModifier(trigger, multiplier, owner);
        }
        // simply converts the failure rate into a MTBF string.  Convenience method
        // Returned string will be of the format "123.00 units"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will 
        // automaticly be converted into progressively higher units until the returned value is <= maximum
        public static String FailureRateToMTBFString(Part part, string alias, double failureRate, int units)
        {
            return TestFlightInterface.FailureRateToMTBFString(part, alias, failureRate, units, false, int.MaxValue);
        }
        public static String FailureRateToMTBFString(Part part, string alias, double failureRate, int units, int maximum)
        {
            return TestFlightInterface.FailureRateToMTBFString(part, alias, failureRate, units, false, maximum);
        }
        // Short version of MTBFString uses a single letter to denote (s)econds, (m)inutes, (h)ours, (d)ays, (y)ears
        // So the returned string will be EF "12.00s" or "0.20d"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will 
        // automaticly be converted into progressively higher units until the returned value is <= maximum
        public static String FailureRateToMTBFString(Part part, string alias, double failureRate, int units, bool shortForm)
        {
            return TestFlightInterface.FailureRateToMTBFString(part, alias, failureRate, units, shortForm, int.MaxValue);
        }
        public static String FailureRateToMTBFString(Part part, string alias, double failureRate, int units, bool shortForm, int maximum)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return "";
            return core.FailureRateToMTBFString(failureRate, (TestFlightUtil.MTBFUnits)units, shortForm, maximum);
        }
        // Simply converts the failure rate to a MTBF number, without any string formatting
        public static double FailureRateToMTBF(Part part, string alias, double failureRate, int units)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
            {
                failureRate = Math.Max(failureRate, TestFlightUtil.MIN_FAILURE_RATE);
                double mtbfSeconds = 1.0f / failureRate;
                return mtbfSeconds;
            }

            return core.FailureRateToMTBF(failureRate, (TestFlightUtil.MTBFUnits)units);
        }
        // Get the FlightData or FlightTime for the part
        public static float GetFlightData(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return 0;

            return core.GetFlightData();
        }
        public static float GetFlightTime(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return 0;

            return core.GetFlightTime();
        }
        public static float GetMaximumFlightData(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return 0;

            return core.GetMaximumData();
        }
        public static float SetDataRateLimit(Part part, string alias, float limit)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return 1;

            return core.SetDataRateLimit(limit);
        }
        public static float SetDataCap(Part part, string alias, float cap)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return float.MaxValue;

            return core.SetDataCap(cap);
        }
        // Modify the FlightData or FlightTime for the part
        // The given modifier is multiplied against the current FlightData unless additive is true
        public static float ModifyFlightData(Part part, string alias, float modifier)
        {
            return TestFlightInterface.ModifyFlightData(part, alias, modifier, false);
        }
        public static float ModifyFlightData(Part part, string alias, float modifier, bool additive)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return 0;

            return core.ModifyFlightData(modifier, additive);

        }
        public static float ModifyFlightTime(Part part, string alias, float modifier)
        {
            return TestFlightInterface.ModifyFlightTime(part, alias, modifier, false);
        }
        public static float ModifyFlightTime(Part part, string alias, float modifier, bool additive)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return 0;

            return core.ModifyFlightTime(modifier, additive);
        }
        // Returns the total engineer bonus for the current vessel's current crew based on the given part's desired per engineer level bonus
        public static float GetEngineerDataBonus(Part part, string alias, float partEngineerBonus)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return 1;

            return core.GetEngineerDataBonus(partEngineerBonus);
        }
        // Cause a failure to occur, either a random failure or a specific one
        // If fallbackToRandom is true, then if the specified failure can't be found or can't be triggered, a random failure will be triggered instead
        public static void TriggerFailure(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return;

            core.TriggerFailure();
        }
        public static void TriggerNamedFailure(Part part, string alias, String failureModuleName)
        {
            TestFlightInterface.TriggerNamedFailure(part, alias, failureModuleName, false);
        }
        public static void TriggerNamedFailure(Part part, string alias, String failureModuleName, bool fallbackToRandom)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return;
            core.TriggerNamedFailure(failureModuleName, fallbackToRandom);
        }
        public static List<String> GetAvailableFailures(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return null;
            return core.GetAvailableFailures();
        }
        public static void EnableFailure(Part part, string alias, String failureModuleName)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return;

            core.EnableFailure(failureModuleName);
        }
        public static void DisableFailure(Part part, string alias, String failureModuleName)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return;

            core.DisableFailure(failureModuleName);
        }
        // Returns the Operational Time or the time, in MET, since the last time the part was fully functional. 
        // If a part is currently in a failure state, return will be -1 and the part should not fail again
        // This counts from mission start time until a failure, at which point it is reset to the time the
        // failure is repaired.  It is important to understand this is NOT the total flight time of the part.
        public static float GetOperatingTime(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return 0;

            return core.GetOperatingTime();
        }
        public static float ForceRepair(Part part, string alias, ITestFlightFailure failure)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
            if (core == null)
                return -1;

            return core.ForceRepair(failure);
        }
        public static bool IsPartOperating(Part part, string alias)
        {
            ITestFlightCore core = TestFlightInterface.GetCore(part, alias);
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
        // Get the active Core Modules that are bound to a given alias
        public static ITestFlightCore GetCore(Part part, string alias)
        {
            return TestFlightUtil.GetCore(part, alias);
        }
        // Get the Data Recorder Modules that are bound to a given alias
        public static IFlightDataRecorder GetDataRecorder(Part part, string alias)
        {
            return TestFlightUtil.GetDataRecorder(part, alias);
        }
        // Get all Reliability Modules that are bound to a given alias
        public static List<ITestFlightReliability> GetReliabilityModules(Part part, string alias)
        {
            return TestFlightUtil.GetReliabilityModules(part, alias);
        }
        // Get all Failure Modules that are bound to a given alias
        public static List<ITestFlightFailure> GetFailureModules(Part part, string alias)
        {
            return TestFlightUtil.GetFailureModules(part, alias);
        }

        // Interop handling is global on the part, and thus only one interop module should be present regardless of core count
        public static ITestFlightInterop GetInterop(Part part)
        {
            if (part == null)
            {
                return null;
            }

            if (part.Modules == null)
            {
                return null;
            }

            if (part.Modules.Contains("TestFlightInterop"))
            {
                return part.Modules["TestFlightInterop"] as ITestFlightInterop;
            }
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


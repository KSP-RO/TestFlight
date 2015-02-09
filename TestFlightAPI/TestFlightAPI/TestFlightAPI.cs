using System;
using System.Collections.Generic;

using UnityEngine;

namespace TestFlightAPI
{
    public class TestFlightUtil
    {
        public const double MIN_FAILURE_RATE = 0.000001;
        public enum MTBFUnits : int
        {
            SECONDS = 0,
            MINUTES,
            HOURS,
            DAYS,
            YEARS,
            INVALID
        };
        // Methods for accessing the TestFlight modules on a given part

        public static string GetFullPartName(Part part)
        {
            string baseName = part.name;

            if (part.Modules == null)
                return baseName;

            if (part.Modules.Contains("ModuleEngineConfigs"))
            {
                string configurationName = (string)(part.Modules["ModuleEngineConfigs"].GetType().GetField("configuration").GetValue(part.Modules["ModuleEngineConfigs"]));
                return String.Format("{0}|{1}", baseName, configurationName);
            }
            if (part.Modules.Contains("ProceduralShapeCylinder"))
            {
                float diameter = (float)(part.Modules["ProceduralShapeCylinder"].GetType().GetField("diameter").GetValue(part.Modules["ProceduralShapeCylinder"]));
                float length = (float)(part.Modules["ProceduralShapeCylinder"].GetType().GetField("length").GetValue(part.Modules["ProceduralShapeCylinder"]));
                string size = "";
                if (length <= 30f)
                    size = "short";
                else if (length <= 60f)
                    size = "normal";
                else
                    size = "long";
                return String.Format("{0}|{1:F2}-{2}", baseName, diameter, size);
            }

            return baseName;
        }
        public static string GetPartTitle(Part part)
        {
            string baseName = part.partInfo.title;

            if (part.Modules == null)
                return baseName;

            if (part.Modules.Contains("ModuleEngineConfigs"))
            {
                string configurationName = (string)(part.Modules["ModuleEngineConfigs"].GetType().GetField("configuration").GetValue(part.Modules["ModuleEngineConfigs"]));
                return String.Format("{0}", configurationName);
            }
            if (part.Modules.Contains("ProceduralShapeCylinder"))
            {
                float diameter = (float)(part.Modules["ProceduralShapeCylinder"].GetType().GetField("diameter").GetValue(part.Modules["ProceduralShapeCylinder"]));
                float length = (float)(part.Modules["ProceduralShapeCylinder"].GetType().GetField("length").GetValue(part.Modules["ProceduralShapeCylinder"]));
                string size = "";
                if (length <= 30f)
                    size = "short";
                else if (length <= 60f)
                    size = "normal";
                else
                    size = "long";
                return String.Format("Tank {0:F2}-{1}", diameter, size);
            }

            return baseName;
        }
        // Get the active Core Module - can only ever be one.
        public static ITestFlightCore GetCore(Part part)
        {
            if (part == null || part.Modules == null)
                return null;

            foreach (PartModule pm in part.Modules)
            {
                ITestFlightCore core = pm as ITestFlightCore;
                if (core != null && core.TestFlightEnabled)
                    return core;
            }
            return null;
        }
        public static ITestFlightCore GetCore(Part part, string configuration)
        {
            if (part == null || part.Modules == null)
                return null;

            foreach (PartModule pm in part.Modules)
            {
                ITestFlightCore core = pm as ITestFlightCore;
                if (core != null && core.TestFlightEnabled && core.Configuration == configuration)
                    return core;
            }
            return null;
        }
        // Get the Data Recorder Module - can only ever be one.
        public static IFlightDataRecorder GetDataRecorder(Part part)
        {
            if (part == null || part.Modules == null)
                return null;

            foreach (PartModule pm in part.Modules)
            {
                IFlightDataRecorder dataRecorder = pm as IFlightDataRecorder;
                if (dataRecorder != null && dataRecorder.TestFlightEnabled)
                    return dataRecorder;
            }
            return null;
        }
        public static IFlightDataRecorder GetDataRecorder(Part part, string configuration)
        {
            if (part == null || part.Modules == null)
                return null;

            foreach (PartModule pm in part.Modules)
            {
                IFlightDataRecorder dataRecorder = pm as IFlightDataRecorder;
                if (dataRecorder != null && dataRecorder.TestFlightEnabled && dataRecorder.Configuration == configuration)
                    return dataRecorder;
            }
            return null;
        }
        // Get all Reliability Modules - can be more than one.
        public static List<ITestFlightReliability> GetReliabilityModules(Part part)
        {
            List<ITestFlightReliability> reliabilityModules;

            if (part == null || part.Modules == null)
                return null;

            reliabilityModules = new List<ITestFlightReliability>();
            foreach (PartModule pm in part.Modules)
            {
                ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                if (reliabilityModule != null && reliabilityModule.TestFlightEnabled)
                    reliabilityModules.Add(reliabilityModule);
            }

            return reliabilityModules;
        }
        public static List<ITestFlightReliability> GetReliabilityModules(Part part, string configuration)
        {
            List<ITestFlightReliability> reliabilityModules;

            if (part == null || part.Modules == null)
                return null;

            reliabilityModules = new List<ITestFlightReliability>();
            foreach (PartModule pm in part.Modules)
            {
                ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                if (reliabilityModule != null && reliabilityModule.TestFlightEnabled && reliabilityModule.Configuration == configuration)
                    reliabilityModules.Add(reliabilityModule);
            }

            return reliabilityModules;
        }
        // Get all Failure Modules - can be more than one.
        public static List<ITestFlightFailure> GetFailureModules(Part part)
        {
            List<ITestFlightFailure> failureModules;

            if (part == null || part.Modules == null)
                return null;

            failureModules = new List<ITestFlightFailure>();
            foreach (PartModule pm in part.Modules)
            {
                ITestFlightFailure failureModule = pm as ITestFlightFailure;
                if (failureModule != null && failureModule.TestFlightEnabled)
                    failureModules.Add(failureModule);
            }

            return failureModules;
        }
        public static List<ITestFlightFailure> GetFailureModules(Part part, string configuration)
        {
            List<ITestFlightFailure> failureModules;

            if (part == null || part.Modules == null)
                return null;

            failureModules = new List<ITestFlightFailure>();
            foreach (PartModule pm in part.Modules)
            {
                ITestFlightFailure failureModule = pm as ITestFlightFailure;
                if (failureModule != null && failureModule.TestFlightEnabled && failureModule.Configuration == configuration)
                    failureModules.Add(failureModule);
            }

            return failureModules;
        }
        public static void Log(string message, Part loggingPart)
        {
            ITestFlightCore core = TestFlightUtil.GetCore(loggingPart);
            bool debug = false;
            if (core != null)
                debug = core.DebugEnabled;

            if (debug)
            {
                Debug.Log("[TestFlight] " + message);
            }
        }
        public static void Log(string message, bool debug)
        {
            if (debug)
            {
                Debug.Log("[TestFlight] " + message);
            }
        }
    }

	public struct TestFlightData
	{
        // Scope is a combination of the current SOI and the Situation, always lowercase.
        // EG "kerbin_atmosphere" or "mun_space"
        // The one exception is "deep-space" which applies regardless of the SOI if you are deep enough into space
		public string scope;
        // The total accumulated flight data for the part
		public double flightData;
        // The specific flight time, in seconds, of this part instance
		public double flightTime;
	}

	public struct TestFlightFailureDetails
	{
        // Human friendly title to display in the MSD for the failure.  25 characters max
        public string failureTitle;
        // "minor", "failure", or "major" used to indicate the severity of the failure to the player
		public string severity;
        // chances of the failure occuring relative to other failure modules on the same part
        // This should never be anything except:
        // 2 = Rare, 4 = Seldom, 8 = Average, 16 = Often, 32 = Common
		public int weight;
        // "mechanical" indicates a physical failure that requires physical repair
        // "software" indicates a software or electric failure that might be fixed remotely by code
		public string failureType;
	}

    public struct RepairRequirements
    {
        // Player friendly string explaining the requirement.  Should be kept short as is feasible
        public string requirementMessage;
        // Is the requirement currently met?
        public bool requirementMet;
        // Is this an optional requirement that will give a repair bonus if met?
        public bool optionalRequirement;
        // Repair chance bonus (IE 0.05 = +5%) if the optional requirement is met
        public float repairBonus;
    }

    public struct MomentaryFailureModifier
    {
        public String scope;
        public String owner;
        public String triggerName;
        public double modifier;
        // ALWAYS check if valid == true before using the data in this structure!  
        // If valid is false, then the data is empty because a valid data set could not be located
        public bool valid;
    }

    public struct MomentaryFailureRate
    {
        public String scope;
        public String triggerName;
        public double failureRate;
        // ALWAYS check if valid == true before using the data in this structure!  
        // If valid is false, then the data is empty because a valid data set could not be located
        public bool valid;
    }

	public interface IFlightDataRecorder
	{
        bool TestFlightEnabled
        {
            get;
        }
        string Configuration
        {
            get;
            set;
        }
        /// <summary>
        /// Returns whether or not the part is considered to be operating or running.  IE is an engine actually turned on and thrusting?  Is a command pod supplied with electricity and operating?
        /// The point of this is to distinguish between the life time of a part and the operating time of a part, which might be smaller than its total lifetime.
        /// </summary>
        /// <returns><c>true</c> if this instance is part operating; otherwise, <c>false</c>.</returns>
        bool IsPartOperating();
	}

	public interface ITestFlightReliability
	{
        bool TestFlightEnabled
        {
            get;
        }
        string Configuration
        {
            get;
            set;
        }
        // New API
        // Get the base or static failure rate for the given scope
        // !! IMPORTANT: Only ONE Reliability module may return a Base Failure Rate.  Additional modules can exist only to supply Momentary rates
        // If this Reliability module's purpose is to supply Momentary Fialure Rates, then it MUST return 0 when asked for the Base Failure Rate
        // If it dosn't, then the Base Failure Rate of the part will not be correct.
        /// <summary>
        /// Gets the Base Failure Rate (BFR) for the given scope.
        /// </summary>
        /// <returns>The base failure rate for scope.  0 if this module only implements Momentary Failure Rates</returns>
        /// <param name="flightData">The flight data that failure rate should be calculated on.</param>
        /// <param name="scope">Scope.</param>
        double GetBaseFailureRateForScope(double flightData, String scope);

        /// <summary>
        /// Gets the reliability curve for the given scope.
        /// </summary>
        /// <returns>The reliability curve for scope.  MUST return null if the reliability module does not handle Base Failure Rate</returns>
        /// <param name="scope">Scope.</param>
        FloatCurve GetReliabilityCurveForScope(String scope);
	}

	public interface ITestFlightFailure
	{
        bool TestFlightEnabled
        {
            get;
        }
        string Configuration
        {
            get;
            set;
        }
        bool OneShot
        {
            get;
        }
        /// <summary>
        /// Gets the details of the failure encapsulated by this module.  In most cases you can let the base class take care of this unless oyu need to do somethign special
        /// </summary>
        /// <returns>The failure details.</returns>
		TestFlightFailureDetails GetFailureDetails();
		
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        void DoFailure();

        /// <summary>
        /// Gets the repair requirements from the Failure module for display to the user
        /// </summary>
        /// <returns>A List of all repair requirements for attempting repair of the part</returns>
        List<RepairRequirements> GetRepairRequirements();

        /// <summary>
        /// Asks the repair module if all condtions have been met for the player to attempt repair of the failure.  Here the module can verify things such as the conditions (landed, eva, splashed), parts requirements, etc
        /// </summary>
        /// <returns><c>true</c> if this instance can attempt repair; otherwise, <c>false</c>.</returns>
        bool CanAttemptRepair();

        /// <summary>
        /// Gets the seconds until repair is complete
        /// </summary>
        /// <returns>The seconds until repair is complete, <c>0</c> if repair is complete, and <c>-1</c> if something changed the inteerupt the repairs and reapir has stopped with the part still broken.</returns>
        double GetSecondsUntilRepair();

        /// <summary>
		/// Trigger a repair ATTEMPT of the module's failure.  It is the module's responsability to take care of any consumable resources, data transmission, etc required to perform the repair
		/// </summary>
        /// <returns>The seconds until repair is complete, <c>0</c> if repair is completed instantly, and <c>-1</c> if repair failed and the part is still broken.</returns>
        double AttemptRepair();

        /// <summary>
        /// Forces the repair.  This should instantly repair the part, regardless of whether or not a normal repair can be done.  IOW if at all possible the failure should fixed after this call.
        /// This is made available as an API method to allow things like failure simulations.
        /// </summary>
        /// <returns>The seconds until repair is complete, <c>0</c> if repair is completed instantly, and <c>-1</c> if repair failed and the part is still broken.</returns>
        double ForceRepair();
	}

    /// <summary>
    /// This is used internally and should not be implemented by any 3rd party modules
    /// </summary>
    public interface ITestFlightCore
    {
        bool TestFlightEnabled
        {
            get;
        }

        string Configuration
        {
            get;
            set;
        }
        System.Random RandomGenerator
        {
            get;
        }
        bool DebugEnabled
        {
            get;
        }

        /// <summary>
        /// 0 = OK, 1 = Minor Failure, 2 = Failure, 3 = Major Failure
        /// </summary>
        /// <returns>The part status.</returns>
        int GetPartStatus();

        ITestFlightFailure GetFailureModule();

        void InitializeFlightData(List<TestFlightData> allFlightData);

        void HighlightPart(bool doHighlight);

        double GetRepairTime();
        bool IsFailureAcknowledged();
        void AcknowledgeFailure();

        string GetRequirementsTooltip();




        // NEW API
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
        MomentaryFailureRate GetWorstMomentaryFailureRate();
        MomentaryFailureRate GetBestMomentaryFailureRate();
        List<MomentaryFailureRate> GetAllMomentaryFailureRates();
        MomentaryFailureRate GetWorstMomentaryFailureRateForScope(String scope);
        MomentaryFailureRate GetBestMomentaryFailureRateForScope(String scope);
        List<MomentaryFailureRate> GetAllMomentaryFailureRatesForScope(String scope);
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
        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units);
        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, int maximum);
        // Short version of MTBFString uses a single letter to denote (s)econds, (m)inutes, (h)ours, (d)ays, (y)ears
        // So the returned string will be EF "12.00s" or "0.20d"
        // Optionally specify a maximum size for MTBF.  If the given units would return an MTBF larger than maximu, it will 
        // automaticly be converted into progressively higher units until the returned value is <= maximum
        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm);
        String FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm, int maximum);
        // Simply converts the failure rate to a MTBF number, without any string formatting
        double FailureRateToMTBF(double failureRate, TestFlightUtil.MTBFUnits units);
        // Get the FlightData or FlightTime for the part
        double GetFlightData();
        double GetFlightDataForScope(String scope);
        double GetInitialFlightData();
        double GetInitialFlightDataforScope(String scope);
        double GetFlightTime();
        double GetFlightTimeForScope(String scope);
        // Methods to restrict the amount of data accumulated.  Useful for KCT or other "Simulation" mods to use
        double SetDataRateLimit(double limit);
        double SetDataCap(double cap);
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
        ITestFlightFailure TriggerFailure();
        ITestFlightFailure TriggerNamedFailure(String failureModuleName);
        ITestFlightFailure TriggerNamedFailure(String failureModuleName, bool fallbackToRandom);
        // Returns a list of all available failures on the part
        List<String> GetAvailableFailures();
        // Enable a failure so it can be triggered (this is the default state)
        void EnableFailure(String failureModuleName);
        // Disable a failure so it can not be triggered
        void DisableFailure(String failureModuleName);
        // Returns the Operational Time or the time, in MET, since the last time the part was fully functional. 
        // If a part is currently in a failure state, return will be -1 and the part should not fail again
        // This counts from mission start time until a failure, at which point it is reset to the time the
        // failure is repaired.  It is important to understand this is NOT the total flight time of the part.
        double GetOperatingTime();
        /// <summary>
        /// Attempt to repair the part's current failure.  The repair conditions must be met before repair will be attempted.
        /// </summary>
        /// <returns>The amount of seconds until repair is complete, <c>0</c> if repair is completed instantly, or <c>-1</c> if rrepair failed.</returns>
        double AttemptRepair();
        /// <summary>
        /// Forces the repair to be instantly complete, even if the conditions for repair are not met
        /// </summary>
        /// <returns>Time for repairs to finish, <c>0</c> if repair is instantly completed, and <c>-1</c> if repair failed</returns>
        double ForceRepair();
        /// <summary>
        /// Determines whether the part is considered operating or not.
        /// </summary>
        bool IsPartOperating();
    }
}


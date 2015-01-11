using System;
using System.Collections.Generic;

namespace TestFlightAPI
{
	public struct TestFlightData
	{
        // Scope is a combination of the current SOI and the Situation, always lowercase.
        // EG "kerbin_atmosphere" or "mun_space"
        // The one exception is "deep-space" which applies regardless of the SOI if you are deep enough into space
		public string scope;
        // The total accumulated flight data for the part
		public double flightData;
        // The specific flight time, in seconds, of this part instance
		public int flightTime;
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

	public interface IFlightDataRecorder
	{
        /// <summary>
        /// Called frequently by TestFlightCore to ask the module for the current flight data.
        /// The module should only return the currently active scope's data, and should return the most up
        /// to date data it has.
        /// This method should only RETURN the current flight data, not calculate it.  Calculation
        /// should be done in DoFlightUpdate()
        /// </summary>
        /// <returns>The current flight data.</returns>
		TestFlightData GetCurrentFlightData();

        /// <summary>
        /// Initializes the flight data on a newly instanced part from the stored persistent flight data.
        /// This data should only be accepted the first time ever.
        /// </summary>
        /// <param name="allFlightData">A list of all TestFlightData stored for the part, once for each known scope</param>
        void InitializeFlightData(List<TestFlightData> allFlightData);

        /// <summary>
        /// Called to set what is considered "deep-space" altitude
        /// </summary>
        /// <param name="newThreshold">New threshold.</param>
        void SetDeepSpaceThreshold(double newThreshold);

        /// <summary>
        /// Called frequently by TestFlightCore to let the DataRecorder do an update cycle to calulate the current flight data.
        /// This is where the calculation of current data based on paremeters (such as elapsed MET) should occur.
        /// Generally this will be called immediately prior to GetcurrentFlightData() so that the DataRecorder
        /// can be up to date.
        /// </summary>
        /// <param name="missionStartTime">Mission start time in seconds.</param>
        /// <param name="flightDataMultiplier">Global Flight data multiplier.  A user setting which should modify the internal collection rate.  Amount of collected data should be multiplied against this.  Base is 1.0 IE no modification.</param>
        /// <param name="flightDataEngineerMultiplier">Flight data engineer multiplier.  A user setting mutiplier that makes the engineer bonus more or less.  1.0 is base.</param>
        void DoFlightUpdate(double missionStartTime, double flightDataMultiplier, double flightDataEngineerMultiplier);

        /// <summary>
        /// Returns the current data situation, "atmosphere", "space", or "deep-space"
        /// </summary>
        /// <returns>The Situation of the current data scope</returns>
        string GetDataSituation();

        /// <summary>
        /// Returns current SOI
        /// </summary>
        /// <returns>The current SOI for the data scope</returns>
        string GetDataBody();

        /// <summary>
        /// Tell the Recorder to add or subtract an amount of FlightData
        /// </summary>
        /// <param name="modifier">Amount of flight data to add (positive) or subract (negative)</param>
        void ModifyCurrentFlightData(float modifier);
	}

	public interface ITestFlightReliability
	{
        /// <summary>
        /// Gets the current reliability of the part as calculated based on the given flightData
        /// </summary>
        /// <returns>The current reliability.  Can be negative in order to reduce overall reliability from other Reliability modules.</returns>
        /// <param name="flightData">Flight data on which to calculate reliability.</param>
        float GetCurrentReliability(TestFlightData flightData);
	}

	public interface ITestFlightFailure
	{
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
		/// Trigger a repair ATTEMPT of the module's failure.  It is the module's responsability to take care of any consumable resources, data transmission, etc required to perform the repair
		/// </summary>
		/// <returns>Should return true if the failure was repaired, false otherwise</returns>
        bool AttemptRepair();
	}

    /// <summary>
    /// This is used internally and should not be implemented by any 3rd party modules
    /// </summary>
    public interface ITestFlightCore
    {
//        void PerformPreflight();

        /// <summary>
        /// 0 = OK, 1 = Minor Failure, 2 = Failure, 3 = Major Failure
        /// </summary>
        /// <returns>The part status.</returns>
        int GetPartStatus();

        ITestFlightFailure GetFailureModule();

        TestFlightData GetCurrentFlightData();

        double GetCurrentReliability(double globalReliabilityModifier);

        /// <summary>
        /// Does the failure check.
        /// </summary>
        /// <returns><c>true</c>, if part fails, <c>false</c> otherwise.</returns>
        /// <param name="missionStartTime">Mission start time.</param>
        /// <param name="globalReliabilityModifier">Global reliability modifier.</param>
        bool DoFailureCheck(double missionStartTime, double globalReliabilityModifier);

        void DoFlightUpdate(double missionStartTime, double flightDataMultiplier, double flightDataEngineerMultiplier, double globalReliabilityModifier);

        void InitializeFlightData(List<TestFlightData> allFlightData, double globalReliabilityModifier);

        void HighlightPart(bool doHighlight);
        bool AttemptRepair();
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
        // Get the base or static failure rate
        double GetBaseFailureRate();
        double GetBaseFailureRateForScope(String scope);
        // Get the momentary (IE current dynamic) failure rates (Can vary per reliability/failure modules)
        // These  methods will let you get a list of all momentary rates or you can get the best (lowest chance of failure)/worst (highest chance of failure) rates
        double GetWorstMomentaryFailureRate();
        double GetBestMomentaryFailureRate();
        double GetAllMomentaryFailureRates();
        Dictionary<String, double> GetWorstMomentaryFailureRateForScope(String scope);
        Dictionary<String, double> GetBestMomentaryFailureRateForScope(String scope);
        Dictionary<String, double> GetAllMomentaryFailureRatesForScope(String scope);
        // The base failure rate can be modified with a multipler that is applied during flight only
        // Returns the total modified failure rate back to the caller for convenience
        double ModifyBaseFailureRate(double multiplier);
        double ModifyBaseFailureRateForScope(String scope, double multiplier);
        // The momentary failure rate is tracked per Reliability/FailureTrigger module
        // Returns the total modified failure rate back to the caller for convenience
        double ModifyModuleMomentaryFailureRate(String module, double multiplier);
        double ModifyModuleMomentaryFailureRateForScope(String module, String scope, double multiplier);
        // simply converts the failure rate into a MTBF string.  Convenience method
        // Returned string will be of the format "123 units"
        // units should be one of:
        //  seconds, hours, days, months, years, flights, missions
        String FailureRateToMTBFString(double failureRate, String units);
        // Simply converts the failure rate to a MTBF number, without any string formatting
        double FailureRateToMTBF(double failureRate, String units);
        // Get the FlightData or FlightTime for the part
        double GetFlightData();
        double GetFlightDataForScope(String scope);
        int GetFlightTime();
        int GetFlightTimeForScope(String scope);
        // Set the FlightData for FlightTime or the part - this is an absolute set and replaces the previous FlightData
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
        // Cause a failure to occur, either a random failure or a specific one
        // If fallbackToRandom is true, then if the specified failure can't be found or can't be triggered, a random failure will be triggered instead
        ITestFlightFailure TriggerFailure();
        ITestFlightFailure TriggerNamedFailure(String failureModuleName);
        ITestFlightFailure TriggerNamedFailure(String failureModuleName, bool fallbackToRandom);
    }
}


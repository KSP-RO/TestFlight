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

	public interface IFlightDataRecorder
	{
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
        /// <summary>
        /// 0 = OK, 1 = Minor Failure, 2 = Failure, 3 = Major Failure
        /// </summary>
        /// <returns>The part status.</returns>
        int GetPartStatus();

        ITestFlightFailure GetFailureModule();

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
        Dictionary<String, double> GetWorstMomentaryFailureRate();
        Dictionary<String, double> GetBestMomentaryFailureRate();
        Dictionary<String, double> GetAllMomentaryFailureRates();
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
        double GetFlightTime();
        double GetFlightTimeForScope(String scope);
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
        // Returns the total engineer bonus for the current vessel's current crew based on the given part's desired per engineer level bonus
        double GetEngineerDataBonus(double partEngineerBonus);
        // Cause a failure to occur, either a random failure or a specific one
        // If fallbackToRandom is true, then if the specified failure can't be found or can't be triggered, a random failure will be triggered instead
        ITestFlightFailure TriggerFailure();
        ITestFlightFailure TriggerNamedFailure(String failureModuleName);
        ITestFlightFailure TriggerNamedFailure(String failureModuleName, bool fallbackToRandom);
    }
}


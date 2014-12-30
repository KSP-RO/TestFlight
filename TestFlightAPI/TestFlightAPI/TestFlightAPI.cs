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
		public float flightData;
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
        // Repair chance bonus (IE 0.5 = +5%) if the optional requirement is met
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

        string GetRequirementsTooltip();
    }
}


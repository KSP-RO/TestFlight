using System;
using System.Collections.Generic;

namespace TestFlightAPI
{
	public struct TestFlightData
	{
		public string scope;
		public float flightData;
		public int flightTime;
	}

	public struct TestFlightFailureDetails
	{
        // Failure details
		public string severity;
		public int weight;
		public string failureType;
        public bool canBeRepaired;  // Indicates in broad sense, is it ever possible to attempt a repair?
        // Repair details
        public int repairTimeRequired;
        // For mechanical failures
        public bool requiresEVA;
        public bool canBeRepairedInFlight;
        public bool canBeRepairedOnLanded;
        public bool canBeRepairedOnSplashed;
        public int sparePartsRequired;
        // For software failures
        public bool canBeRepairedByRemote;
	}

	public interface IFlightDataRecorder
	{
		TestFlightData GetCurrentFlightData();
        void InitializeFlightData(List<TestFlightData> allFlightData);
        void SetDeepSpaceThreshold(double newThreshold);
        void DoFlightUpdate(double missionStartTime, double flightDataMultiplier, double flightDataEngineerMultiplier);
	}

	public interface ITestFlightReliability
	{
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

        /// <summary>
        /// Does the failure check.
        /// </summary>
        /// <returns><c>true</c>, if part fails, <c>false</c> otherwise.</returns>
        /// <param name="missionStartTime">Mission start time.</param>
        /// <param name="globalReliabilityModifier">Global reliability modifier.</param>
        bool DoFailureCheck(double missionStartTime, double globalReliabilityModifier);

        void DoFlightUpdate(double missionStartTime, double flightDataMultiplier, double flightDataEngineerMultiplier, double globalReliabilityModifier);
    }
}


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
		// 0 = Minor, 1 = Normal, 2 = Major
		public int severity;
		public float chance;
		// 0 = Mechanical, 1 = Software
		public int failureType;
		public bool isRepairable;
	}

	public interface IFlightDataRecorder
	{
		TestFlightData GetCurrentFlightData();
        void InitializeFlightData(List<TestFlightData> allFlightData);
        void SetDeepSpaceThreshold(double newThreshold);
        void DoFlightUpdate(double missionStartTime);
	}

	public interface ITestFlightReliability
	{
		float GetCurrentReliability(TestFlightData flightData);
	}

	public interface ITestFlightFailure
	{
		TestFlightFailureDetails GetFailureDetails();
		/// <summary>
		/// Triggers the module's failure routine.  Module should return based on the status of that failure.
		/// </summary>
		/// <returns>The failure status: 0 = Failed properly, 1 = Failed properly but failure can not be repaired (even if original failure details say it can), -1 = An error orccured in the module</returns>
		int DoFailure();
		/// <summary>
		/// Trigger a repair ATTEMPT of the moduel's failure.  Module should return based on the status of the repair
		/// </summary>
		/// <returns>The repair status: -1 = an error occured in the module.  0 = Failure repaired. 1 = Failure not repaired.</returns>
		int DoRepair();
	}

    /// <summary>
    /// This is used internally and should not be implemented by any modules
    /// </summary>
    public interface ITestFlightCore
    {
//        void PerformPreflight();
//        /// <summary>
        /// 0 = OK, 1 = Minor Failure, 2 = Failure, 3 = Major Failure
        /// </summary>
        /// <returns>The part status.</returns>
//        int GetPartStatus();
//        string GetFailureModule();
        TestFlightData GetCurrentFlightData();
        void DoFlightUpdate(double missionStartTime);
    }
}


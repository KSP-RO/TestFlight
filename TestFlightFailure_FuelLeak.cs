using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_FuelLeak : TestFlightFailureBase
    {
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            Debug.Log("TestFlightFailure_FuelLeak: Failing part");
            this.part.RequestResource("LiquidFuel", 50);
            StartCoroutine("LeakFuel");
        }

        internal IEnumerator LeakFuel()
        {
            while (true)
            {
                this.part.RequestResource("LiquidFuel", 0.5);
                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// Asks the repair module if all condtions have been met for the player to attempt repair of the failure.  Here the module can verify things such as the conditions (landed, eva, splashed), parts requirements, etc
        /// </summary>
        /// <returns><c>true</c> if this instance can attempt repair; otherwise, <c>false</c>.</returns>
        public override bool CanAttemptRepair()
        {
            return true;
        }

        /// <summary>
        /// Trigger a repair ATTEMPT of the module's failure.  It is the module's responsability to take care of any consumable resources, data transmission, etc required to perform the repair
        /// </summary>
        /// <returns>Should return true if the failure was repaired, false otherwise</returns>
        public override bool AttemptRepair()
        {
            StopCoroutine("LeakFuel");
            return true;
        }
    }
}


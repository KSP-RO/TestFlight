using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailure_ResourceLeak : TestFlightFailureBase
    {
        [KSPField(isPersistant = true)]
        public string resourceToLeak = "random";
        [KSPField(isPersistant = true)]
        public float initialAmount = 10f;
        [KSPField(isPersistant = true)]
        public float perSecondAmount = 0.1f;

        private string leakingResource;
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            Debug.Log("TestFlightFailure_ResourceLeak: Failing part");
            if (resourceToLeak.ToLower() == "random")
            {
                List<PartResource> allResources = this.part.Resources.list;
                int randomResource = UnityEngine.Random.Range(0, allResources.Count());
                leakingResource = allResources[randomResource].resourceName;
            }
            else
                leakingResource = resourceToLeak;

            this.part.RequestResource(leakingResource, initialAmount);
            StartCoroutine("LeakResource");
        }

        internal IEnumerator LeakResource()
        {
            while (true)
            {
                this.part.RequestResource(leakingResource, perSecondAmount);
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
            StopCoroutine("LeakResource");
            return true;
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("resourceToLeak"))
                resourceToLeak = node.GetValue("resourceToLeak");
            else
                resourceToLeak = "random";
            if (node.HasValue("initialAmount"))
                initialAmount = float.Parse(node.GetValue("initialAmount"));
            if (node.HasValue("perSecondAmount"))
                perSecondAmount = float.Parse(node.GetValue("perSecondAmount"));
        }
    }
}


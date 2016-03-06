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
        public string initialAmount = "10";
        [KSPField(isPersistant = true)]
        public string perSecondAmount = "0.1";
        [KSPField(isPersistant = true)]
        public bool calculatePerTick = false;

        private string leakingResource = "";
        private int leakingResourceID = 0;
        private float _initialAmount = 10f;
        private float _perSecondAmount = 0.1f;

        [KSPField(isPersistant = true)]
        public bool isLeaking = false;

        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            base.DoFailure();
            if (resourceToLeak.ToLower() == "random")
            {
                List<PartResource> allResources = this.part.Resources.list;
                int randomResource = UnityEngine.Random.Range(0, allResources.Count());
                leakingResource = allResources[randomResource].resourceName;
                leakingResourceID = allResources[randomResource].info.id;
            }
            else
            {
                List<PartResource> resources = this.part.Resources.list.Where(n => n.resourceName == resourceToLeak).ToList();
                if (resources != null && resources.Count > 0)
                {
                    PartResource resource = resources[0];
                    leakingResource = resourceToLeak;
                    leakingResourceID = resource.info.id;
                }
            }

            if (!String.IsNullOrEmpty(leakingResource))
            {
                ParseResourceValues();
                isLeaking = true;
            }
        }

        public void FixedUpdate()
        {
            if(HighLogic.LoadedSceneIsFlight && isLeaking)
            {
                if (!String.IsNullOrEmpty(leakingResource))
                {
                    if (calculatePerTick)
                        ParseResourceValues();
                    this.part.RequestResource(leakingResourceID, _perSecondAmount * TimeWarp.fixedDeltaTime, ResourceFlowMode.NO_FLOW);
                }
            }
        }

        public override float DoRepair()
        {
            base.DoRepair();
            isLeaking = false;

            return 0;
        }

        private float ParseValue(string rawValue)
        {
            float parsedValue = 0f;
            int index = rawValue.IndexOf("%");
            string trimmedValue = "";
            double calculateFromAmount = 0;

            rawValue = rawValue.ToLowerInvariant();

            if (index > 0)
            {
                if (rawValue.EndsWith("%t"))
                {
                    trimmedValue = rawValue.Substring(0, index);
                    if (!float.TryParse(trimmedValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"), out parsedValue))
                        parsedValue = 0f;
                    // Calculate the % value based on the total capacity of the tank
                    calculateFromAmount = this.part.Resources.Get(leakingResourceID).maxAmount;
                    Log(String.Format("Calculating leak amount from maxAmount: {0:F2}", calculateFromAmount));
                }
                else if (rawValue.EndsWith("%c"))
                {
                    trimmedValue = rawValue.Substring(0, index);
                    if (!float.TryParse(trimmedValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"), out parsedValue))
                        parsedValue = 0f;
                    // Calculate the % value based on the current resource level of the tank
                    calculateFromAmount = this.part.Resources.Get(leakingResourceID).amount;
                    Log(String.Format("Calculating leak amount from current amount: {0:F2}", calculateFromAmount));
                }
                Log(String.Format("Base value was parsed as: {0:F2}", parsedValue));
                parsedValue = parsedValue * (float)calculateFromAmount;
                Log(String.Format("Calculated leak: {0:F2}", parsedValue));
            }
            else
            {
                if (!float.TryParse(rawValue, out parsedValue))
                    parsedValue = 0f;
            }

            return parsedValue;
        }

        private void ParseResourceValues()
        {
            _initialAmount = ParseValue(initialAmount);
            _perSecondAmount = ParseValue(perSecondAmount);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("resourceToLeak"))
                resourceToLeak = node.GetValue("resourceToLeak");
            else
                resourceToLeak = "random";
            if (node.HasValue("initialAmount"))
                initialAmount = node.GetValue("initialAmount");
            if (node.HasValue("perSecondAmount"))
                perSecondAmount = node.GetValue("perSecondAmount");
            if (node.HasValue("calculatePerTick"))
                calculatePerTick = bool.Parse(node.GetValue("calculatePerTick"));
        }
    }
}


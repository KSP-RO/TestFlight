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
        [KSPField]
        public string resourceToLeak = "random";
        [KSPField]
        public string initialAmount = "10";
        [KSPField]
        public string perSecondAmount = "0.1";
        [KSPField]
        public bool calculatePerTick = false;

        [KSPField(isPersistant = true)]
        public bool isLeaking = false;

        private List<ResourceLeak> leaks;
        private float _initialAmount, _perSecondAmount;

        public class ResourceLeak : IConfigNode
        {
            public int id = 0;
            public double amount;
            public double initialAmount;

            public void Load(ConfigNode node)
            {
                id = int.Parse(node.GetValue("id"));
                amount = double.Parse(node.GetValue("amount"));
                initialAmount = 0d; // if we're loading, the initial leak has occurred.
            }

            public void Save(ConfigNode node)
            {
                node.AddValue("id", id);
                node.AddValue("amount", amount.ToString("G17"));
            }

            public ResourceLeak(int newId, double newAmount, double newInit)
            {
                id = newId;
                amount = newAmount;
                initialAmount = newInit;
            }

            public ResourceLeak(ConfigNode node)
            {
                Load(node);
            }
        }

        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            base.DoFailure();
            if (resourceToLeak.ToLower() == "all")
            {
                foreach (PartResource r in this.part.Resources)
                {
                    int resId = r.info.id;
                    ParseResourceValues(resId);
                    leaks.Add(new ResourceLeak(resId, _perSecondAmount, _initialAmount));
                }
            }
            else
            {
                int resId = 0;
                bool found = false;
                if (resourceToLeak.ToLower() == "random")
                {
                    if (part.Resources.Count > 0)
                    {
                        List<PartResource> allResources = this.part.Resources.list;
                        int randomResource = UnityEngine.Random.Range(0, allResources.Count());
                        resId = allResources[randomResource].info.id;
                        found = true;
                    }
                }
                else
                {
                    List<PartResource> resources = this.part.Resources.list.Where(n => n.resourceName == resourceToLeak).ToList();
                    if (resources != null && resources.Count > 0)
                    {
                        resId = resources[0].info.id;
                        found = true;
                    }
                }
                if (found)
                {
                    ParseResourceValues(resId);
                    leaks.Add(new ResourceLeak(resId, _perSecondAmount, _initialAmount));
                }
            }
            if (leaks.Count > 0)
            {
                isLeaking = true;
                foreach (ResourceLeak leak in leaks)
                    this.part.RequestResource(leak.id, leak.initialAmount, ResourceFlowMode.NO_FLOW);
            }
        }

        public void FixedUpdate()
        {
            if(HighLogic.LoadedSceneIsFlight && isLeaking)
            {
                ResourceLeak leak;
                for(int i = leaks.Count - 1; i >= 0; --i)
                {
                    leak = leaks[i];
                    if (calculatePerTick)
                    {
                        leak.amount = ParseValue(perSecondAmount, leak.id);
                    }
                    this.part.RequestResource(leak.id, _perSecondAmount * TimeWarp.fixedDeltaTime, ResourceFlowMode.NO_FLOW);
                }
            }
        }

        public override float DoRepair()
        {
            base.DoRepair();
            isLeaking = false;

            return 0;
        }

        private float ParseValue(string rawValue, int leakingResourceID)
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

        private void ParseResourceValues(int resID)
        {
            _initialAmount = ParseValue(initialAmount, resID);
            _perSecondAmount = ParseValue(perSecondAmount, resID);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasNode("LEAK"))
            {
                leaks.Clear();
                foreach (ConfigNode n in node.GetNodes("LEAK"))
                    leaks.Add(new ResourceLeak(n));
            }
        }

        public override void OnSave(ConfigNode node)
        {
            if (leaks.Count > 0)
            {
                foreach (ResourceLeak leak in leaks)
                {
                    ConfigNode n = node.AddNode("LEAK");
                    leak.Save(n);
                }
            }
        }

        public override void OnAwake()
        {
            leaks = new List<ResourceLeak>();
        }
    }
}


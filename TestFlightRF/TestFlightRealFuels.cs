using System;
using System.Collections.Generic;
using UnityEngine;
using RealFuels;
using TestFlight;

namespace TestFlightRF
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class TestFlightRealFuels : MonoBehaviour
    {
        protected Dictionary<string, float> burnTimes = null;

        public void Start()
        {
            burnTimes = new Dictionary<string, float>();
            foreach (AvailablePart part in PartLoader.LoadedPartsList)
            {
                // cache up the burn times first
                List<TestFlightReliability_EngineCycle> engineCycles = new List<TestFlightReliability_EngineCycle>();
                engineCycles.AddRange(part.partPrefab.GetComponents<TestFlightReliability_EngineCycle>());
                if (engineCycles.Count <= 0)
                    return;
                
                foreach (TestFlightReliability_EngineCycle engineCycle in engineCycles)
                {
                    if (engineCycle.engineConfig != "")
                    {
                        burnTimes[engineCycle.engineConfig] = engineCycle.ratedBurnTime;
                    }
                }
                // now add that info to the RF configs
                List<ModuleEngineConfigs> allConfigs = new List<ModuleEngineConfigs>();
                allConfigs.AddRange(part.partPrefab.GetComponents<ModuleEngineConfigs>());
                if (allConfigs.Count <= 0)
                    return;
                
                foreach (ModuleEngineConfigs mec in allConfigs)
                {
                    List<ConfigNode> configs = mec.configs;
                    foreach (ConfigNode node in configs)
                    {
                        if (node.HasValue("name"))
                        {
                            string configName = node.GetValue("name");
                            if (burnTimes.ContainsKey(configName))
                            {
                                if (node.HasValue("configDescription"))
                                {
                                    string description = node.GetValue("configDescription");
                                    description += String.Format("\nRated Burn Time {0:F2}", burnTimes[configName]);
                                    node.AddValue("configDescription", description);
                                }
                                else
                                {
                                    node.AddValue("configDescription", String.Format("\nRated Burn Time {0:F2}", burnTimes[configName]));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}


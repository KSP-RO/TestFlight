using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealFuels;
using TestFlight;
using TestFlightAPI;
using TestFlightCore;

namespace TestFlightRF
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class TestFlightRealFuels : MonoBehaviour
    {
        protected Dictionary<string, float> burnTimes = null;
        protected TestFlightManagerScenario tfScenario = null;

        IEnumerator Setup()
        {
            if (!PartLoader.Instance.IsReady() || PartResourceLibrary.Instance == null)
            {
                yield return null;
            }
            Startup();
        }

        public void Startup()
        {
            Debug.Log("[TestFlightRF] Injecting burn times into RF");
            burnTimes = new Dictionary<string, float>();
            Debug.Log(String.Format("Processing {0} parts in LoadedPartsList", PartLoader.LoadedPartsList.Count));
            foreach (AvailablePart part in PartLoader.LoadedPartsList)
            {
//                Debug.Log("URL: " + part.partUrl);
//                Debug.Log("Name: " + part.name);
//                Debug.Log("Prefab Name: " + part.partPrefab.partName);
                // cache up the burn times first
                List<ITestFlightReliability> engineCycles = new List<ITestFlightReliability>();
//                Debug.Log("Scanning prefab modules");
//                Debug.Log(String.Format("Part Prefab has {0} modules", part.partPrefab.Modules.Count));
                burnTimes.Clear();
                foreach (PartModule pm in part.partPrefab.Modules)
                {
                    ITestFlightReliability reliabilityModule = pm as ITestFlightReliability;
                    if (reliabilityModule != null)
                        engineCycles.Add(reliabilityModule);
                }
                Debug.Log(String.Format("Collected {0} engineCycles to inject", burnTimes.Count));
                if (engineCycles.Count <= 0)
                    continue;

                foreach (ITestFlightReliability rm in engineCycles)
                {
                    TestFlightReliability_EngineCycle engineCycle = rm as TestFlightReliability_EngineCycle;
                    if (engineCycle != null)
                    {
                        if (engineCycle.engineConfig != "")
                        {
                            burnTimes[engineCycle.engineConfig] = engineCycle.ratedBurnTime;
                        }
                    }
                }
                // now add that info to the RF configs
                List<ModuleEngineConfigs> allConfigs = new List<ModuleEngineConfigs>();
                allConfigs.AddRange(part.partPrefab.Modules.GetModules<ModuleEngineConfigs>());
                Debug.Log(String.Format("Found {0} RF configs", allConfigs.Count));
                if (allConfigs.Count <= 0)
                    continue;

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
                                Debug.Log(String.Format("Injecting into config named {0}", configName));
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

        public void Start()
        {
            StartCoroutine("Setup");
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using KSP.UI.Screens;
using TestFlightAPI;

namespace TestFlightCore
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class TestFlightRnD : MonoBehaviour
    {
        Dictionary<string, int> baseCost = null;

        public void Start()
        {
            RDController.OnRDTreeSpawn.Add(OnTreeSpawn);
            DontDestroyOnLoad(this);
        }

        public void OnTreeSpawn(RDController controller)
        {
            if (TestFlightManagerScenario.Instance == null || controller.nodes == null)
            {
                return;
            }
            List<RDNode> nodes = controller.nodes;
            if (this.baseCost == null)
            {
                baseCost = new Dictionary<string, int>();
                for (int i = 0; i < nodes.Count; i++)
                {
                    RDNode node = nodes[i];
                    if (node != null && node.tech != null)
                    {
                        baseCost.Add(nodes[i].tech.techID, nodes[i].tech.scienceCost);
                    }
                }
            }
            for (int n = 0; n < nodes.Count; n++)
            {
                RDNode node = nodes[n];
                if (node != null && node.tech != null && !node.IsResearched && node.tech.partsAssigned != null)
                {
                    float discount = 0f;
                    List<AvailablePart> parts = node.tech.partsAssigned;
                    for (int p = 0; p < parts.Count; p++)
                    {
                        if (parts[p] != null)
                        {
                            Part prefab = parts[p].partPrefab;
                            TestFlightPartData partData = TestFlightManagerScenario.Instance.GetPartDataForPart(parts[p].name);
                            if (partData != null && prefab != null)
                            {
                                TestFlightCore core = (TestFlightCore)prefab.Modules.OfType<TestFlightCore>().FirstOrDefault();
                                float flightData = partData.flightData;
                                if (core != null && flightData > core.startFlightData)
                                {
                                    discount += (int)(((flightData - core.startFlightData) / (core.maxData - core.startFlightData)) * core.scienceDataValue);
                                }
                            }
                        }
                    }
                    if (discount > 0)
                    {
                        node.tech.scienceCost = (int)Math.Max(0, baseCost[node.tech.techID] - discount);
                    }
                }
            }
        }
    }
}
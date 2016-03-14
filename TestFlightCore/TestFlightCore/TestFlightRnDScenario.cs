using System;
using TestFlightAPI;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TestFlightCore
{
    public class TestFlightRnDTeam
    {
        public string Name { get; private set; }
        public float Points { get; set; }
        public float CostFactor { get; set; }
        public float MaxData { get; set; }
        public float PartRnDRate { get; set; }
        public float PartRnDCost { get; set; }
        public float Cost
        {
            get
            {
                return Points * CostFactor;
            }
        }

        public string PartInResearch { get; set; }

        protected float lastUpdatedTime = 0f;

        internal void Log(string message)
        {
            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = "TestFlightRnDTeam: " + message;
            TestFlightUtil.Log(message, debug);
        }

        public TestFlightRnDTeam(string name, float points, float costFactor)
        {
            Name = name;
            Points = points;
            CostFactor = costFactor;
            PartInResearch = "";
        }

        /// <summary>
        /// Updates the research on the team's current part
        /// </summary>
        /// <returns>The amount of du added to the part this update.</returns>
        public float UpdateResearch(float currentPartData)
        {
            if (PartInResearch != "")
            {
                float currentUTC = (float)Planetarium.GetUniversalTime();
                float timeInTick = currentUTC - lastUpdatedTime;
                float normalizedTime = timeInTick / 86400f;
                Log("Time in tick " + timeInTick + ", normalized time " + normalizedTime);
                float pointsForTick = Points * normalizedTime * PartRnDRate;
                pointsForTick = Mathf.Min(pointsForTick, MaxData - currentPartData);
                float costForTick = Cost * normalizedTime * -1.0f * PartRnDCost;
                Log("Points " + pointsForTick + ", Cost " + costForTick);
                lastUpdatedTime = currentUTC;
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    CurrencyModifierQuery query = CurrencyModifierQuery.RunQuery(TransactionReasons.RnDPartPurchase, costForTick, 0f, 0f);
                    float modifiedFunds = query.GetEffectDelta(Currency.Funds);
                    Log("Modified cost " + modifiedFunds);
                    if (modifiedFunds * -1 > Funding.Instance.Funds)
                    {
                        Log("Subtracting cost...");
                        Funding.Instance.AddFunds(modifiedFunds, TransactionReasons.RnDPartPurchase);
                        return pointsForTick;
                    }
                    else
                    {
                        return 0f;
                    }
                }
                else
                    return pointsForTick;
            }
            return 0f;
        }
    }

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, 
        new GameScenes[]
    { 
        GameScenes.FLIGHT,
        GameScenes.EDITOR,
        GameScenes.SPACECENTER,
        GameScenes.TRACKSTATION
    }
    )]
    public class TestFlightRnDScenario : ScenarioModule
    {
        public static TestFlightRnDScenario Instance { get; private set; }
        public bool isReady = false;

        protected TestFlightManagerScenario tfScenario = null;
        protected List<TestFlightRnDTeam> availableTeams = null;
        protected Dictionary<string, TestFlightRnDTeam> activeTeams = null;

        [KSPField(isPersistant=true)]
        protected float lastUpdateTime = 0f;

        protected double updateFrequency = 86400d;

        internal void Log(string message)
        {
            bool debug = TestFlightManagerScenario.Instance.userSettings.debugLog;
            message = "TestFlightRnDScenario: " + message;
            TestFlightUtil.Log(message, debug);
        }

        public void Start()
        {
            Log("RnD Start");
            StartCoroutine("ConnectToScenario");
        }

        public void Update()
        {
            float currentTime = (float)Planetarium.GetUniversalTime();
            List<string> teamsToStop = new List<string>();
            Log("Update.  Current Time " + currentTime + ", Last Update " + lastUpdateTime);
            if (currentTime - lastUpdateTime >= updateFrequency)
            {
                lastUpdateTime = currentTime;
                foreach (KeyValuePair<string, TestFlightRnDTeam> entry in activeTeams)
                {
                    if (entry.Value.PartInResearch != "")
                    {
                        float partCurrentData = tfScenario.GetFlightDataForPartName(entry.Value.PartInResearch);
                        if (partCurrentData >= entry.Value.MaxData)
                        {
                            Log("Part " + entry.Value.PartInResearch + " has reached maximum RnD data.  Removing research automatically");
                            teamsToStop.Add(entry.Key);
                        }
                        else
                        {
                            float partData = entry.Value.UpdateResearch(partCurrentData);
                            Log("Research tick for part " + entry.Value.PartInResearch + " yielded " + partData + "du");
                            if (partData > 0)
                            {
                                TestFlightManagerScenario.Instance.AddFlightDataForPartName(entry.Value.PartInResearch, partData);
                            }
                        }
                    }
                }
                if (teamsToStop.Count > 0)
                {
                    foreach (string team in teamsToStop)
                    {
                        activeTeams.Remove(team);
                    }
                }
            }
        }

        IEnumerator ConnectToScenario()
        {
            while (TestFlightManagerScenario.Instance == null)
            {
                yield return null;
            }

            tfScenario = TestFlightManagerScenario.Instance;
            while (!tfScenario.isReady)
            {
                yield return null;
            }
            Startup();
        }

        public void Startup()
        {
            CreateTeams();
            isReady = true;
            Instance = this;
        }

        public void CreateTeams()
        {
            if (availableTeams.Count > 0)
                return;
            
            availableTeams = new List<TestFlightRnDTeam>(3);

            availableTeams.Add(new TestFlightRnDTeam("Skilled Engineering Team", 100f, 1f));
            availableTeams.Add(new TestFlightRnDTeam("Advanced Engineering Team", 125f, 1.5f));
            availableTeams.Add(new TestFlightRnDTeam("Expert Engineering Team", 150f, 2.0f));

            activeTeams = new Dictionary<string, TestFlightRnDTeam>();
        }

        public void AddResearchTeam(Part part, int team)
        {
            string partName = TestFlightUtil.GetFullPartName(part);

            if (IsPartBeingResearched(partName))
                return;

            ITestFlightCore core = TestFlightUtil.GetCore(part);
            if (core == null)
                return;

            if (team < availableTeams.Count)
            {
                TestFlightRnDTeam template = availableTeams[team];
                activeTeams.Add(partName, new TestFlightRnDTeam(template.Name, template.Points, template.CostFactor));
                activeTeams[partName].PartInResearch = partName;
                activeTeams[partName].MaxData = core.GetMaximumRnDData();
                activeTeams[partName].PartRnDCost = core.GetRnDCost();
                activeTeams[partName].PartRnDRate = core.GetRnDRate();
            }
        }

        public void RemoveResearch(string partName)
        {
            if (activeTeams == null)
                return;
            
            activeTeams.Remove(partName);
        }

        public bool IsPartBeingResearched(string partName)
        {
            if (activeTeams == null)
                return false;
            
            return activeTeams.ContainsKey(partName);
        }

        protected TestFlightRnDTeam GetTeamTemplate(string templateName)
        {
            if (availableTeams == null)
                CreateTeams();
            
            foreach (TestFlightRnDTeam team in availableTeams)
            {
                if (team.Name == templateName)
                    return team;
            }
            return null;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasNode("TESTFLIGHT_RNDTEAM"))
            {
                if (activeTeams == null)
                    activeTeams = new Dictionary<string, TestFlightRnDTeam>();
                foreach (ConfigNode teamNode in node.GetNodes("TESTFLIGHT_RNDTEAM"))
                {
                    TestFlightRnDTeam template = GetTeamTemplate(teamNode.GetValue("template"));
                    if (template != null)
                    {
                        string partName = teamNode.GetValue("PartInResearch");
                        activeTeams.Add(partName, new TestFlightRnDTeam(template.Name, template.Points, template.CostFactor));
                        activeTeams[partName].PartInResearch = partName;
                        activeTeams[partName].MaxData = float.Parse(teamNode.GetValue("MaxData"));
                        activeTeams[partName].PartRnDCost = float.Parse(teamNode.GetValue("PartRnDCost"));
                        activeTeams[partName].PartRnDRate = float.Parse(teamNode.GetValue("PartRnDRate"));
                    }
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            if (activeTeams == null)
                return;
            
            if (activeTeams.Count > 0)
            {
                foreach (KeyValuePair<string, TestFlightRnDTeam> entry in activeTeams)
                {
                    ConfigNode teamNode = node.AddNode("TESTFLIGHT_RNDTEAM");
                    teamNode.AddValue("template", entry.Value.Name);
                    teamNode.AddValue("PartInResearch", entry.Value.PartInResearch);
                    teamNode.AddValue("MaxData", entry.Value.MaxData);
                    teamNode.AddValue("PartRnDCost", entry.Value.PartRnDCost);
                    teamNode.AddValue("PartRnDRate", entry.Value.PartRnDRate);
                }
            }
        }
    }
}


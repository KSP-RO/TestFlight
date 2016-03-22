using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using KSPPluginFramework;
using TestFlightAPI;

namespace TestFlightCore
{
    public class MultiCoreConfig : DynamicPartConfig
    {
        #region KSPFields
        public string techTransfer = "";
        public float techTransferMax = 1000;
        public float techTransferGenerationPenalty = 0.05f;
        public int scienceDataValue = 0;
        #endregion

        public void Load(ConfigNode node)
        {
            base.Load(node);
            if (node.HasValue("techTransfer"))
                techTransfer = node.GetValue("techTransfer");
            if (node.HasValue("techTransferMax"))
                int.TryParse(node.GetValue("techTransferMax"), out techTransferMax);
            if (node.HasValue("techTransferGenerationPenalty"))
                float.TryParse(node.GetValue("techTransferGenerationPenalty"), out techTransferGenerationPenalty);
            if (node.HasValue("scienceDataValue"))
                int.TryParse(node.GetValue("scienceDataValue"), out scienceDataValue);
        }

        public void Save(ConfigNode node)
        {
            base.Save(node);
            node.AddValue("techTransfer", techTransfer);
            node.AddValue("techTransferMax", techTransferMax);
            node.AddValue("techTransferGenerationPenalty", techTransferGenerationPenalty);
            node.AddValue("scienceDataValue", scienceDataValue);
        }
    }

    public class TFMultiCore : PartModuleExtended, ITestFlightCore
    {
        // KSPFields should NOT be used
        // Dynamic data is stored in the scenario data store
        // Static config data is stored in a DynamicPartConfig config node and read from the prefab when needed

        // Dynamic data
        public float currentFlightData;
        public float initialFlightData;
        public float startFlightData;
        public float operatingTime;
        public float lastMET;
        public bool initialized = false;
        public float failureRateModifier = 1f;
        // This will be updated so we always know what part to point to
        public string currentPartName = this.part.name;

        // Static config nodes
        List<MultiCoreConfig> coreConfigs;
        MultiCoreConfig currentConfig;

        TestFlightManagerScenario scenario = null;

        #region ITestFlightCore implementation

        public void UpdatePartConfig()
        {
            // If an interop has changed then we need to look through our coreConfigs and reset our current config to the proper one
            // TODO this currently is simplified to only look at the value as an engineConfig just for testing.  Needs to be plugged into
            // a proper query syntax in the future
            foreach (MultiCoreConfig coreConfig in coreConfigs)
            {
                if (TestFlightUtil.EvaluateQuery(coreConfig.PartFilter, this.part))
                {
                    // Set this to be our current config
                    currentConfig = coreConfig;
                    currentPartName = coreConfig.PartName;
                    // update dynamic values from the scenario store
                    currentFlightData = scenario.GetPartDataForPart(currentPartName).GetFloat("flightData");
                    currentFlightData = scenario.GetPartDataForPart(currentPartName).GetFloat("flightData");
                }
            }
        }

        public int GetPartStatus()
        {
            throw new NotImplementedException();
        }

        public ITestFlightFailure GetFailureModule()
        {
            throw new NotImplementedException();
        }

        public void InitializeFlightData(float flightData)
        {
            throw new NotImplementedException();
        }

        public void HighlightPart(bool doHighlight)
        {
            throw new NotImplementedException();
        }

        public float GetRepairTime()
        {
            throw new NotImplementedException();
        }

        public bool IsFailureAcknowledged()
        {
            throw new NotImplementedException();
        }

        public void AcknowledgeFailure()
        {
            throw new NotImplementedException();
        }

        public string GetRequirementsTooltip()
        {
            throw new NotImplementedException();
        }

        public double GetBaseFailureRate()
        {
            throw new NotImplementedException();
        }

        public float GetMaximumData()
        {
            throw new NotImplementedException();
        }

        public FloatCurve GetBaseReliabilityCurve()
        {
            throw new NotImplementedException();
        }

        public MomentaryFailureRate GetWorstMomentaryFailureRate()
        {
            throw new NotImplementedException();
        }

        public MomentaryFailureRate GetBestMomentaryFailureRate()
        {
            throw new NotImplementedException();
        }

        public List<MomentaryFailureRate> GetAllMomentaryFailureRates()
        {
            throw new NotImplementedException();
        }

        public double GetMomentaryFailureRateForTrigger(string trigger)
        {
            throw new NotImplementedException();
        }

        public double SetTriggerMomentaryFailureModifier(string trigger, double multiplier, PartModule owner)
        {
            throw new NotImplementedException();
        }

        public string FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units)
        {
            throw new NotImplementedException();
        }

        public string FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, int maximum)
        {
            throw new NotImplementedException();
        }

        public string FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm)
        {
            throw new NotImplementedException();
        }

        public string FailureRateToMTBFString(double failureRate, TestFlightUtil.MTBFUnits units, bool shortForm, int maximum)
        {
            throw new NotImplementedException();
        }

        public double FailureRateToMTBF(double failureRate, TestFlightUtil.MTBFUnits units)
        {
            throw new NotImplementedException();
        }

        public float GetFlightData()
        {
            throw new NotImplementedException();
        }

        public float GetInitialFlightData()
        {
            throw new NotImplementedException();
        }

        public float GetFlightTime()
        {
            throw new NotImplementedException();
        }

        public float SetDataRateLimit(float limit)
        {
            throw new NotImplementedException();
        }

        public float SetDataCap(float cap)
        {
            throw new NotImplementedException();
        }

        public void SetFlightData(float data)
        {
            throw new NotImplementedException();
        }

        public void SetFlightTime(float seconds)
        {
            throw new NotImplementedException();
        }

        public float ModifyFlightData(float modifier)
        {
            throw new NotImplementedException();
        }

        public float ModifyFlightTime(float modifier)
        {
            throw new NotImplementedException();
        }

        public float ModifyFlightData(float modifier, bool additive)
        {
            throw new NotImplementedException();
        }

        public float ModifyFlightTime(float modifier, bool additive)
        {
            throw new NotImplementedException();
        }

        public float GetEngineerDataBonus(float partEngineerBonus)
        {
            throw new NotImplementedException();
        }

        public ITestFlightFailure TriggerFailure()
        {
            throw new NotImplementedException();
        }

        public ITestFlightFailure TriggerNamedFailure(string failureModuleName)
        {
            throw new NotImplementedException();
        }

        public ITestFlightFailure TriggerNamedFailure(string failureModuleName, bool fallbackToRandom)
        {
            throw new NotImplementedException();
        }

        public List<string> GetAvailableFailures()
        {
            throw new NotImplementedException();
        }

        public void EnableFailure(string failureModuleName)
        {
            throw new NotImplementedException();
        }

        public void DisableFailure(string failureModuleName)
        {
            throw new NotImplementedException();
        }

        public float GetOperatingTime()
        {
            throw new NotImplementedException();
        }

        public float AttemptRepair()
        {
            throw new NotImplementedException();
        }

        public float ForceRepair()
        {
            throw new NotImplementedException();
        }

        public bool IsPartOperating()
        {
            throw new NotImplementedException();
        }

        public bool TestFlightEnabled
        {
            get
            {
                return true;
            }
        }

        public string Configuration
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string Title
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public System.Random RandomGenerator
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool DebugEnabled
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        public override void OnAwake()
        {
            base.OnAwake();
            scenario = TestFlightManagerScenario.Instance;

            // Pull dynamic values from scenario store

            TestFlightManagerScenario.Instance.GetPartDataForPart(TestFlightUtil.GetFullPartName(this.part));

            // Pull static values from prefab
        }
    }
}


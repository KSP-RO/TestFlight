using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace TestFlightAPI
{

    public class FlightDataRecorderBase : PartModule, IFlightDataRecorder
    {
        private double lastRecordedMet = 0;
        private ITestFlightCore core = null;
        #region KSPFields
        [KSPField(isPersistant = true)]
        public float flightDataMultiplier = 10.0f;
        [KSPField(isPersistant = true)]
        public float flightDataEngineerModifier = 0.25f;
        [KSPField(isPersistant=true)]
        public string configuration = "";
        #endregion

        public bool TestFlightEnabled
        {
            get
            {
                bool enabled = true;
                // Verify we have a valid core attached
                if (core == null)
                    enabled = false;
                // If this part has a ModuleEngineConfig then we need to verify we are assigned to the active configuration
                if (this.part.Modules.Contains("ModuleEngineConfigs"))
                {
                    string currentConfig = (string)(part.Modules["ModuleEngineConfigs"].GetType().GetField("configuration").GetValue(part.Modules["ModuleEngineConfigs"]));
                    if (currentConfig != configuration)
                        enabled = false;
                }
                return enabled;
            }
        }
        public string Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            core = TestFlightUtil.GetCore(this.part);

            if (core == null)
                StartCoroutine("GetCore");
        }

        public override void OnUpdate()
        {
            if (!TestFlightEnabled)
                return;

            base.OnUpdate();

            double currentMet = core.GetOperatingTime();
            if (!IsRecordingFlightData())
            {
                lastRecordedMet = currentMet;
                return;
            }

            string scope = core.GetScope();

            if (IsRecordingFlightData())
            {
                double flightData = (currentMet - lastRecordedMet) * flightDataMultiplier;
                double engineerBonus = core.GetEngineerDataBonus(flightDataEngineerModifier);
                flightData *= engineerBonus;
                if (flightData >= 0)
                    core.ModifyFlightDataForScope(flightData, scope, true);
            }

            core.ModifyFlightTimeForScope(currentMet - lastRecordedMet, scope, true);

            lastRecordedMet = currentMet;
        }

        IEnumerator GetCore()
        {
            while (core == null)
            {
                core = TestFlightUtil.GetCore(this.part);
                yield return null;
            }
        }

        public virtual bool IsPartOperating()
        {
            if (!TestFlightEnabled)
                return false;

            return true;
        }

        public virtual bool IsRecordingFlightData()
        {
            if (!TestFlightEnabled)
                return false;

            if (!IsPartOperating())
                return false;

            if (!isEnabled)
                return false;
				
            return true;
        }

        public override void OnAwake()
        {
            base.OnAwake();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }
    }
}


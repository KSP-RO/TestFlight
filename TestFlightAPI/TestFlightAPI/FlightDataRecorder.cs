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
        private bool isReady = false;
        private ITestFlightCore core = null;
        #region KSPFields
        [KSPField(isPersistant = true)]
        public float flightDataMultiplier = 10.0f;
        [KSPField(isPersistant = true)]
        public float flightDataEngineerModifier = 0.25f;
        [KSPField(isPersistant=true)]
        public string configuration = "";
        #endregion

        public string Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }
        public bool IsCurrentEngineConfiguration()
        {
            if (this.part.Modules.Contains("ModuleEngineConfigs"))
            {
                string currentConfig = (string)(part.Modules["ModuleEngineConfigs"].GetType().GetField("configuration").GetValue(part.Modules["ModuleEngineConfigs"]));
                return currentConfig.Equals(configuration);
            }
            else
            {
                return configuration.Equals("");
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            foreach (PartModule pm in this.part.Modules)
            {
                core = pm as ITestFlightCore;
                if (core != null && core.Configuration == configuration)
                    break;
            }

            if (core == null)
            {
                StartCoroutine("GetCore");
            }
            else
                isReady = true;

            if (!IsCurrentEngineConfiguration())
                isReady = false;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!IsCurrentEngineConfiguration())
                return;

            if (!isReady)
                return;

            if (core == null)
                return;

            double currentMet = core.GetOperatingTime();
            if (!IsRecordingFlightData())
            {
                lastRecordedMet = currentMet;
                return;
            }

            string scope = core.GetScope();
            double flightData = (currentMet - lastRecordedMet) * flightDataMultiplier;
            double engineerBonus = core.GetEngineerDataBonus(flightDataEngineerModifier);
            flightData *= engineerBonus;

            if (IsRecordingFlightData())
                core.ModifyFlightDataForScope(flightData, scope, true);

            core.ModifyFlightTimeForScope(currentMet - lastRecordedMet, scope, true);

            lastRecordedMet = currentMet;
        }

        IEnumerator GetCore()
        {
            while (core == null)
            {
                foreach (PartModule pm in this.part.Modules)
                {
                    core = pm as ITestFlightCore;
                    if (core != null && core.Configuration == configuration)
                    {
                        Debug.Log("Found Code");
                        break;
                    }
                }
                Debug.Log("Yielding");
                yield return null;
            }
            if (this.part.started)
                isReady = true;
            if (!IsCurrentEngineConfiguration())
                isReady = false;
        }

        public virtual bool IsPartOperating()
        {
            return true;
        }

        public virtual bool IsRecordingFlightData()
        {
            bool isRecording = true;

            if (!IsCurrentEngineConfiguration())
                return false;

            if (!IsPartOperating())
                return false;

            if (!isReady)
                return false;

            if (!isEnabled)
                return false;
				
            return isRecording;
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


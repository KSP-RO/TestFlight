using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace TestFlightAPI
{

    public class FlightDataRecorderBase : PartModule, IFlightDataRecorder
    {
        private double lastRecordedMet = 0;
        #region KSPFields
        [KSPField(isPersistant = true)]
        public float flightDataMultiplier = 10.0f;
        [KSPField(isPersistant = true)]
        public float flightDataEngineerModifier = 0.25f;
        #endregion

        private ITestFlightCore core = null;


        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            foreach (PartModule pm in this.part.Modules)
            {
                core = pm as ITestFlightCore;
                if (core != null)
                    break;
            }

        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            double currentMet = this.vessel.missionTime;
            if (!IsRecordingFlightData())
            {
                lastRecordedMet = currentMet;
                return;
            }

            string scope = core.GetScope();
            double flightData = (currentMet - lastRecordedMet) * flightDataMultiplier;
            double engineerBonus = core.GetEngineerDataBonus(flightDataEngineerModifier);
            flightData *= engineerBonus;

            if (IsRecordingFlightData)
                core.ModifyFlightDataForScope(flightData, scope, true);

            core.ModifyFlightTimeForScope(currentMet - lastRecordedMet, scope, true);

            lastRecordedMet = currentMet;
        }

        public virtual bool IsRecordingFlightData()
        {
            bool isRecording = true;

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


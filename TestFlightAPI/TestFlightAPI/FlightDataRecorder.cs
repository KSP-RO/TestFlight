using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace TestFlightAPI
{

    public class FlightDataRecorderBase : PartModule, IFlightDataRecorder
    {
        private ITestFlightCore core = null;
        #region KSPFields
        [KSPField(isPersistant = true)]
        public float lastRecordedMet = 0;
        [KSPField(isPersistant = true)]
        public float flightDataMultiplier = 10.0f;
        [KSPField(isPersistant = true)]
        public float flightDataEngineerModifier = 0.25f;
        [KSPField(isPersistant=true)]
        public string configuration = "";
        #endregion

        internal void Log(string message)
        {
            message = String.Format("FlightDataRecorder({0}[{1}]): {2}", TestFlightUtil.GetFullPartName(this.part), Configuration, message);
            TestFlightUtil.Log(message, this.part);
        }


        public bool TestFlightEnabled
        {
            get
            {
                // Verify we have a valid core attached
                if (core == null)
                    return false;
                if (string.IsNullOrEmpty(Configuration))
                    return true;
                return TestFlightUtil.EvaluateQuery(Configuration, this.part);
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

            float currentMet = core.GetOperatingTime();
            if (!IsRecordingFlightData())
            {
                lastRecordedMet = currentMet;
                return;
            }

            if (IsRecordingFlightData())
            {
                float flightData = (currentMet - lastRecordedMet) * flightDataMultiplier;
                float engineerBonus = core.GetEngineerDataBonus(flightDataEngineerModifier);
                flightData *= engineerBonus;
                if (flightData >= 0)
                    core.ModifyFlightData(flightData, true);
            }

            core.ModifyFlightTime(currentMet - lastRecordedMet, true);

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

        public virtual string GetTestFlightInfo()
        {
            return "";
        }
    }
}


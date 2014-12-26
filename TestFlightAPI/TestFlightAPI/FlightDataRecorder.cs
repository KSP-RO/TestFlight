using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestFlightAPI
{

    public class FlightDataRecorderBase : PartModule, IFlightDataRecorder
    {
        private float currentData = 0.0f;
        private int currentFlightTime = 0;
		private string currentScope = "NONE";

        private double lastRecordedMet = 0;

        private double deepSpaceThreshold = 10000000;


        #region KSPFields
        [KSPField(isPersistant = true)]
        public FlightDataConfig flightData;
        [KSPField(isPersistant = true)]
        private bool isNewInstanceOfPart = true;
        [KSPField(isPersistant = true)]
        public float flightDataMultiplier = 10.0f;
        [KSPField(isPersistant = true)]
        public float flightDataEngineerModifier = 0.25f;
        [KSPField(isPersistant = true)]
        public float maxFlightData = 10000.0f;
        #endregion


        /// <summary>
        /// Gets the flight data for a specified scope
        /// </summary>
        /// <returns>The data.</returns>
        /// <param name="bodyName">Body name.</param>
        /// <param name="situation">Situation.</param>
        public float GetData(string scope)
        {
            FlightDataBody bodyData = flightData.GetFlightData(scope);
            if (bodyData != null)
            {
                return bodyData.flightData;
            }
            return 0.0f;
        }

        /// <summary>
        /// Gets the flight data for the current body and situation
        /// </summary>
        /// <returns>The current data.</returns>
        public float GetCurrentData()
        {
            return currentData;
        }

        /// <summary>
        /// Gets the flight time for the current body and situation
        /// </summary>
        /// <returns>The current flight time.</returns>
        public int GetCurrentFlightTime()
        {
            return currentFlightTime;
        }

        public string GetDataSituation()
        {
            string situation = this.vessel.situation.ToString().ToLower();

            // Determine if we are recording data in SPACE or ATMOSHPHERE
            if (situation == "sub_orbital" || situation == "orbiting" || situation == "escaping" || situation == "docked")
            {
                if (this.vessel.altitude > deepSpaceThreshold)
                    situation = "deep-space";
                else
                    situation = "space";
            }
            else if (situation == "flying" || situation == "landed" || situation == "splashed" || situation == "prelaunch")
                situation = "atmosphere";
            else
                situation = "default";

            return situation;
        }

        public string GetDataBody()
        {
            string situation = GetDataSituation();

            if (situation == "deep-space")
                return "none";
            else
                return this.vessel.mainBody.name.ToLower();
        }

        // Interface Implementation
        public TestFlightData GetCurrentFlightData()
        {
            TestFlightData data = new TestFlightData();
            data.scope = String.Format("{0}_{1}", GetDataBody(), GetDataSituation());
            data.flightData = GetCurrentData();
            data.flightTime = GetCurrentFlightTime();

            return data;
        }
        /// <summary>
        /// Called by the API when the player enteres the flight scene.  This sends the saved flight data for the part so that
        /// new instances of the part can load the existing data.  
        /// IMPORTANT!!!
        /// A part should accept this data ONLY if it is a new instance of the part and not an existing one!  In otherwords an existing instance of a part doesn't suddenly become more reliable.  Just new ones.
        /// </summary>
        /// <param name="allFlightData">All flight data.</param>
        public void InitializeFlightData(List<TestFlightData> allFlightData)
        {
            Debug.Log("FlightDataRecorderBase: InitializeFlightData()");
            if (isNewInstanceOfPart)
            {
                foreach (TestFlightData data in allFlightData)
                {
                    if (flightData == null)
                    {
                        flightData = new FlightDataConfig();
                    }
                    flightData.AddFlightData(data.scope, data.flightData, 0);
                }
                isNewInstanceOfPart = false;
            }
            else
            {
                Debug.Log("Skipping intialization because not a new instance of part");
            }
            // Initialize our current scope with the data we just received
            currentScope = String.Format("{0}_{1}", GetDataBody(), GetDataSituation());
            currentData = GetData(currentScope);
            currentFlightTime = 0;
        }

        public void SetDeepSpaceThreshold(double newThreshold)
        {
            deepSpaceThreshold = newThreshold;
        }

        public virtual void DoFlightUpdate(double missionStartTime, double flightDataMultiplier, double flightDataEngineerMultiplier)
        {
            double currentMet = Planetarium.GetUniversalTime() - missionStartTime;
            string scope = String.Format("{0}_{1}", GetDataBody(), GetDataSituation());
            // Check to see if we have changed scope
            if (scope != currentScope)
            {
                // If we have moved to a new scope then we need to reset our current data counters
                // First save what we have for the old scope
                Debug.Log("FlightDataRecorderBase: Changing scope from " + currentScope + " to " + scope);
                flightData.AddFlightData(currentScope, currentData, currentFlightTime);
                // Try to get any existing stored data for this scope, or set it to 0
                FlightDataBody bodyData = flightData.GetFlightData(scope);
                if (bodyData != null)
                {
                    Debug.Log("FlightDataRecorderBase: Loading existing data for new scope");
                    currentData = bodyData.flightData;
                    currentFlightTime = bodyData.flightTime;
                }
                else
                {
                    Debug.Log("FlightDataRecorderBase: No existing data for new scope.  Starting fresh");
                    currentData = 0.0f;
                    currentFlightTime = 0;
                }
                // move to the new scope
                currentScope = scope;
            }
            if (!IsRecordingFlightData())
            {
                lastRecordedMet = currentMet;
                return;
            }

            // TODO once migrated to KSP 0.90, hook this up to the Kerbal engineer skill if an engineer is present on board ship
            int engineerLevel = 0;
            double baseEnginerModifier = engineerLevel * flightDataEngineerModifier * flightDataEngineerMultiplier;
            double engineerModifier = 1.0 + baseEnginerModifier;

            if (currentMet > lastRecordedMet)
            {
                currentData += (float)(((currentMet - lastRecordedMet) * flightDataMultiplier) * engineerModifier);
                currentFlightTime += (int)(currentMet - lastRecordedMet);
            }
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
            Debug.Log("FlightDataRecorderBase: OnAwake()");
            base.OnAwake();
            if (flightData == null)
            {
                flightData = new FlightDataConfig();
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("isNewInstanceOfPart"))
                isNewInstanceOfPart = bool.Parse(node.GetValue("isNewInstanceOfPart"));
            base.OnLoad(node);
        }

        public override void OnSave(ConfigNode node)
        {
            // Make sure our FlightData configs are up to date
            flightData.AddFlightData(currentScope, currentData, currentFlightTime);
            node.AddValue("isNewInstanceOfPart", isNewInstanceOfPart);
            base.OnSave(node);
        }
    }
}


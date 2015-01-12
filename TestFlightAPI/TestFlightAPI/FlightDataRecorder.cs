using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace TestFlightAPI
{

    public class FlightDataRecorderBase : PartModule, IFlightDataRecorder
    {
        private double currentData = 0.0f;
        private double currentFlightTime = 0;
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
        #endregion


        /// <summary>
        /// Gets the flight data for a specified scope
        /// </summary>
        /// <returns>The data.</returns>
        /// <param name="bodyName">Body name.</param>
        /// <param name="situation">Situation.</param>
        public double GetData(string scope)
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
        public double GetCurrentData()
        {
            return currentData;
        }

        /// <summary>
        /// Gets the flight time for the current body and situation
        /// </summary>
        /// <returns>The current flight time.</returns>
        public double GetCurrentFlightTime()
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
            if (isNewInstanceOfPart)
            {
                if (allFlightData != null)
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

        public virtual void DoFlightUpdate(double missionStartTime, double globalFlightDataMultiplier, double globalFlightDataEngineerMultiplier)
        {
            double currentMet = Planetarium.GetUniversalTime() - missionStartTime;
            string scope = String.Format("{0}_{1}", GetDataBody(), GetDataSituation());
            // Check to see if we have changed scope
            if (scope != currentScope)
            {
                // If we have moved to a new scope then we need to reset our current data counters
                // First save what we have for the old scope
                flightData.AddFlightData(currentScope, currentData, currentFlightTime);
                // Try to get any existing stored data for this scope, or set it to 0
                FlightDataBody bodyData = flightData.GetFlightData(scope);
                if (bodyData != null)
                {
                    currentData = bodyData.flightData;
                    currentFlightTime = bodyData.flightTime;
                }
                else
                {
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

            List<ProtoCrewMember> crew = this.part.vessel.GetVesselCrew().Where(c => c.experienceTrait.Title == "Engineer").ToList();
            double totalEngineerBonus = 0;
            foreach (ProtoCrewMember crewMember in crew)
            {
                int engineerLevel = crewMember.experienceLevel;
                totalEngineerBonus = totalEngineerBonus + (flightDataEngineerModifier * engineerLevel * globalFlightDataEngineerMultiplier);
            }
            double engineerModifier = 1.0 + totalEngineerBonus;
            if (currentMet > lastRecordedMet)
            {
                currentData += (float)(((currentMet - lastRecordedMet) * flightDataMultiplier * globalFlightDataMultiplier) * engineerModifier);
                currentFlightTime += (int)(currentMet - lastRecordedMet);
            }
            lastRecordedMet = currentMet;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            ITestFlightCore core = null;
            foreach (PartModule pm in this.part.Modules)
            {
                core = pm as ITestFlightCore;
                if (core != null)
                    break;
            }
            if (core == null)
                return;

            double currentMet = this.vessel.missionTime;
            if (!IsRecordingFlightData())
            {
                lastRecordedMet = currentMet;
                return;
            }

            string scope = core.GetScope();
            // TODO
            // The core needs to expose the main settings for data multipliers so we can use them here
            // Need to calculate the FlightData multiplier as well as Engineer bonus
            core.ModifyFlightDataForScope( (currentMet - lastRecordedMet) * flightDataMultiplier, scope, true);
            core.ModifyFlightTimeForScope(currentMet - lastRecordedMet, scope, true);
            lastRecordedMet = currentMet;
        }
        public virtual void ModifyCurrentFlightData(float modifier)
        {
            currentData += modifier;
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


using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestFlightAPI
{
    public class RepairConfig : IConfigNode
    {
        [KSPField(isPersistant = true)]
        public bool requiresEVA = false;

        [KSPField(isPersistant = true)]
        public bool canBeRepairedOnSplashed = false;

        [KSPField(isPersistant = true)]
        public bool canBeRepairedByRemote = false;

        [KSPField(isPersistant = true)]
        public bool canBeRepairedInFlight = false;

        [KSPField(isPersistant = true)]
        public int repairChance = 0;

        [KSPField(isPersistant = true)]
        public float dataScale = 0;

        [KSPField(isPersistant = true)]
        public float dataSize = 0;

        [KSPField(isPersistant = true)]
        public string replacementPart = "NONE";

        [KSPField(isPersistant = true)]
        public bool replacementPartOptional = false;

        [KSPField(isPersistant = true)]
        public float replacementPartBonus = 0.5f;

        public void Load(ConfigNode node)
        {
            if (node.HasValue("canBeRepairedInFlight"))
                canBeRepairedInFlight = bool.Parse(node.GetValue("canBeRepairedInFlight"));
            if (node.HasValue("canBeRepairedOnSplashed"))
                canBeRepairedOnSplashed = bool.Parse(node.GetValue("canBeRepairedOnSplashed"));
            if (node.HasValue("canBeRepairedByRemote"))
                canBeRepairedByRemote = bool.Parse(node.GetValue("canBeRepairedByRemote"));
            if (node.HasValue("requiresEVA"))
                requiresEVA = bool.Parse(node.GetValue("requiresEVA"));
            if (node.HasValue("repairChance"))
                repairChance = int.Parse(node.GetValue("repairChance"));
            if (node.HasValue("dataScale"))
                dataScale = float.Parse(node.GetValue("dataScale"));
            if (node.HasValue("dataSize"))
                dataSize = float.Parse(node.GetValue("dataSize"));
            if (node.HasValue("replacementPart"))
                replacementPart = node.GetValue("replacementPart");
            if (node.HasValue("replacementPartOptional"))
                replacementPartOptional = bool.Parse(node.GetValue("replacementPartOptional"));
            if (node.HasValue("replacementPartBonus"))
                replacementPartBonus = float.Parse(node.GetValue("replacementPartBonus"));
        }
        
        public void Save(ConfigNode node)
        {
            node.AddValue("requiresEVA", requiresEVA);
            node.AddValue("canBeRepairedOnSplashed", canBeRepairedOnSplashed);
            node.AddValue("canBeRepairedByRemote", canBeRepairedByRemote);
            node.AddValue("repairChance", repairChance);
            node.AddValue("dataScale", dataScale);
            node.AddValue("dataSize", dataSize);
            node.AddValue("replacementPart", replacementPart);
            node.AddValue("replacementPartOptional", replacementPartOptional);
            node.AddValue("replacementPartBonus", replacementPartBonus);
        }
        
        public override string ToString()
        {
            string stringRepresentation = "";
            
            stringRepresentation = String.Format("{0},{1},{2},{3},{4},{5:F2},{6:F2},{7},{8},{9:F2}", 
                canBeRepairedInFlight,
                canBeRepairedOnSplashed,
                canBeRepairedByRemote,
                requiresEVA,
                repairChance,
                dataScale,
                dataSize,
                replacementPart,
                replacementPartOptional,
                replacementPartBonus);
            
            return stringRepresentation;
        }
        
        public static RepairConfig FromString(string s)
        {
            RepairConfig repairConfig = null;
            string[] sections = s.Split(new char[1] { ',' });
            if (sections.Length == 10)
            {
                repairConfig = new RepairConfig();
                repairConfig.canBeRepairedInFlight = bool.Parse(sections[0]);
                repairConfig.canBeRepairedOnSplashed = bool.Parse(sections[1]);
                repairConfig.canBeRepairedByRemote = bool.Parse(sections[2]);
                repairConfig.requiresEVA = bool.Parse(sections[3]);

                repairConfig.repairChance = int.Parse(sections[4]);
                
                repairConfig.dataScale = float.Parse(sections[5]);
                repairConfig.dataScale = float.Parse(sections[6]);

                repairConfig.replacementPart = sections[7];
                repairConfig.replacementPartOptional = bool.Parse(sections[8]);
                repairConfig.replacementPartBonus = float.Parse(sections[9]);
            }
            
            return repairConfig;
        }
        
    }

    /// <summary>
    /// This part module provides a specific failure for the TestFlight system
    /// </summary>
    public class TestFlightFailureBase : PartModule, ITestFlightFailure
    {
        [KSPField(isPersistant = true)]
        public string failureType;
        [KSPField(isPersistant = true)]
        public string severity;
        [KSPField(isPersistant = true)]
        public int weight;
        [KSPField(isPersistant = true)]
        public string failureTitle = "Failure";
        [KSPField(isPersistant=true)]
        public string configuration = "";

        public string repairConfigString;

        public bool TestFlightEnabled
        {
            get
            {
                bool enabled = true;
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


        public RepairConfig repairConfig;

        /// <summary>
        /// Gets the details of the failure encapsulated by this module.  In most cases you can let the base class take care of this unless oyu need to do somethign special
        /// </summary>
        /// <returns>The failure details.</returns>
        public virtual TestFlightFailureDetails GetFailureDetails()
        {
            TestFlightFailureDetails details = new TestFlightFailureDetails();

            details.weight = weight;
            details.failureType = failureType;
            details.severity = severity;
            details.failureTitle = failureTitle;

            return details;
        }

        internal bool HasReplacementPart(string replacementPart)
        {
            // Look that all materials are available
            List<PartResource> availableResources = new List<PartResource> ();
            // Try to find the resource which represents the part
            PartResourceDefinition partDefinition = PartResourceLibrary.Instance.GetDefinition(replacementPart);
            if (partDefinition == null)
                return false;

            this.part.GetConnectedResources (partDefinition.id, ResourceFlowMode.ALL_VESSEL, availableResources);
            foreach (PartResource res in availableResources) {
                if (res.amount >= 1.0f)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Gets the repair requirements from the Failure module for display to the user
        /// </summary>
        /// <returns>A List of all repair requirements for attempting repair of the part</returns>
        public List<RepairRequirements> GetRepairRequirements()
        {
            if (!TestFlightEnabled)
                return null;

            if (repairConfig == null)
            {
                Debug.Log("TestFlightFailure: No repairConfig found");
                return null;
            }

            List<RepairRequirements> requirements = new List<RepairRequirements>();
            Vessel.Situations situation = this.vessel.situation;

            // In flight
            if (!repairConfig.canBeRepairedInFlight)
            {
                RepairRequirements requirement = new RepairRequirements();
                requirement.requirementMessage = "Vessel must not be in flight";
                if (situation == Vessel.Situations.DOCKED
                    || situation == Vessel.Situations.LANDED
                    || situation == Vessel.Situations.PRELAUNCH
                    || situation == Vessel.Situations.SPLASHED)
                {
                    requirement.requirementMet = true;
                }
                else
                    requirement.requirementMet = false;
                requirements.Add(requirement);
            }

            // Splashed down
            if (!repairConfig.canBeRepairedOnSplashed)
            {
                RepairRequirements requirement = new RepairRequirements();
                requirement.requirementMessage = "Vessel must not be in water";
                if (situation == Vessel.Situations.SPLASHED)
                {
                    requirement.requirementMet = false;
                }
                else
                    requirement.requirementMet = true;
                requirements.Add(requirement);
            }

            // Requires EVA
            // TODO
            // Don't know how to do this yet

            // Remote repair
            if (!repairConfig.canBeRepairedByRemote)
            {
                RepairRequirements requirement = new RepairRequirements();
                requirement.requirementMessage = "Vessel must not be under remote control";
                if (this.vessel.GetCrewCount() <= 0)
                    requirement.requirementMet = false;
                else
                    requirement.requirementMet = true;
                requirements.Add(requirement);
            }

            // Replacement part
            if (repairConfig.replacementPart != "NONE")
            {
                RepairRequirements requirement = new RepairRequirements();
                if (repairConfig.replacementPartOptional)
                {
                    requirement.requirementMessage = "Having a replacement " + repairConfig.replacementPart + " on board would be a bonus";
                    requirement.optionalRequirement = true;
                    requirement.repairBonus = repairConfig.replacementPartBonus;
                }
                else
                {
                    requirement.requirementMessage = "Need a replacement " + repairConfig.replacementPart;
                }
                if (HasReplacementPart(repairConfig.replacementPart))
                    requirement.requirementMet = true;
                else
                {
                    requirement.requirementMet = false;
                }
                requirements.Add(requirement);
            }

            return requirements;
        }

        internal float GetOptionalRepairBonus()
        {
            float totalBonus = 0f;
            List<RepairRequirements> requirements = GetRepairRequirements();
            foreach (var entry in requirements)
            {
                if (entry.optionalRequirement && entry.requirementMet)
                    totalBonus += entry.repairBonus;
            }

            return totalBonus;
        }

        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public virtual void DoFailure()
        {
        }
        
        /// <summary>
        /// Asks the repair module if all condtions have been met for the player to attempt repair of the failure.  Here the module can verify things such as the conditions (landed, eva, splashed), parts requirements, etc
        /// </summary>
        /// <returns><c>true</c> if this instance can attempt repair; otherwise, <c>false</c>.</returns>
        public virtual bool CanAttemptRepair()
        {
            List<RepairRequirements> requirements = GetRepairRequirements();
            foreach (var entry in requirements)
            {
                if (!entry.optionalRequirement && !entry.requirementMet)
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Trigger a repair ATTEMPT of the module's failure.  It is the module's responsability to take care of any consumable resources, data transmission, etc required to perform the repair
        /// </summary>
        /// <returns>Should return true if the failure was repaired, false otherwise</returns>
        public virtual double AttemptRepair()
        {
            if (repairConfig == null)
                return -1;

            if (!CanAttemptRepair())
                return -1;

            float repairChance = repairConfig.repairChance;
            repairChance += GetOptionalRepairBonus();
            if (UnityEngine.Random.Range(0f, 100f) <= repairChance)
                return DoRepair();
            return -1;
        }

        /// <summary>
        /// Forces the repair.  This should instantly repair the part, regardless of whether or not a normal repair can be done.  IOW if at all possible the failure should fixed after this call.
        /// This is made available as an API method to allow things like failure simulations.
        /// </summary>
        /// <returns><c>true</c>, if failure was repaired, <c>false</c> otherwise.</returns>
        public virtual double ForceRepair()
        {
            return DoRepair();
        }

        public virtual double DoRepair()
        {
            return 0;
        }

        /// <summary>
        /// Gets the seconds until repair is complete
        /// </summary>
        /// <returns>The seconds until repair is complete, <c>0</c> if repair is complete, and <c>-1</c> if something changed the inteerupt the repairs and reapir has stopped with the part still broken.</returns>
        public double GetSecondsUntilRepair()
        {
            return 0;
        }

        public override void OnAwake()
        {
            base.OnAwake();
        }
        
        public override void OnStart(StartState state)
        {
            if (repairConfig == null && repairConfigString.Length > 0)
            {
                repairConfig = RepairConfig.FromString(repairConfigString);
            }
            base.OnStart(state);
        }
        
        public override void OnLoad(ConfigNode node)
        {
            if (node.HasNode("REPAIR"))
            {
                repairConfig = new RepairConfig();
                repairConfig.Load(node.GetNode("REPAIR"));
                repairConfigString = repairConfig.ToString();
            }
            else
            {
                repairConfig = null;
            }
            base.OnLoad(node);
        }
        
        public override void OnSave(ConfigNode node)
        {
            if (repairConfig != null)
            {
                repairConfig.Save(node.AddNode("REPAIR"));
            }
            base.OnLoad(node);
        }
    }
}

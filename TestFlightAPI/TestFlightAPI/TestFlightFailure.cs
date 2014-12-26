using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestFlightAPI
{
    public class RepairConfig : IConfigNode
    {
        [KSPField(isPersistant = true)]
        public bool canBeRepairedOnLanded = false;

        [KSPField(isPersistant = true)]
        public bool requiresEVA = false;

        [KSPField(isPersistant = true)]
        public bool canBeRepairedOnSplashed = false;

        [KSPField(isPersistant = true)]
        public bool canBeRepairedByRemote = false;

        [KSPField(isPersistant = true)]
        public bool canBeRepairedInFlight = false;

        [KSPField(isPersistant = true)]
        public int sparePartsRequired = 0;

        [KSPField(isPersistant = true)]
        public int timeRequired = 0;

        [KSPField(isPersistant = true)]
        public int repairChance = 0;

        [KSPField(isPersistant = true)]
        public float dataScale = 0;

        [KSPField(isPersistant = true)]
        public float dataSize = 0;

        public void Load(ConfigNode node)
        {
            if (node.HasValue("canBeRepairedInFlight"))
                canBeRepairedInFlight = bool.Parse(node.GetValue("canBeRepairedInFlight"));
            if (node.HasValue("canBeRepairedOnLanded"))
                canBeRepairedOnLanded = bool.Parse(node.GetValue("canBeRepairedOnLanded"));
            if (node.HasValue("canBeRepairedOnSplashed"))
                canBeRepairedOnSplashed = bool.Parse(node.GetValue("canBeRepairedOnSplashed"));
            if (node.HasValue("canBeRepairedByRemote"))
                canBeRepairedByRemote = bool.Parse(node.GetValue("canBeRepairedByRemote"));
            if (node.HasValue("requiresEVA"))
                requiresEVA = bool.Parse(node.GetValue("requiresEVA"));
            if (node.HasValue("sparePartsRequired"))
                sparePartsRequired = int.Parse(node.GetValue("sparePartsRequired"));
            if (node.HasValue("timeRequired"))
                timeRequired = int.Parse(node.GetValue("timeRequired"));
            if (node.HasValue("repairChance"))
                repairChance = int.Parse(node.GetValue("repairChance"));
            if (node.HasValue("dataScale"))
                dataScale = float.Parse(node.GetValue("dataScale"));
            if (node.HasValue("dataSize"))
                dataSize = float.Parse(node.GetValue("dataSize"));
        }
        
        public void Save(ConfigNode node)
        {
            node.AddValue("requiresEVA", requiresEVA);
            node.AddValue("canBeRepairedInFlight", canBeRepairedOnLanded);
            node.AddValue("canBeRepairedOnLanded", canBeRepairedOnLanded);
            node.AddValue("canBeRepairedOnSplashed", canBeRepairedOnSplashed);
            node.AddValue("canBeRepairedByRemote", canBeRepairedByRemote);
            node.AddValue("sparePartsRequired", sparePartsRequired);
            node.AddValue("timeRequired", timeRequired);
            node.AddValue("repairChance", repairChance);
            node.AddValue("dataScale", dataScale);
            node.AddValue("dataSize", dataSize);
        }
        
        public override string ToString()
        {
            string stringRepresentation = "";
            
            stringRepresentation = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8:F2},{9:F2}", 
                canBeRepairedInFlight,
                canBeRepairedOnLanded,
                canBeRepairedOnSplashed,
                canBeRepairedByRemote,
                requiresEVA,
                sparePartsRequired,
                timeRequired,
                repairChance,
                dataScale,
                dataSize);
            
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
                repairConfig.canBeRepairedOnLanded = bool.Parse(sections[1]);
                repairConfig.canBeRepairedOnSplashed = bool.Parse(sections[2]);
                repairConfig.canBeRepairedByRemote = bool.Parse(sections[3]);
                repairConfig.requiresEVA = bool.Parse(sections[4]);

                repairConfig.sparePartsRequired = int.Parse(sections[5]);
                repairConfig.timeRequired = int.Parse(sections[6]);
                repairConfig.repairChance = int.Parse(sections[7]);
                
                repairConfig.dataScale = float.Parse(sections[8]);
                repairConfig.dataScale = float.Parse(sections[9]);

            }
            
            return repairConfig;
        }
        
    }

    /// <summary>
    /// This part module provides a specific failure for the TestFlight system
    /// </summary>
    public class TestFlightFailureBase : PartModule, ITestFlightFailure
    {
        /*
         *  Example Module
         *     MODULE
         *      {
         *          name = TestFlightFailure_Disable
         *          failureType = mechanical
         *          severity = major
         *          // 2 = Rare, 4 = Seldom, 8 = Average, 16 = Often, 32 = Common
         *          weight = 4
         *          REPAIR
         *          {
         *              canBeRepairedOnLanded = true
         *              canBeRepairedOnEVA = true
         *              canBeRepairedOnSplashed = false
         *              sparePartsRequired = 10
         *              repairTimeRequired = 100
         *              repairChance = 100
         *          }
         *      }
         */

        [KSPField(isPersistant = true)]
        public string failureType;
        [KSPField(isPersistant = true)]
        public string severity;
        [KSPField(isPersistant = true)]
        public int weight;
        [KSPField(isPersistant = true)]
        public string failureTitle = "Failure";


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
            if (repairConfig == null)
            {
                details.canBeRepaired = false;
                return details;
            }
            details.canBeRepairedByRemote = repairConfig.canBeRepairedByRemote;
            details.requiresEVA = repairConfig.requiresEVA;
            details.canBeRepairedOnLanded = repairConfig.canBeRepairedOnLanded;
            details.canBeRepairedInFlight = repairConfig.canBeRepairedInFlight;
            details.canBeRepairedOnSplashed = repairConfig.canBeRepairedOnSplashed;

            details.sparePartsRequired = repairConfig.sparePartsRequired;

            return details;
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
            return false;
        }
        
        /// <summary>
        /// Trigger a repair ATTEMPT of the module's failure.  It is the module's responsability to take care of any consumable resources, data transmission, etc required to perform the repair
        /// </summary>
        /// <returns>Should return true if the failure was repaired, false otherwise</returns>
        public virtual bool AttemptRepair()
        {
            return true;
        }

        
        public override void OnAwake()
        {
            base.OnAwake();
        }
        
        public override void OnStart(StartState state)
        {
            base.OnStart();
        }
        
        public override void OnLoad(ConfigNode node)
        {
            if (node.HasNode("REPAIR"))
            {
                repairConfig = new RepairConfig();
                repairConfig.Load(node.GetNode("REPAIR"));
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

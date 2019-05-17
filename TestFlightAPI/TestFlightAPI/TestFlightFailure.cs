using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestFlightAPI
{
    /// <summary>
    /// This part module provides a specific failure for the TestFlight system
    /// </summary>
    public class TestFlightFailureBase : PartModule, ITestFlightFailure
    {
        [KSPField]
        public string failureType;
        [KSPField]
        public string severity;
        [KSPField]
        public int weight;
        [KSPField]
        public string failureTitle = "Failure";
        [KSPField]
        public string configuration = "";
        [KSPField]
        public float duFail = 0f;
        [KSPField]
        public float duRepair = 0f;
        [KSPField]
        public bool oneShot = false;

        [KSPField(isPersistant=true)]
        public bool failed;


        public bool Failed
        {
            get { return failed; }
            set { failed = value; }
        }

        public bool TestFlightEnabled
        {
            get
            {
                ITestFlightCore core = TestFlightUtil.GetCore(this.part, Configuration);
                if (core != null)
                    return core.TestFlightEnabled;
                return false;
            }
        }
        public string Configuration
        {
            get 
            { 
                if (configuration.Equals(string.Empty))
                    configuration = TestFlightUtil.GetPartName(this.part);

                return configuration; 
            }
            set 
            { 
                configuration = value; 
            }
        }

        protected void Log(string message)
        {
            message = String.Format("TestFlightFailure({0}[{1}]): {2}", Configuration, Configuration, message);
            TestFlightUtil.Log(message, this.part);
        }

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

        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public virtual void DoFailure()
        {
            Failed = true;
            ITestFlightCore core = TestFlightUtil.GetCore(this.part, Configuration);
            if (core != null)
            {
                core.ModifyFlightData(duFail, true);
                FlightLogger.eventLog.Add(String.Format("[{0}] {1} failed: {2}", KSPUtil.PrintTimeCompact((int)Math.Floor(this.vessel.missionTime), false), core.Title, failureTitle));
            }
        }
        
        /// <summary>
        /// Forces the repair.  This should instantly repair the part, regardless of whether or not a normal repair can be done.  IOW if at all possible the failure should fixed after this call.
        /// This is made available as an API method to allow things like failure simulations.
        /// </summary>
        /// <returns><c>true</c>, if failure was repaired, <c>false</c> otherwise.</returns>
        public virtual float ForceRepair()
        {
            return DoRepair();
        }

        public virtual float DoRepair()
        {
            Failed = false;
            ITestFlightCore core = TestFlightUtil.GetCore(this.part, Configuration);
            if (core != null)
                core.ModifyFlightData(duRepair, true);
            return 0;
        }


        public virtual List<string> GetTestFlightInfo()
        {
            return null;
        }

        public override void OnAwake()
        {
            Failed = false;
            base.OnAwake();
        }
        
        public override void OnStart(StartState state)
        {
        }
        
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }
        
        public override void OnSave(ConfigNode node)
        {
            base.OnLoad(node);
        }


        public virtual float GetRepairTime()
        {
            return 0f;
        }
        public virtual float AttemptRepair()
        {
            return 0f;
        }
        public virtual bool CanAttemptRepair()
        {
            return true;
        }

        public virtual string GetModuleInfo()
        {
            return string.Empty;
        }
    }
}

using System;

using UnityEngine;

using KSP;

using TestFlightAPI;
using Contracts;
using ContractConfigurator;
using ContractConfigurator.Parameters;


namespace TestFlightContracts
{

    /// <summary>
    /// Simple timer implementation.
    /// </summary>
    public class FlightData : ContractConfiguratorParameter
    {
        protected double requiredData { get; set; }
        protected double flightData { get; set; }
        protected string requiredScope { get; set; }

        private TitleTracker titleTracker = new TitleTracker();

        public FlightData(double requiredData, string requiredScope)
        {
            this.requiredData = requiredData;
            this.requiredScope = requiredScope;
            disableOnStateChange = false;
        }

        protected override string GetTitle()
        {
            if (flightData >= requiredData)
            {
                return "All data collected";
            }
            else if (flightData < requiredData)
            {
                string title = String.Format("Data remaining: {0:F2}", requiredData - flightData);

                // Add the string that we returned to the titleTracker.  This is used to update
                // the contract title element in the GUI directly, as it does not support dynamic
                // text.
                titleTracker.Add(title);

                return title;
            }
            else
            {
                return String.Format("Required Flight Data: {0:F2}", requiredData);
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("requiredData", requiredData);
            node.AddValue("requiredScope", requiredScope);
            node.AddValue("flightData", flightData);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            requiredData = Convert.ToDouble(node.GetValue("duration"));
            flightData = Convert.ToDouble(node.GetValue("endTime"));
            requiredScope = node.GetValue("requiredScope");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected void OnContractAccepted(Contract contract)
        {
            // Set the end time
            if (contract == Root)
            {
                SetState(ParameterState.Incomplete);
                flightData = 0;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Every time the clock ticks over, make an attempt to update the contract window
            // title.  We do this because otherwise the window will only ever read the title once,
            // so this is the only way to get our fancy timer to work.
//            if (Planetarium.GetUniversalTime() - lastUpdate > 1.0f)
//            {
//                // Boom!
//                if (Planetarium.GetUniversalTime() > endTime)
//                {
//                    SetState(ParameterState.Failed);
//                }
//                lastUpdate = Planetarium.GetUniversalTime();
//
//                titleTracker.UpdateContractWindow(this, GetTitle());
//            }
        }
    }

    /// <summary>
    /// ParameterFactory wrapper for FlightData ContractParameter.
    /// </summary>
    public class TFFlightDataFactory : ParameterFactory
    {
        protected double requiredData;
        protected string requiredScope;
        protected string requiredPartQuery;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get requiredData
            string requiredDataStr = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "requiredData", x => requiredDataStr = x, this, "");
            if (requiredDataStr != null)
            {
                requiredData = requiredDataStr != "" ? DurationUtil.ParseDuration(requiredDataStr) : 0.0;
            }
            // Get requiredScope
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "requiredScope", x => this.requiredScope = x, this, "");
            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new FlightData(requiredData, requiredScope);
        }
    }
}


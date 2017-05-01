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
    public class CollectFlightData : VesselParameter
    {
        protected float requiredData { get; set; }
        protected float flightData { get; set; }
        protected string partName { get; set; }

        private double lastUpdate = 0;
        private float lastData = 0f;

        private TitleTracker titleTracker;

        public CollectFlightData(float requiredData, string partName)
        {
            this.requiredData = requiredData;
            this.partName = partName;
            disableOnStateChange = false;
            titleTracker = new TitleTracker(this);
        }

        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            return true;
        }

        protected override string GetTitle()
        {
            if (flightData >= requiredData)
            {
                return "All data collected";
            }
            else if (flightData > 0 && flightData < requiredData)
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
                return String.Format("Flight Data: {1}: {0:F2}", requiredData, partName);
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("requiredData", requiredData);
            node.AddValue("flightData", flightData);
            node.AddValue("lastData", lastData);
            node.AddValue("partName", partName);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            requiredData = float.Parse(node.GetValue("requiredData"));
            flightData = float.Parse(node.GetValue("flightData"));
            lastData = float.Parse(node.GetValue("lastData"));
            partName = node.GetValue("partName");
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
            if (contract == Root)
            {
                SetState(ParameterState.Incomplete);
                flightData = 0;
                TestFlightPartData partData = TestFlightUtil.GetPartDataForPart(partName);
                if (partData != null)
                    lastData = float.Parse(partData.GetValue("flightData"));
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            float partCurrentFlightData;
            float newFlightData;

            TestFlightPartData partData = TestFlightUtil.GetPartDataForPart(partName);
            if (partData == null)
                return;

            if (Planetarium.GetUniversalTime() - lastUpdate > 1.0f)
            {
                partCurrentFlightData = float.Parse(partData.GetValue("flightData"));
                newFlightData = partCurrentFlightData - lastData;
                lastData = partCurrentFlightData;
                lastUpdate = Planetarium.GetUniversalTime();

                if (ReadyToComplete())
                {
                    flightData = flightData + newFlightData;    
                    titleTracker.UpdateContractWindow(GetTitle());
                }
            }
        }
    }

    /// <summary>
    /// ParameterFactory wrapper for FlightData ContractParameter.
    /// </summary>
    public class CollectFlightDataFactory : ParameterFactory
    {
        protected float data;
        protected string part;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "data", x => data = x, this, 0f);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "part", x => part = x, this, "+");
            return valid;
        }

        public bool ValidatePartQuery(string partQuery)
        {
            string part = SelectPart(partQuery);
            if (String.IsNullOrEmpty(part.Trim()))
                return false;
            return true;
        }

        public string SelectPart(string partQuery)
        {
            // Part can be specified directly wth a part name, or through a query expression to dynamically select a part at runtime
            // Lists of parts to choose from can be specified by a comma seperated list
            // the + character prepended means choose the part from the list with the most data
            // the - character prepended means choose the part from the list with the least data
            // the ! character prepended means choose the first part from the list with no data
            // The + and - options can also be used without a list, in which case they mean simply choose the part with the most or least data of all parts
            // If no operator is specified, then a random part from the list is chosen
            if (partQuery.Contains(","))
            {
                // we have a list
                if (partQuery[0] == '+')
                {
                    string newQuery = partQuery.Substring(1);
                    float maxData = 0f;
                    string maxPart = "";
                    TestFlightPartData partData = null;
                    string[] parts = newQuery.Split(new char[1]{ ',' });
                    foreach (string partName in parts)
                    {
                        partData = TestFlightUtil.GetPartDataForPart(partName);
                        if (partData == null)
                            continue;
                        float partFlightData = float.Parse(partData.GetValue("flightData"));
                        if (partFlightData > maxData)
                        {
                            maxData = partFlightData;
                            maxPart = partData.PartName;
                        }
                    }
                    return maxPart;
                }
                else if (partQuery[0] == '-')
                {
                    string newQuery = partQuery.Substring(1);
                    float minData = float.MaxValue;
                    string minPart = "";
                    TestFlightPartData partData = null;
                    string[] parts = newQuery.Split(new char[1]{ ',' });
                    foreach (string partName in parts)
                    {
                        partData = TestFlightUtil.GetPartDataForPart(partName);
                        if (partData == null)
                            continue;
                        float partFlightData = float.Parse(partData.GetValue("flightData"));
                        if (partFlightData < minData)
                        {
                            minData = partFlightData;
                            minPart = partData.PartName;
                        }
                    }
                    return minPart;
                }
                else if (partQuery[0] == '!')
                {
                    return TestFlightUtil.PartWithNoData(partQuery.Substring(1));
                }
                else
                {
                    // TODO not yet implemented
                    return "";
                }
            }
            else
            {
                // no list, so this is either a direct part name, or a simple +/- operator
                if (partQuery == "+")
                    return TestFlightUtil.PartWithMostData();
                else if (partQuery == "-")
                    return TestFlightUtil.PartWithLeastData();
                else
                {
                    TestFlightPartData partData = TestFlightUtil.GetPartDataForPart(partQuery);
                    if (partData == null)
                        return "";
                    return partQuery;
                }
            }
        }

        public override ContractParameter Generate(Contract contract)
        {
            string selectedPart = SelectPart(part);
            if (String.IsNullOrEmpty(selectedPart.Trim()))
            {
                return null;
            }
            
            return new CollectFlightData(data, selectedPart);
        }
    }
}


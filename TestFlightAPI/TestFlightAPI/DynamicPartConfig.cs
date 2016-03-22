using System;

namespace TestFlightAPI
{
    public class DynamicPartConfig : ITestFlightConfigNode
    {
        #region KSPFields
        public string partFilter = "";
        public string partName = "";
        public string partTitle = "";
        public bool partEnabled = false;
        #endregion

        #region ITestFlightConfigNode implementation

        public string PartFilter
        {
            get
            {
                return partFilter;
            }
            set
            {
                partFilter = value;
            }
        }

        public string PartName
        {
            get
            {
                return partName;
            }
            set
            {
                partName = value;
            }
        }

        public string PartTitle
        {
            get
            {
                return partTitle;
            }
            set
            {
                partTitle = value;
            }
        }

        public bool PartEnabled
        {
            get
            {
                return partEnabled;
            }
            set
            {
                partEnabled = value;
            }
        }
            
        #endregion
    }
}


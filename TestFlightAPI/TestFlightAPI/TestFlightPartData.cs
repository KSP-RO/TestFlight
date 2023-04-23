using System;
using System.Collections.Generic;

namespace TestFlightAPI
{
    public class TestFlightPartData : IConfigNode
    {
        [Persistent] public string partName;
        [Persistent] public float flightData;
        [Persistent] public float transferData;
        [Persistent] public float researchData;
        [Persistent] public float flightTime;
        private readonly Dictionary<string, string> partData = new Dictionary<string, string>();

        public string PartName
        {
            get { return partName; }
            set { partName = value; }
        }

        public TestFlightPartData()
        {
        }

        public string GetValue(string key)
        {
            string res;
            return partData.TryGetValue(key.ToLowerInvariant(), out res) ? res : string.Empty;
        }

        public double GetDouble(string key)
        {
            string res = GetValue(key);
            double returnValue = 0;
            if (!string.IsNullOrEmpty(res))
                double.TryParse(res, out returnValue);
            return returnValue;
        }

        public float GetFloat(string key)
        {
            string res = GetValue(key);
            float returnValue = 0;
            if (!string.IsNullOrEmpty(res))
                float.TryParse(res, out returnValue);
            return returnValue;
        }

        public bool GetBool(string key)
        {
            string res = GetValue(key);
            bool returnValue = false;
            if (!string.IsNullOrEmpty(res))
                bool.TryParse(res, out returnValue);
            return returnValue;
        }

        public int GetInt(string key)
        {
            string res = GetValue(key);
            int returnValue = 0;
            if (!string.IsNullOrEmpty(res))
                int.TryParse(res, out returnValue);
            return returnValue;
        }

        public void SetValue(string key, object value)
        {
            key = key.ToLowerInvariant();
            partData[key] = value.ToString();
        }

        public void AddValue(string key, float value)
        {
            float existingValue = GetFloat(key);
            SetValue(key, existingValue + value);
        }

        public void AddValue(string key, int value)
        {
            int existingValue = GetInt(key);
            SetValue(key, existingValue + value);
        }

        public void ToggleValue(string key, bool _=false)
        {
            bool existingValue = GetBool(key);
            SetValue(key, !existingValue);
        }

        public void AddValue(string key, double value)
        {
            double existingValue = GetDouble(key);
            SetValue(key, existingValue + value);
        }

        private void DecodeRawPartData(string data)
        {
            var colonSep = new char[1] { ':' };
            string[] propertyGroups = data.Split(new char[1]{ ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string propertyGroup in propertyGroups)
            {
                ExtractRawDataToKnownFields(propertyGroup.Split(colonSep));
            }
        }

        private string EncodeRawPartData(Dictionary<string,string> data)
        {
            var sb = StringBuilderCache.Acquire();
            foreach (var entry in data)
                sb.Append($"{entry.Key}:{entry.Value},");
            return sb.ToStringAndRelease();
        }

        public void Load(ConfigNode node)
        {
            partData.Clear();
            string rawPartdata = node.GetValue("partData");
            ConfigNode.LoadObjectFromConfig(this, node);
            DecodeRawPartData(rawPartdata);
        }

        public void Save(ConfigNode node)
        {
            var tempData = new Dictionary<string, string>(partData);
            tempData.Add(nameof(flightData).ToLowerInvariant(), $"{flightData}");
            tempData.Add(nameof(transferData).ToLowerInvariant(), $"{transferData}");
            tempData.Add(nameof(researchData).ToLowerInvariant(), $"{researchData}");
            tempData.Add(nameof(flightTime).ToLowerInvariant(), $"{flightTime}");
            node.AddValue("partData", EncodeRawPartData(tempData));
            var n = ConfigNode.CreateConfigFromObject(this);
            node.AddData(n);
        }

        private void ExtractRawDataToKnownFields(string[] kvp)
        {
            if (kvp[0].Equals(nameof(flightData), StringComparison.OrdinalIgnoreCase))
                flightData = float.Parse(kvp[1]);
            else if (kvp[0].Equals(nameof(researchData), StringComparison.OrdinalIgnoreCase))
                researchData = float.Parse(kvp[1]);
            else if (kvp[0].Equals(nameof(transferData), StringComparison.OrdinalIgnoreCase))
                transferData = float.Parse(kvp[1]);
            else if (kvp[0].Equals(nameof(flightTime), StringComparison.OrdinalIgnoreCase))
                flightTime = float.Parse(kvp[1]);
            else
                SetValue(kvp[0], kvp[1]);
        }
    }
}
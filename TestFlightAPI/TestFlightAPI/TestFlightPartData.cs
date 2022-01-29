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
        private string rawPartdata;
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
            if (partData.ContainsKey(key))
                partData[key] = value.ToString();
            else
                partData.Add(key, value.ToString());
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

        private void DecodeRawPartData()
        {
            var colonSep = new char[1] { ':' };
            string[] propertyGroups = rawPartdata.Split(new char[1]{ ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string propertyGroup in propertyGroups)
            {
                ExtractRawDataToKnownFields(propertyGroup.Split(colonSep));
            }
        }

        private void EncodeRawPartData()
        {
            var sb = StringBuilderCache.Acquire();
            foreach (var entry in partData)
                sb.Append($"{entry.Key}:{entry.Value},");
            rawPartdata = sb.ToStringAndRelease();
        }

        public void Load(ConfigNode node)
        {
            partData.Clear();
            rawPartdata = node.GetValue("partData");
            ConfigNode.LoadObjectFromConfig(this, node);
            DecodeRawPartData();
        }

        public void Save(ConfigNode node)
        {
            EncodeRawPartData();
            node.AddValue("partData", rawPartdata);
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
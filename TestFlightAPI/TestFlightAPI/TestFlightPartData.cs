using System;
using System.Collections.Generic;

using UnityEngine;

namespace TestFlightAPI
{
    public class TestFlightPartData : IConfigNode
    {
        private string partName;
        private string rawPartdata;
        private Dictionary<string, string> partData;

        public string PartName
        {
            get { return partName; }
            set { partName = value; }
        }

        public TestFlightPartData()
        {
            InitDataStore();            
        }

        private void InitDataStore()
        {
            if (partData == null)
                partData = new Dictionary<string, string>();
            else
                partData.Clear();
        }

        public string GetValue(string key)
        {
            key = key.ToLowerInvariant();
            if (partData.ContainsKey(key))
                return partData[key];
            else
                return "";
        }

        public void AddValue(string key, string value)
        {
            key = key.ToLowerInvariant();
            if (partData.ContainsKey(key))
                partData[key] = value;
            else
                partData.Add(key, value);
        }

        private void decodeRawPartData()
        {
            string[] propertyGroups = rawPartdata.Split(new char[1]{ ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string propertyGroup in propertyGroups)
            {
                string[] keyValuePair = propertyGroup.Split(new char[1]{ ':' });
                AddValue(keyValuePair[0], keyValuePair[1]);
            }
        }

        private void encodeRawPartData()
        {
            rawPartdata = "";
            foreach (var entry in partData)
            {
                rawPartdata += String.Format("{0}:{1},", entry.Key, entry.Value);
            }
        }

        public void Load(ConfigNode node)
        {
            InitDataStore();            
            partName = node.GetValue("partName");
            rawPartdata = node.GetValue("partData");
            decodeRawPartData();
        }

        public void Save(ConfigNode node)
        {
            encodeRawPartData();
            node.AddValue("partName", partName);
            node.AddValue("partData", rawPartdata);
        }
    }
}
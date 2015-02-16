using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace TestFlightAPI
{
    public class TestFlightInterop : PartModule, ITestFlightInterop
    {
        private Dictionary<String, InteropValue> knownInterops;

        public bool AddInteropValue(string name, string value, string owner)
        {
            if (!RemoveInteropValue(name, owner))
                return false;

            InteropValue opValue = new InteropValue();
            opValue.owner = owner;
            opValue.value = value;
            opValue.valueType = InteropValueType.STRING;

            Debug.Log("Added new interop " + name + " = " + value + ", for " + owner);

            return true;
        }
        public bool AddInteropValue(string name, int value, string owner)
        {
            if (!RemoveInteropValue(name, owner))
                return false;

            InteropValue opValue = new InteropValue();
            opValue.owner = owner;
            opValue.value = String.Format("{0:D}",value);
            opValue.valueType = InteropValueType.INT;

            Debug.Log("Added new interop " + name + " = " + value + ", for " + owner);

            return true;
        }
        public bool AddInteropValue(string name, float value, string owner)
        {
            if (!RemoveInteropValue(name, owner))
                return false;

            InteropValue opValue = new InteropValue();
            opValue.owner = owner;
            opValue.value = String.Format("{0:F}",value);
            opValue.valueType = InteropValueType.FLOAT;

            Debug.Log("Added new interop " + name + " = " + value + ", for " + owner);

            return true;
        }
        public bool AddInteropValue(string name, bool value, string owner)
        {
            if (!RemoveInteropValue(name, owner))
                return false;

            InteropValue opValue = new InteropValue();
            opValue.owner = owner;
            opValue.value = String.Format("{0:D}",value);
            opValue.valueType = InteropValueType.BOOL;

            Debug.Log("Added new interop " + name + " = " + value + ", for " + owner);

            return true;
        }
        public bool RemoveInteropValue(string name, string owner)
        {
            if (knownInterops == null)
                knownInterops = new Dictionary<string, InteropValue>();

            if (!knownInterops.ContainsKey(name))
                return true;

            InteropValue opValue = knownInterops[name];
            if (opValue.owner != owner)
                return false;

            knownInterops.Remove(name);
            return true;
        }
        public void ClearInteropValues(string owner)
        {
            List<String> keysToDelete = new List<string>();

            foreach (string key in knownInterops.Keys)
            {
                if (knownInterops[key].owner == owner)
                    keysToDelete.Add(key);
            }

            if (keysToDelete.Count > 0)
            {
                foreach (string key in keysToDelete)
                {
                    knownInterops.Remove(key);
                }
            }
        }
        public InteropValue GetInterop(string name)
        {
            if (knownInterops.ContainsKey(name))
                return knownInterops[name];
            else
            {
                InteropValue returnVal = new InteropValue();
                returnVal.valueType = InteropValueType.INVALID;
                return returnVal;
            }
        }
    }
}


using System;
using System.Collections.Generic;

using KSP;
using UnityEngine;
using TestFlightCore.KSPPluginFramework;

namespace TestFlightCore
{
    public class BodySettings : ConfigNodeStorage
    {
        public BodySettings(String FilePath) : base(FilePath) {

        }

        [Persistent] public Dictionary<String, String> bodyAliases = new Dictionary<string, string>();
    }
}


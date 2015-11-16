using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_AvionicsClamp : TestFlightFailureBase_Avionics
    {
        public override float Calculate(float value)
        {
            return Mathf.Clamp(value, -this.failedValue, this.failedValue);
        }
    }
}
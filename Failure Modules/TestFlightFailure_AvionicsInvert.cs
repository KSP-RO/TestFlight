using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_AvionicsInvert : TestFlightFailureBase_Avionics
    {
        public override float Calculate(float value)
        {
            return -value;
        }
    }
}
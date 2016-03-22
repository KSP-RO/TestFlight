using System;

namespace TestFlightAPI
{
    public interface ITestFlightConfigNode
    {
        string PartFilter { get; set; }

        string PartName { get; set; }

        string PartTitle { get; set; }

        bool PartEnabled { get; set; }
    }
}


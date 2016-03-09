using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TestFlightAPI;

namespace TestFlight
{
    public class TestFlightFailureBase_Science : TestFlightFailureBase
    {
        [KSPField]
        public string experimentID = "";

        protected ModuleScienceExperiment module;
        public override void OnAwake()
        {
            base.OnAwake();
            List<ModuleScienceExperiment> list = base.part.Modules.OfType<ModuleScienceExperiment>().ToList();
            if (list != null)
            {
                for (int i=0; i<list.Count; i++)
                {
                    if (list[i].experimentID == this.experimentID)
                    {
                        this.module = list[i];
                        return;
                    }
                }
            }

        }
    }
}

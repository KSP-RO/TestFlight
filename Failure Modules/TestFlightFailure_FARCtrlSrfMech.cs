using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestFlight.Failure_Modules
{
    public class TestFlightFailure_FARCtrlSrfMech : TestFlightFailureBase_FARCtrlSrf
    {
        [KSPField(isPersistant = true)]
        public float pitchaxis;
        [KSPField(isPersistant = true)]
        public float rollaxis;
        [KSPField(isPersistant = true)]
        public float yawaxis;
        [KSPField(isPersistant = true)]
        public float pitchaxisDueToAoA;
        [KSPField(isPersistant = true)]
        public float brakeRudder;
        [KSPField(isPersistant = true)]
        public float maxdeflect;

        public override void DoFailure()
        {
            base.DoFailure();

            SetSymState(UI_Scene.Editor);
            SetField("pitchaxis", false, 0);
            SetField("rollaxis", false, 0);
            SetField("yawaxis", false, 0);
            SetField("pitchaxisDueToAoA", false, -100);
            SetField("brakeRudder", false, 0);
            SetField("maxdeflect", false, 40);
        }
        public override float DoRepair()
        {
            base.DoRepair();
            SetSymState(UI_Scene.All);
            SetField("pitchaxis", true, this.pitchaxis);
            SetField("rollaxis", true, this.rollaxis);
            SetField("yawaxis", true, this.yawaxis);
            SetField("pitchaxisDueToAoA", true, this.pitchaxisDueToAoA);
            SetField("brakeRudder", true, this.brakeRudder);
            SetField("maxdeflect", true, this.maxdeflect);
            return 0f;
        }
        private void SetField(string name, bool state, float value)
        {
            BaseField mf = base.module.Fields[name];
            BaseField tf = this.Fields[name];
            UI_Scene ui = (state ? UI_Scene.All : UI_Scene.Editor);

            mf.uiControlFlight.affectSymCounterparts = ui;
            mf.guiActive = state;
            if (!state)
            {
                tf.SetValue(mf.GetValue(mf.host), tf.host);
            }
            mf.SetValue(value, mf.host);
        }
        private void SetSymState(UI_Scene state)
        {
            for (int i = 0; i < base.part.symmetryCounterparts.Count; i++)
            {
                Part p = base.part.symmetryCounterparts[i];
                for (int j = 0; j < p.Modules.Count; j++)
                {
                    PartModule m = p.Modules[j];
                    if (m.moduleName == "FARControllableSurface")
                    {
                        m.Fields["pitchaxis"].uiControlFlight.affectSymCounterparts = state;
                        m.Fields["rollaxis"].uiControlFlight.affectSymCounterparts = state;
                        m.Fields["yawaxis"].uiControlFlight.affectSymCounterparts = state;
                        m.Fields["pitchaxisDueToAoA"].uiControlFlight.affectSymCounterparts = state;
                        m.Fields["brakeRudder"].uiControlFlight.affectSymCounterparts = state;
                        m.Fields["maxdeflect"].uiControlFlight.affectSymCounterparts = state;
                    }
                }
            }
        }
    }
}

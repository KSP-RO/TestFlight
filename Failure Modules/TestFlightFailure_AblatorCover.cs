using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestFlightAPI;
using KSP;

namespace TestFlight
{
    public class TestFlightFailure_AblatorCover : TestFlightFailureBase
    {
        [KSPField]
        public int minDegradation = 5;
        [KSPField]
        public int maxDegradation = 10;

        private double baseConductivity;
        private double ablatorConductivity;
        private ModuleAblator ablator;
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            this.baseConductivity = this.part.heatConductivity;
            this.ablator = this.part.FindModuleImplementing<ModuleAblator>();
            if (this.ablator != null)
            {
                this.ablatorConductivity = this.ablator.reentryConductivity;
            }
        }
        public override void DoFailure()
        {
            base.DoFailure();
            if (this.ablator != null)
            {
                double total = this.baseConductivity - this.ablatorConductivity;
                double remains = total - (this.ablator.reentryConductivity - this.ablatorConductivity);
                if (remains > 0)
                {
                    Random ran = new Random();
                    double degrade = (double)ran.Next(this.minDegradation, this.maxDegradation + 1) * 0.01;
                    this.ablator.reentryConductivity += Math.Min(remains, total * degrade);
                    if (this.ablator.ablativeResource != string.Empty && base.part.Resources.Contains(this.ablator.ablativeResource))
                    {
                        PartResource res = base.part.Resources[this.ablator.ablativeResource];
                        res.amount -= res.amount * degrade;
                    }
                }
            }
        }
        public override float DoRepair()
        {
            base.DoRepair();
            if (this.ablator != null)
            {
                this.ablator.reentryConductivity = this.ablatorConductivity;
            }
            return 0F;
        }
    }
}
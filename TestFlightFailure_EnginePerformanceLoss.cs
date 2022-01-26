namespace TestFlight
{
    public class TestFlightFailure_EnginePerformanceLoss : TestFlightFailure_Engine
    {
        [KSPField]
        public float ispMultiplier = 0.7f;
        [KSPField]
        public float ispMultiplierJitter = 0.1f;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (Failed)
                DoFailure();
        }
        /// <summary>
        /// Triggers the failure controlled by the failure module
        /// </summary>
        public override void DoFailure()
        {
            base.DoFailure();
            // for each engine change its specific impulse
            foreach (EngineHandler engine in engines)
            {
                float jitter = ispMultiplierJitter - ((float)core.RandomGenerator.NextDouble() * (ispMultiplierJitter * 2));
                float actualMultiplier = ispMultiplier + jitter;
                engine.engine.SetFuelIspMult(actualMultiplier);
                engine.engine.failMessage = failureTitle;
                engine.engine.failed = true;
            }
        }

        public override float DoRepair()
        {
            base.DoRepair();
            // for each engine restore its specific impulse back to 1.0
            foreach (EngineHandler engine in engines)
            {
                engine.engine.SetFuelIspMult(1.0f);
                engine.engine.failMessage = "";
                engine.engine.failed = false;
            }
            return 0;
        }
        public override void SetActiveConfig(string alias)
        {
            base.SetActiveConfig(alias);
            
            if (currentConfig == null) return;

            // update current values with those from the current config node
            currentConfig.TryGetValue("ispMultiplier", ref ispMultiplier);
            currentConfig.TryGetValue("ispMultiplierJitter", ref ispMultiplierJitter);
        }
    }
}


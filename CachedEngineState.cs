namespace TestFlight
{
    public class CachedEngineState : IConfigNode
    {
        [Persistent] public bool allowShutdown;
        [Persistent] public bool allowRestart;
        [Persistent] public int numIgnitions;

        public CachedEngineState() { }

        public CachedEngineState(ConfigNode node)
        {
            Load(node);
        }

        public CachedEngineState(EngineModuleWrapper engine)
        {
            allowShutdown = engine.allowShutdown;
            allowRestart = engine.allowRestart;
            numIgnitions = engine.GetIgnitionCount();
        }

        public void Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
        }

        public void Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, node);
        }
    }
}

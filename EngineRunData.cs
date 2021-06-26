namespace TestFlight
{
    public class EngineRunData : IConfigNode
    {
        public uint id;
        public bool hasBeenRun;
        public float timeSinceLastShutdown;

        public EngineRunData(uint id)
        {
            this.id = id;
        }

        public EngineRunData(ConfigNode node)
        {
            Load(node);
        }

        public void Load(ConfigNode node)
        {
            node.TryGetValue("id", ref id);
            node.TryGetValue("hasBeenRun", ref hasBeenRun);
            node.TryGetValue("timeSinceLastShutdown", ref timeSinceLastShutdown);
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("id", id);
            node.AddValue("hasBeenRun", hasBeenRun);
            node.AddValue("timeSinceLastShutdown", timeSinceLastShutdown);
        }
    }
}

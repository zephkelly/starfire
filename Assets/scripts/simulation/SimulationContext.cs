namespace Starfire.Sim
{
    public class SimulationContext
    {
        public double SimulationTime { get; set; }
        public float DeltaTime { get; set; }

        // Services here... EntityManager, EventManager, etc.
    }
}
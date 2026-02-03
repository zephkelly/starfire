namespace Starfire.Core.V2.World.Simulation.Events
{
    /// <summary>
    /// Types of events that can occur in the background simulation.
    /// </summary>
    public enum SimulationEventType
    {
        /// <summary>Two entities collided (may or may not have destruction).</summary>
        Collision,

        /// <summary>An entity was destroyed by collision.</summary>
        Destruction,

        /// <summary>Debris was spawned from a destruction event.</summary>
        DebrisSpawned,

        /// <summary>An entity crossed a chunk boundary.</summary>
        ChunkTransition
    }
}

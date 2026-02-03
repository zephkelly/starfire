namespace Starfire.Core.V2.World.Simulation.Events
{
    /// <summary>
    /// Outcome of a collision between two entities.
    /// </summary>
    public enum CollisionResult
    {
        /// <summary>Both entities survived and bounced.</summary>
        Bounced,

        /// <summary>Entity A was destroyed by the collision.</summary>
        EntityADestroyed,

        /// <summary>Entity B was destroyed by the collision.</summary>
        EntityBDestroyed,

        /// <summary>Both entities were destroyed by the collision.</summary>
        BothDestroyed
    }
}

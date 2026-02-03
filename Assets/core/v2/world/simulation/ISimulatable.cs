using Starfire.Core.V2.World.Chunk;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Interface for entities that can be simulated in the background simulation system.
    /// Defines the core physics properties required for Tier 1 active simulation and
    /// Tier 2 ballistic prediction.
    /// </summary>
    public interface ISimulatable
    {
        /// <summary>Unique identifier for this entity.</summary>
        int EntityId { get; }

        /// <summary>Type of entity (Asteroid, Ship, Projectile, etc.).</summary>
        EntityType EntityType { get; }

        /// <summary>Absolute position in double-precision world space.</summary>
        Vector2D AbsolutePosition { get; set; }

        /// <summary>Velocity in world units per second.</summary>
        Vector2D Velocity { get; set; }

        /// <summary>Rotation in degrees.</summary>
        float Rotation { get; set; }

        /// <summary>Angular velocity in degrees per second.</summary>
        float AngularVelocity { get; set; }

        /// <summary>Mass for physics calculations.</summary>
        float Mass { get; }

        /// <summary>Collision radius.</summary>
        float Radius { get; }

        /// <summary>Linear drag coefficient.</summary>
        float Drag { get; }

        /// <summary>Chunk where this entity was originally generated (procedural origin).</summary>
        ChunkCoord OriginChunk { get; }

        /// <summary>Chunk where this entity is currently located.</summary>
        ChunkCoord CurrentChunk { get; set; }

        /// <summary>Whether this entity has been modified from its procedural baseline.</summary>
        bool HasBeenModified { get; set; }

        /// <summary>
        /// Entity ID of whoever originally caused this entity to move.
        /// Used for blame tracking in collision chains. Null if movement is from natural causes.
        /// </summary>
        int? InstigatorEntityId { get; set; }
    }
}

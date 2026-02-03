using System;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Lightweight representation of an entity being simulated outside of Unity physics.
    /// Uses absolute (double-precision) coordinates for position tracking.
    /// </summary>
    [Serializable]
    public class SimulatedEntity
    {
        public int EntityId;
        public EntityType EntityType;

        /// <summary>Chunk this entity is currently located in.</summary>
        public ChunkCoord CurrentChunk;

        /// <summary>Chunk where this entity was originally generated (procedural origin).</summary>
        public ChunkCoord OriginChunk;

        /// <summary>Absolute position in double-precision world space.</summary>
        public Vector2D AbsolutePosition;

        /// <summary>Velocity in world units per second.</summary>
        public Vector2D Velocity;

        public float Rotation;
        public float AngularVelocity;

        // Physical properties
        public float Mass;
        public float Radius;
        public float Drag;

        /// <summary>Which simulation tier this entity is currently in.</summary>
        public SimulationTier CurrentTier;

        /// <summary>Game time (Time.timeAsDouble) when this entity was last actively simulated.</summary>
        public double LastSimulationTime;

        /// <summary>Whether this entity has been modified from its procedural baseline.</summary>
        public bool HasBeenModified;

        /// <summary>
        /// Variant index for visual representation (maps to prefab array).
        /// </summary>
        public int Variant;

        /// <summary>
        /// Procedural seed for regeneration.
        /// </summary>
        public float Seed;

        /// <summary>
        /// Source type for asteroids (belt, ring, scatter).
        /// </summary>
        public int SourceType;
    }
}

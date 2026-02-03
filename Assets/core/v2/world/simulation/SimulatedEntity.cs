using System;
using System.Collections.Generic;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Lightweight representation of an entity being simulated outside of Unity physics.
    /// Uses absolute (double-precision) coordinates for position tracking.
    /// Implements ISimulatable for generic background simulation support.
    /// </summary>
    [Serializable]
    public class SimulatedEntity : ISimulatable
    {
        // ── ISimulatable Implementation ─────────────────────────────────────

        int ISimulatable.EntityId => EntityId;
        EntityType ISimulatable.EntityType => EntityType;

        Vector2D ISimulatable.AbsolutePosition
        {
            get => AbsolutePosition;
            set => AbsolutePosition = value;
        }

        Vector2D ISimulatable.Velocity
        {
            get => Velocity;
            set => Velocity = value;
        }

        float ISimulatable.Rotation
        {
            get => Rotation;
            set => Rotation = value;
        }

        float ISimulatable.AngularVelocity
        {
            get => AngularVelocity;
            set => AngularVelocity = value;
        }

        float ISimulatable.Mass => Mass;
        float ISimulatable.Radius => Radius;
        float ISimulatable.Drag => Drag;

        ChunkCoord ISimulatable.OriginChunk => OriginChunk;

        ChunkCoord ISimulatable.CurrentChunk
        {
            get => CurrentChunk;
            set => CurrentChunk = value;
        }

        bool ISimulatable.HasBeenModified
        {
            get => HasBeenModified;
            set => HasBeenModified = value;
        }

        int? ISimulatable.InstigatorEntityId
        {
            get => InstigatorEntityId;
            set => InstigatorEntityId = value;
        }

        // ── Core Fields ─────────────────────────────────────────────────────

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
        /// Entity ID of whoever originally caused this entity to move (e.g., player who pushed it).
        /// Used for blame tracking in collision chains. Null if movement is from natural causes.
        /// </summary>
        public int? InstigatorEntityId;

        // ── Asteroid-Specific Fields (Backward Compatibility) ───────────────

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

        // ── Extensible Type-Specific Data ───────────────────────────────────

        /// <summary>
        /// Dictionary for storing type-specific data (ships, projectiles, etc.).
        /// Use extension methods in SimulatedEntityExtensions for typed access.
        /// Not serialized by default - use SerializedTypeData for persistence.
        /// </summary>
        [NonSerialized]
        public Dictionary<string, object> TypeData;

        /// <summary>
        /// Serialized form of TypeData for save/load.
        /// </summary>
        public byte[] SerializedTypeData;

        /// <summary>
        /// Ensures TypeData dictionary is initialized.
        /// </summary>
        public void EnsureTypeData()
        {
            TypeData ??= new Dictionary<string, object>();
        }

        /// <summary>
        /// Reset the entity for pooling reuse.
        /// </summary>
        public void Reset()
        {
            EntityId = 0;
            EntityType = EntityType.Unknown;
            CurrentChunk = default;
            OriginChunk = default;
            AbsolutePosition = Vector2D.Zero;
            Velocity = Vector2D.Zero;
            Rotation = 0f;
            AngularVelocity = 0f;
            Mass = 0f;
            Radius = 0f;
            Drag = 0f;
            CurrentTier = SimulationTier.None;
            LastSimulationTime = 0;
            HasBeenModified = false;
            InstigatorEntityId = null;
            Variant = 0;
            Seed = 0f;
            SourceType = 0;
            TypeData?.Clear();
            SerializedTypeData = null;
        }
    }
}

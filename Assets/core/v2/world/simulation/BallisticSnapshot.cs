using System;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Snapshot of an entity's state at a specific point in time.
    /// Used for analytical position prediction without per-frame simulation.
    /// </summary>
    [Serializable]
    public class BallisticSnapshot
    {
        public int EntityId;
        public EntityType EntityType;
        public ChunkCoord OriginChunk;

        /// <summary>Game time (Time.timeAsDouble) when this snapshot was taken.</summary>
        public double SnapshotTime;

        public Vector2D Position;
        public Vector2D Velocity;
        public float Rotation;
        public float AngularVelocity;

        public float Mass;
        public float Radius;
        public float Drag;

        // Visual reconstruction data
        public int Variant;
        public float Seed;
        public int SourceType;
    }
}

using System;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Simulation;
using StarfireV2;

namespace Starfire.Core.V2.World.Consumers
{
    /// <summary>
    /// Interface for runtime entity trackers that monitor entity state changes and chunk boundary crossings.
    /// Implementations attach to spawned GameObjects and handle the lifecycle of entities
    /// as they move between chunks and transition to/from background simulation.
    /// </summary>
    public interface IRuntimeEntityTracker
    {
        /// <summary>Unique identifier for this entity.</summary>
        int EntityId { get; }

        /// <summary>Type of entity being tracked.</summary>
        EntityType EntityType { get; }

        /// <summary>Chunk where this entity was originally spawned (procedural origin).</summary>
        ChunkCoord OriginChunk { get; }

        /// <summary>Chunk where this entity currently resides.</summary>
        ChunkCoord CurrentChunk { get; }

        /// <summary>Whether this entity has been modified from its procedural baseline.</summary>
        bool HasBeenModified { get; }

        /// <summary>The procedural definition of this entity for save/restore matching.</summary>
        ISimulatableDefinition Definition { get; }

        /// <summary>
        /// Raised when the entity needs to be destroyed/pooled
        /// (e.g., when it escapes into an unloaded chunk).
        /// </summary>
        event Action<IRuntimeEntityTracker> OnRequestDestroy;

        /// <summary>
        /// Raised when the entity crosses from one loaded chunk to another.
        /// Parameters: tracker, oldChunk, newChunk
        /// </summary>
        event Action<IRuntimeEntityTracker, ChunkCoord, ChunkCoord> OnChunkMigration;

        /// <summary>
        /// Raised when the entity's state is modified (e.g., after a collision).
        /// </summary>
        event Action<IRuntimeEntityTracker> OnModified;

        /// <summary>
        /// Convert this runtime entity to a SimulatedEntity for background simulation.
        /// Called when the entity leaves the active gameplay area.
        /// </summary>
        /// <returns>A SimulatedEntity representation of the current state.</returns>
        SimulatedEntity ToSimulatedEntity();
    }
}

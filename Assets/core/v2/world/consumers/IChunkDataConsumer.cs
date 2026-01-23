using System;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;

namespace Starfire.Core.V2.World.Consumers
{
    /// <summary>
    /// Interface for systems that consume generated chunk data.
    /// Consumers bridge the chunk system to runtime managers
    /// (e.g., NebulaRegionManager, AsteroidManager).
    ///
    /// Register consumers with WorldGenerationService to have them
    /// automatically notified when relevant chunks load/unload.
    /// </summary>
    public interface IChunkDataConsumer
    {
        /// <summary>
        /// Type of ChunkData this consumer handles.
        /// The consumer will only be notified for chunks
        /// that contain data of this type.
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// Called when a chunk with relevant data is loaded.
        /// Create runtime objects based on the chunk data.
        /// </summary>
        /// <param name="chunk">The loaded chunk containing data</param>
        void OnChunkLoaded(Chunk.Chunk chunk);

        /// <summary>
        /// Called when a chunk with relevant data is about to unload.
        /// Destroy any runtime objects created for this chunk.
        /// </summary>
        /// <param name="chunk">The chunk being unloaded</param>
        void OnChunkUnloading(Chunk.Chunk chunk);

        /// <summary>
        /// Called when the world origin is shifted (floating origin).
        /// Update all runtime object positions by the offset.
        /// </summary>
        /// <param name="offset">The shift amount (add to current positions)</param>
        void OnOriginShift(Vector2 offset);

        /// <summary>
        /// Optional per-frame update for consumers that need it.
        /// Called each frame from WorldGenerationService.
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        void Update(float deltaTime);
    }
}

using System;
using Starfire.Core.V2.World.Chunk;

namespace Starfire.Core.V2.World.Generation
{
    /// <summary>
    /// Interface for procedural content generators.
    /// Each generator produces a specific type of ChunkData.
    /// Register generators with WorldGenerationService to have them
    /// automatically invoked when chunks load.
    /// </summary>
    public interface IChunkDataGenerator
    {
        /// <summary>
        /// Priority determines generation order (lower = earlier).
        /// Use consistent priorities:
        /// - 0-10: Terrain/base features
        /// - 10-20: Environment (nebulas, asteroids)
        /// - 20-30: Structures (stations, POIs)
        /// - 30+: Entities (spawners)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Whether this generator is currently enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Type of ChunkData this generator produces.
        /// Used for routing to appropriate consumers.
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// Generate content for a chunk.
        /// Called when a chunk enters the load radius.
        /// Store results via chunk.SetData&lt;T&gt;().
        /// </summary>
        /// <param name="chunk">The chunk to generate content for</param>
        /// <param name="context">Generation context with seeds and utility methods</param>
        void Generate(Chunk.Chunk chunk, ChunkGenerationContext context);

        /// <summary>
        /// Called when a chunk is about to unload.
        /// Use this to clean up any runtime resources
        /// that aren't automatically handled by ChunkData.OnUnload().
        /// </summary>
        /// <param name="chunk">The chunk being unloaded</param>
        void OnChunkUnloading(Chunk.Chunk chunk);
    }
}

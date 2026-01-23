using Starfire.Core.V2.World.Chunk;

namespace Starfire.Core.V2.World.Generation
{
    /// <summary>
    /// Context passed to generators during chunk generation.
    /// Contains information needed for procedural generation.
    /// </summary>
    public class ChunkGenerationContext
    {
        /// <summary>
        /// The master world seed.
        /// </summary>
        public float WorldSeed { get; }

        /// <summary>
        /// The chunk being generated.
        /// </summary>
        public Chunk.Chunk Chunk { get; }

        /// <summary>
        /// Size of chunks in world units.
        /// </summary>
        public float ChunkSize { get; }

        /// <summary>
        /// Current origin offset for floating origin support.
        /// Add this to absolute positions to get Unity world positions.
        /// </summary>
        public Vector2D OriginOffset { get; }

        /// <summary>
        /// Seeded random generator for this chunk.
        /// </summary>
        public System.Random Random { get; }

        public ChunkGenerationContext(
            float worldSeed,
            Chunk.Chunk chunk,
            float chunkSize,
            Vector2D originOffset)
        {
            WorldSeed = worldSeed;
            Chunk = chunk;
            ChunkSize = chunkSize;
            OriginOffset = originOffset;
            Random = ChunkSeed.GetRandom(worldSeed, chunk.Coord);
        }

        /// <summary>
        /// Get a seeded random generator for a specific generator index.
        /// Use consistent indices for deterministic results.
        /// </summary>
        public System.Random GetGeneratorRandom(int generatorIndex)
        {
            return ChunkSeed.GetGeneratorRandom(Chunk.GenerationSeed, generatorIndex);
        }
    }
}

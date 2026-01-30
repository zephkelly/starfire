using Starfire.Core.V2.World.Chunk;

namespace Starfire.Core.V2.World.Generation
{
    /// <summary>
    /// Utilities for deterministic chunk-based seeding.
    /// Ensures consistent generation across sessions.
    /// </summary>
    public static class ChunkSeed
    {
        /// <summary>
        /// Calculate a unique seed for a chunk based on world seed and coordinates.
        /// Uses a hash combining method for good distribution.
        /// </summary>
        /// <param name="worldSeed">The master world seed</param>
        /// <param name="coord">The chunk coordinates</param>
        /// <returns>A deterministic seed value between 0 and 1</returns>
        public static float Calculate(float worldSeed, ChunkCoord coord)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + coord.X.GetHashCode();
                hash = hash * 31 + coord.Y.GetHashCode();
                hash = hash * 31 + worldSeed.GetHashCode();

                // Normalize to 0-1 range
                return (hash & 0x7FFFFFFF) / (float)int.MaxValue;
            }
        }

        /// <summary>
        /// Get a sub-seed for a specific generator within a chunk.
        /// Allows multiple generators to have independent but deterministic seeds.
        /// </summary>
        /// <param name="chunkSeed">The chunk's base seed</param>
        /// <param name="generatorIndex">Index of the generator (use consistent values)</param>
        /// <returns>A deterministic seed value between 0 and 1</returns>
        public static float GetGeneratorSeed(float chunkSeed, int generatorIndex)
        {
            unchecked
            {
                int hash = chunkSeed.GetHashCode();
                hash = hash * 31 + generatorIndex;
                return (hash & 0x7FFFFFFF) / (float)int.MaxValue;
            }
        }

        /// <summary>
        /// Get a random number generator seeded for a specific chunk.
        /// </summary>
        /// <param name="worldSeed">The master world seed</param>
        /// <param name="coord">The chunk coordinates</param>
        /// <returns>A seeded System.Random instance</returns>
        public static System.Random GetRandom(float worldSeed, ChunkCoord coord)
        {
            float seed = Calculate(worldSeed, coord);
            return new System.Random((int)(seed * int.MaxValue));
        }

        /// <summary>
        /// Get a random number generator for a specific generator within a chunk.
        /// </summary>
        /// <param name="chunkSeed">The chunk's base seed</param>
        /// <param name="generatorIndex">Index of the generator</param>
        /// <returns>A seeded System.Random instance</returns>
        public static System.Random GetGeneratorRandom(float chunkSeed, int generatorIndex)
        {
            float seed = GetGeneratorSeed(chunkSeed, generatorIndex);
            return new System.Random((int)(seed * int.MaxValue));
        }
    }
}

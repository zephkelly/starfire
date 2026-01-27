using System;
using System.Collections.Generic;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

namespace StarfireV2
{
    /// <summary>
    /// Context provided to world layers during generation.
    /// Provides access to earlier layer data for dependency resolution.
    /// </summary>
    public class WorldFabricContext
    {
        public float WorldSeed { get; }
        public Chunk Chunk { get; }
        public float ChunkSize { get; }
        public Vector2D OriginOffset { get; }
        public System.Random Random { get; }

        private readonly Dictionary<Type, WorldLayerData> _layerData = new();
        private readonly Dictionary<string, IWorldLayerQuery> _queries;

        public WorldFabricContext(
            float worldSeed,
            Chunk chunk,
            float chunkSize,
            Vector2D originOffset,
            Dictionary<string, IWorldLayerQuery> queries = null)
        {
            WorldSeed = worldSeed;
            Chunk = chunk;
            ChunkSize = chunkSize;
            OriginOffset = originOffset;
            _queries = queries ?? new Dictionary<string, IWorldLayerQuery>();

            // Create seeded random for this chunk
            int seed = (int)(chunk.GenerationSeed * int.MaxValue);
            Random = new System.Random(seed);
        }

        /// <summary>
        /// Get data from an earlier layer for this chunk.
        /// </summary>
        public T GetLayerData<T>() where T : WorldLayerData
        {
            if (_layerData.TryGetValue(typeof(T), out var data))
                return data as T;
            return null;
        }

        /// <summary>
        /// Add layer data (called by WorldFabricService after each layer generates).
        /// </summary>
        public void AddLayerData(WorldLayerData data)
        {
            if (data != null)
                _layerData[data.GetType()] = data;
        }

        /// <summary>
        /// Query a layer at any position (for cross-chunk lookups).
        /// </summary>
        public T QueryLayer<T>(string layerId, Vector2D position) where T : class
        {
            if (_queries.TryGetValue(layerId, out var query))
                return query.QueryAt(position, WorldSeed) as T;
            return null;
        }

        /// <summary>
        /// Get chunk center in absolute coordinates.
        /// </summary>
        public Vector2D GetChunkCenter()
        {
            return Chunk.Coord.ToAbsoluteCenter(ChunkSize);
        }

        /// <summary>
        /// Sample a noise field at a local position within the chunk.
        /// </summary>
        public float SampleNoise(INoiseField field, UnityEngine.Vector2 localPosition)
        {
            Vector2D absolute = GetChunkCenter() + Vector2D.FromVector2(localPosition);
            return field.Sample(absolute, WorldSeed);
        }
    }
}

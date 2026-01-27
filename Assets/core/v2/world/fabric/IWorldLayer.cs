using System;
using System.Collections.Generic;
using Starfire.Core.V2.World.Chunk;

namespace StarfireV2
{
    /// <summary>
    /// A layer in the world fabric that generates one type of world data.
    /// Layers are processed in priority order, allowing later layers to
    /// read data from earlier layers.
    /// </summary>
    public interface IWorldLayer
    {
        /// <summary>
        /// Unique identifier for this layer.
        /// </summary>
        string LayerId { get; }

        /// <summary>
        /// Processing priority. Lower = earlier.
        /// 0-9: Foundation (zones)
        /// 10-19: Political (factions)
        /// 20-29: Structural (POIs)
        /// 30-39: Environmental (hazards)
        /// 40+: Dynamic (spawners)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Whether this layer is currently enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Type of data this layer produces.
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// Types of layer data this layer depends on.
        /// WorldFabricService ensures these run first.
        /// </summary>
        IReadOnlyList<Type> Dependencies { get; }

        /// <summary>
        /// Generate layer data for a chunk.
        /// </summary>
        WorldLayerData Generate(Chunk chunk, WorldFabricContext context);

        /// <summary>
        /// Create a query instance for runtime position queries.
        /// </summary>
        IWorldLayerQuery CreateQuery();

        /// <summary>
        /// Called when a chunk is unloading.
        /// </summary>
        void OnChunkUnloading(Chunk chunk);
    }
}

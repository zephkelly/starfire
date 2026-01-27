using Starfire.Core.V2.World.Data;

namespace StarfireV2
{
    /// <summary>
    /// Base class for all world layer data stored in chunks.
    /// Extends ChunkData for compatibility with existing system.
    /// </summary>
    public abstract class WorldLayerData : ChunkData
    {
        /// <summary>
        /// The layer ID that generated this data.
        /// </summary>
        public abstract string LayerId { get; }
    }
}

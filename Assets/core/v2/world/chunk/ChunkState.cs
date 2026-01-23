namespace Starfire.Core.V2.World.Chunk
{
    /// <summary>
    /// Represents the lifecycle state of a chunk.
    /// </summary>
    public enum ChunkState
    {
        /// <summary>
        /// Chunk exists but has not been loaded.
        /// </summary>
        Unloaded,

        /// <summary>
        /// Chunk is currently being generated/loaded.
        /// </summary>
        Loading,

        /// <summary>
        /// Chunk is fully loaded and active.
        /// </summary>
        Loaded,

        /// <summary>
        /// Chunk is being unloaded.
        /// </summary>
        Unloading
    }
}

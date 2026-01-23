namespace Starfire.Core.V2.World.Data
{
    /// <summary>
    /// Base class for all chunk content data.
    /// Generators produce specific ChunkData subclasses,
    /// which are stored in chunks and consumed by data consumers.
    /// </summary>
    public abstract class ChunkData
    {
        /// <summary>
        /// Called when the chunk is being unloaded.
        /// Override to clean up any resources.
        /// </summary>
        public virtual void OnUnload() { }
    }
}

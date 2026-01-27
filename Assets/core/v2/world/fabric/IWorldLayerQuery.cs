using Starfire.Core.V2.World;

namespace StarfireV2
{
    /// <summary>
    /// Stateless query interface for runtime position queries.
    /// Allows querying layer information at any position without loaded chunks.
    /// </summary>
    public interface IWorldLayerQuery
    {
        /// <summary>
        /// Query layer data at a specific absolute position.
        /// Does not require the chunk to be loaded.
        /// </summary>
        object QueryAt(Vector2D absolutePosition, float worldSeed);
    }

    /// <summary>
    /// Typed query interface for specific layer data.
    /// </summary>
    public interface IWorldLayerQuery<T> : IWorldLayerQuery
    {
        new T QueryAt(Vector2D absolutePosition, float worldSeed);
    }
}

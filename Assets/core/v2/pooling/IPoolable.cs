namespace StarfireV2.Pooling
{
    /// <summary>
    /// Interface for objects that can be managed by an object pool.
    /// Implement this on components that need to reset their state when reused.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when the object is retrieved from the pool.
        /// Use this to prepare the object for use.
        /// </summary>
        /// <returns>True if the object is valid and ready to use, false if it should be discarded.</returns>
        bool OnPoolGet();

        /// <summary>
        /// Called when the object is returned to the pool.
        /// Reset all state here to prepare for reuse.
        /// </summary>
        void OnPoolReturn();

        /// <summary>
        /// Whether this object is currently active (not in the pool).
        /// </summary>
        bool IsActive { get; }
    }
}

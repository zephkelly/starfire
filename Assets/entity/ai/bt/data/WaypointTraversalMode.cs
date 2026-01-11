namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Determines how waypoints are traversed when reaching the end of the list.
    /// </summary>
    public enum WaypointTraversalMode
    {
        /// <summary>
        /// Loop back to start: 0 → 1 → 2 → 0 → 1 → 2 → ...
        /// </summary>
        Loop,

        /// <summary>
        /// Reverse direction at endpoints: 0 → 1 → 2 → 1 → 0 → 1 → 2 → ...
        /// </summary>
        PingPong
    }
}

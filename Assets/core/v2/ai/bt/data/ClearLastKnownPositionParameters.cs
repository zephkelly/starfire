using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for ClearLastKnownPositionAction.
    /// Clears the last known position after searching.
    /// </summary>
    [Serializable]
    public class ClearLastKnownPositionParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key to clear.
        /// </summary>
        public string lastPositionKey = "last_known_position";
    }
}

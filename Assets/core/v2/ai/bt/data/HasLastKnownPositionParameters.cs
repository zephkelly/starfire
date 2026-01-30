using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for HasLastKnownPositionCondition.
    /// Checks if a last known position is stored.
    /// </summary>
    [Serializable]
    public class HasLastKnownPositionParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key to check.
        /// </summary>
        public string lastPositionKey = "last_known_position";
    }
}

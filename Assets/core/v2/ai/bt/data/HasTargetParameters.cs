using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for HasTargetCondition.
    /// Checks if a target exists in the blackboard.
    /// </summary>
    [Serializable]
    public class HasTargetParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key to check for target presence.
        /// </summary>
        public string targetKey = "steering_target";
    }
}

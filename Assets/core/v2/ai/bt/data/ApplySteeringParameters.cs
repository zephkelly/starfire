using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for ApplySteeringAction.
    /// Reads calculated steering force from blackboard and applies it via the AIEntityControllerDriver.
    /// </summary>
    [Serializable]
    public class ApplySteeringParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for steering force to apply (Vector2).
        /// </summary>
        public string forceKey = "steering_force";
    }
}

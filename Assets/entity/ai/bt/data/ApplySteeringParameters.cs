using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for ApplySteeringAction.
    /// Reads steering data from blackboard (written by CalculateSteeringAction)
    /// and sets driver state for movement.
    /// </summary>
    [Serializable]
    public class ApplySteeringParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the steering direction (Vector2 normalized).
        /// </summary>
        public string directionKey = "steering_direction";

        /// <summary>
        /// Blackboard key for the throttle value (float 0-1).
        /// </summary>
        public string throttleKey = "steering_throttle";

        /// <summary>
        /// Blackboard key for the arrived flag (bool).
        /// </summary>
        public string arrivedKey = "steering_arrived";

        /// <summary>
        /// Blackboard key for the steering force vector (Vector2).
        /// </summary>
        public string steeringForceKey = "steering_force";

        /// <summary>
        /// Blackboard key for the target position, used for aim direction (Vector2).
        /// </summary>
        public string targetKey = "move_target";

        /// <summary>
        /// Whether to update the driver's AimPosition to face the target.
        /// </summary>
        public bool aimAtTarget = true;
    }
}

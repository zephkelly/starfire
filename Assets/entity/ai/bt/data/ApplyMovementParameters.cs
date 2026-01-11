using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for ApplyMovementAction.
    /// Reads steering data from blackboard and sets driver movement state.
    /// </summary>
    [Serializable]
    public class ApplyMovementParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the steering direction (Vector2 normalized).
        /// </summary>
        public string directionKey = "steering_direction";

        /// <summary>
        /// Blackboard key for the throttle value (float 0-1).
        /// </summary>
        public string throttleKey = "steering_throttle";
    }
}

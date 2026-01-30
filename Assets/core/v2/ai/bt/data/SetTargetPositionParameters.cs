using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for SetTargetPositionAction.
    /// Sets a static Vector2 position as the steering target.
    /// </summary>
    [Serializable]
    public class SetTargetPositionParameters : IBTNodeParameters
    {
        /// <summary>
        /// The static position to set as target.
        /// </summary>
        public Vector2 position = Vector2.zero;

        /// <summary>
        /// Blackboard key to write the target position.
        /// </summary>
        public string targetKey = "steering_target";
    }
}

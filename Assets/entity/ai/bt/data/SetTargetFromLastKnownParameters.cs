using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for SetTargetFromLastKnownAction.
    /// Sets steering target to last known position of a lost contact.
    /// </summary>
    [Serializable]
    public class SetTargetFromLastKnownParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the last known position.
        /// </summary>
        public string lastPositionKey = "last_known_position";

        /// <summary>
        /// Blackboard key to write the steering target.
        /// </summary>
        public string steeringTargetKey = "steering_target";
    }
}

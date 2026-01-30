using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for UpdateTargetDetectionAction.
    /// Refreshes detection level of investigation target and stores last known position.
    /// </summary>
    [Serializable]
    public class UpdateTargetDetectionParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the investigation target.
        /// </summary>
        public string targetKey = "investigation_target";

        /// <summary>
        /// Blackboard key to write the current detection level.
        /// </summary>
        public string levelKey = "target_detection_level";

        /// <summary>
        /// Blackboard key to write the last known position.
        /// </summary>
        public string lastPositionKey = "last_known_position";
    }
}

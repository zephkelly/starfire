using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for SetTargetFromInvestigationAction.
    /// Copies position from investigation target to steering target.
    /// </summary>
    [Serializable]
    public class SetTargetFromInvestigationParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the investigation target.
        /// </summary>
        public string targetKey = "investigation_target";

        /// <summary>
        /// Blackboard key to write the steering target position.
        /// </summary>
        public string steeringTargetKey = "steering_target";
    }
}

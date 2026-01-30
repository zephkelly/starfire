using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for StoreMonitoredTargetAction.
    /// Copies a detected entity from one blackboard key to another for monitoring.
    /// </summary>
    [Serializable]
    public class StoreMonitoredTargetParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key to read the source V2DetectedEntity from.
        /// </summary>
        public string sourceKey = "investigation_target";

        /// <summary>
        /// Blackboard key to store the target for monitoring.
        /// </summary>
        public string targetKey = "monitored_target";
    }
}

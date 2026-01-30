using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for IsTargetDetectionLevelCondition.
    /// Checks if target is at or above a specific detection level.
    /// </summary>
    [Serializable]
    public class IsTargetDetectionLevelParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the current detection level.
        /// </summary>
        public string levelKey = "target_detection_level";

        /// <summary>
        /// Minimum required detection level.
        /// Can be a fixed value or read from the blackboard at runtime.
        /// </summary>
        public BlackboardKeyOr<V2DetectionLevel> minLevel = new()
        {
            useBlackboardKey = false,
            fixedValue = V2DetectionLevel.Silhouette
        };
    }
}

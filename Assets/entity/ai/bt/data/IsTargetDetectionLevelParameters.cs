using System;
using Starfire.Entity.Modules.Sensor;

namespace Starfire.Entity.AI.BT
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
        public BlackboardKeyOr<DetectionLevel> minLevel = new()
        {
            useBlackboardKey = false,
            fixedValue = DetectionLevel.Silhouette
        };
    }
}

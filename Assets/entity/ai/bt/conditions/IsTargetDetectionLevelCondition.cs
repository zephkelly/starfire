using Starfire.Entity.Modules.Sensor;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when target is at or above a specific detection level.
    /// The required level can be a fixed value or read from the blackboard at runtime.
    /// </summary>
    public class IsTargetDetectionLevelCondition : BTLeafCondition
    {
        private readonly string _levelKey;
        private readonly BlackboardKeyOr<DetectionLevel> _minLevel;

        public IsTargetDetectionLevelCondition(
            string levelKey = "target_detection_level",
            BlackboardKeyOr<DetectionLevel> minLevel = null)
        {
            _levelKey = levelKey;
            _minLevel = minLevel ?? new BlackboardKeyOr<DetectionLevel>(DetectionLevel.Silhouette);
        }

        protected override bool CheckCondition()
        {
            if (!Context.TryGet<DetectionLevel>(_levelKey, out var currentLevel))
            {
                return false;
            }

            var requiredLevel = _minLevel.GetValue(Context);
            return currentLevel >= requiredLevel;
        }
    }
}

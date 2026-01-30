
namespace StarfireV2
{
    /// <summary>
    /// Condition that succeeds when target is at or above a specific detection level.
    /// The required level can be a fixed value or read from the blackboard at runtime.
    /// </summary>
    public class IsTargetDetectionLevelCondition : BTLeafCondition
    {
        private readonly string _levelKey;
        private readonly BlackboardKeyOr<V2DetectionLevel> _minLevel;

        public IsTargetDetectionLevelCondition(
            string levelKey = "target_detection_level",
            BlackboardKeyOr<V2DetectionLevel> minLevel = null)
        {
            _levelKey = levelKey;
            _minLevel = minLevel ?? new BlackboardKeyOr<V2DetectionLevel>(V2DetectionLevel.Silhouette);
        }

        protected override bool CheckCondition()
        {
            if (!Context.TryGet<V2DetectionLevel>(_levelKey, out var currentLevel))
            {
                return false;
            }

            var requiredLevel = _minLevel.GetValue(Context);
            return currentLevel >= requiredLevel;
        }
    }
}

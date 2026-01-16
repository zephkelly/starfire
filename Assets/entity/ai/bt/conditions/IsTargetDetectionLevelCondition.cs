using Starfire.Entity.Modules.Sensor;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when target is at or above a specific detection level.
    /// </summary>
    public class IsTargetDetectionLevelCondition : BTLeafCondition
    {
        private readonly string _levelKey;
        private readonly DetectionLevel _minLevel;

        public IsTargetDetectionLevelCondition(
            string levelKey = "target_detection_level",
            DetectionLevel minLevel = DetectionLevel.Silhouette)
        {
            _levelKey = levelKey;
            _minLevel = minLevel;
        }

        protected override bool CheckCondition()
        {
            if (!Context.TryGet<DetectionLevel>(_levelKey, out var level))
            {
                return false;
            }

            return level >= _minLevel;
        }
    }
}

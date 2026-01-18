using System.Linq;
using UnityEngine;
using Starfire.Entity.Modules.Sensor;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Refreshes detection level of investigation target and stores last known position.
    /// Returns Success if target still valid, Failure if lost.
    /// </summary>
    public class UpdateTargetDetectionAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _levelKey;
        private readonly string _lastPositionKey;

        public UpdateTargetDetectionAction(
            string targetKey = "investigation_target",
            string levelKey = "target_detection_level",
            string lastPositionKey = "last_known_position")
        {
            _targetKey = targetKey;
            _levelKey = levelKey;
            _lastPositionKey = lastPositionKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            if (!Context.TryGet<DetectedEntity>(_targetKey, out var target))
            {
                return BTNodeStatus.Failure;
            }

            // Check if target controller is still valid (destroyed check)
            if (!target.IsValid)
            {
                return BTNodeStatus.Failure;
            }

            // Store last known position
            Vector2 lastPosition = target.Position;
            Context.Set(_lastPositionKey, lastPosition);

            // Get fresh detection data from sensor
            var sensor = Context.Systems?.GetAllModulesOfType<ISensorShipModule>().FirstOrDefault();
            if (sensor == null)
            {
                return BTNodeStatus.Failure;
            }

            // Find updated detection info for this target
            var updatedTarget = sensor.DetectedEntities
                .FirstOrDefault(e => e.Controller == target.Controller);

            if (updatedTarget.IsValid)
            {
                // Update target with fresh detection data
                Context.Set(_targetKey, updatedTarget);
                Context.Set(_levelKey, updatedTarget.Level);
            }
            else
            {
                // Target not in current sensor sweep, but controller still exists
                // Keep using stored data - don't fail
                Context.Set(_levelKey, target.Level);
            }

            return BTNodeStatus.Success;
        }
    }
}

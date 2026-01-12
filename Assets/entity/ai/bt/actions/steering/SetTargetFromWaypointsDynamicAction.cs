using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Copies the current waypoint position to the target key using live Transform references.
    /// Unlike SetTargetFromWaypointsAction which uses static Vector2 positions,
    /// this action reads the transform's current position each tick for dynamic tracking.
    /// </summary>
    public class SetTargetFromWaypointsDynamicAction : BTAction
    {
        private readonly string _waypointsKey;
        private readonly string _indexKey;
        private readonly string _targetKey;

        public SetTargetFromWaypointsDynamicAction(
            string waypointsKey = "waypoint_transforms",
            string indexKey = "waypoint_index",
            string targetKey = "steering_target")
        {
            _waypointsKey = waypointsKey;
            _indexKey = indexKey;
            _targetKey = targetKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get waypoint transforms
            if (!Context.TryGet<List<Transform>>(_waypointsKey, out var waypoints) || waypoints == null || waypoints.Count == 0)
            {
                Debug.LogWarning($"[SetTargetFromWaypointsDynamic] FAILURE: No waypoint transforms found in blackboard key '{_waypointsKey}'. Make sure waypoints are initialized.");
                return BTNodeStatus.Failure;
            }

            // Get current index (default to 0 if not set)
            if (!Context.TryGet<int>(_indexKey, out var index))
            {
                index = 0;
                Context.Set(_indexKey, index);
                Debug.Log($"[SetTargetFromWaypointsDynamic] Initialized waypoint index to 0");
            }

            // Clamp index to valid range
            index = Mathf.Clamp(index, 0, waypoints.Count - 1);

            // Get transform at current index
            var waypointTransform = waypoints[index];
            if (waypointTransform == null)
            {
                Debug.LogWarning($"[SetTargetFromWaypointsDynamic] FAILURE: Waypoint transform at index {index} is null (destroyed?).");
                return BTNodeStatus.Failure;
            }

            // Set target to current waypoint position (read live from transform)
            Vector2 target = waypointTransform.position;
            Context.Set(_targetKey, target);

            Debug.Log($"[SetTargetFromWaypointsDynamic] Set target to waypoint[{index}] = {target}");

            return BTNodeStatus.Success;
        }
    }
}

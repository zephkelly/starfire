using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Copies the current waypoint position to the target key.
    /// Supports both stack-based waypoints (WaypointStackState) and legacy List waypoints.
    /// Checks for stack first, falls back to legacy for backward compatibility.
    /// </summary>
    public class SetTargetFromWaypointsAction : BTAction
    {
        private readonly string _waypointsKey;
        private readonly string _indexKey;
        private readonly string _targetKey;
        private readonly string _stackKey;

        public SetTargetFromWaypointsAction(
            string waypointsKey = "waypoint_list",
            string indexKey = "waypoint_index",
            string targetKey = "steering_target",
            string stackKey = "waypoint_stack")
        {
            _waypointsKey = waypointsKey;
            _indexKey = indexKey;
            _targetKey = targetKey;
            _stackKey = stackKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Try stack-based waypoints first
            if (Context.TryGet<WaypointStackState>(_stackKey, out var stack) && !stack.IsEmpty)
            {
                return SetTargetFromStack(stack);
            }

            // Fall back to legacy waypoint list
            return SetTargetFromLegacyList();
        }

        private BTNodeStatus SetTargetFromStack(WaypointStackState stack)
        {
            var target = stack.GetCurrentTarget();
            if (!target.HasValue)
            {
                Debug.LogWarning($"[SetTargetFromWaypoints] FAILURE: Stack has no current target");
                return BTNodeStatus.Failure;
            }

            Context.Set(_targetKey, target.Value);
            return BTNodeStatus.Success;
        }

        private BTNodeStatus SetTargetFromLegacyList()
        {
            // Get waypoint list
            if (!Context.TryGet<List<Vector2>>(_waypointsKey, out var waypoints) || waypoints == null || waypoints.Count == 0)
            {
                Debug.LogWarning($"[SetTargetFromWaypoints] FAILURE: No waypoints found in blackboard key '{_waypointsKey}'. Make sure waypoints are initialized.");
                return BTNodeStatus.Failure;
            }

            // Get current index (default to 0 if not set)
            if (!Context.TryGet<int>(_indexKey, out var index))
            {
                index = 0;
                Context.Set(_indexKey, index);
            }

            // Clamp index to valid range
            index = Mathf.Clamp(index, 0, waypoints.Count - 1);

            // Set target to current waypoint
            var target = waypoints[index];
            Context.Set(_targetKey, target);

            return BTNodeStatus.Success;
        }
    }
}

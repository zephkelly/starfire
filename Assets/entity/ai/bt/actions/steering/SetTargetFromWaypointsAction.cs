using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Copies the current waypoint position to the target key.
    /// Reads the waypoint list and current index from the blackboard.
    /// </summary>
    public class SetTargetFromWaypointsAction : BTAction
    {
        private readonly string _waypointsKey;
        private readonly string _indexKey;
        private readonly string _targetKey;

        public SetTargetFromWaypointsAction(
            string waypointsKey = "waypoint_list",
            string indexKey = "waypoint_index",
            string targetKey = "steering_target")
        {
            _waypointsKey = waypointsKey;
            _indexKey = indexKey;
            _targetKey = targetKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
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
                Debug.Log($"[SetTargetFromWaypoints] Initialized waypoint index to 0");
            }

            // Clamp index to valid range
            index = Mathf.Clamp(index, 0, waypoints.Count - 1);

            // Set target to current waypoint
            var target = waypoints[index];
            Context.Set(_targetKey, target);

            Debug.Log($"[SetTargetFromWaypoints] Set target to waypoint[{index}] = {target}");

            return BTNodeStatus.Success;
        }
    }
}

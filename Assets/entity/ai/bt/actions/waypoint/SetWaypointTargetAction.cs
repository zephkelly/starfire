using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Sets the current waypoint as the move target. Does NOT advance the index.
    /// This action is idempotent - calling it multiple times has the same effect.
    ///
    /// Use with AdvanceWaypointAction after arriving to create patrol behavior:
    ///   Sequence:
    ///     - SetWaypointTarget (sets current waypoint as move_target)
    ///     - MoveTo / ApplySteering (move to target)
    ///     - AdvanceWaypoint (increment index for next iteration)
    /// </summary>
    public class SetWaypointTargetAction : BTAction
    {
        private readonly string _waypointsKey;
        private readonly string _targetKey;
        private readonly string _indexKey;

        public SetWaypointTargetAction(
            string waypointsKey = "patrol_waypoints",
            string targetKey = "move_target",
            string indexKey = "waypoint_index")
        {
            _waypointsKey = waypointsKey;
            _targetKey = targetKey;
            _indexKey = indexKey;
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            // Get waypoints from blackboard
            if (!Context.TryGet<List<Vector2>>(_waypointsKey, out var waypoints) || waypoints.Count == 0)
            {
                Debug.LogWarning($"[SetWaypointTarget] No waypoints found at key '{_waypointsKey}'");
                return BTNodeStatus.Failure;
            }

            // Get current index (default to 0)
            if (!Context.TryGet<int>(_indexKey, out var currentIndex))
            {
                currentIndex = 0;
                Context.Set(_indexKey, currentIndex);
            }

            // Ensure index is valid (wrap if needed)
            currentIndex = currentIndex % waypoints.Count;

            // Set current waypoint as target (idempotent operation)
            var target = waypoints[currentIndex];
            Context.Set(_targetKey, target);

            return BTNodeStatus.Success;
        }
    }
}

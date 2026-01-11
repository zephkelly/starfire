using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Cycles through a list of waypoints and sets the current one as the move target.
    /// Use with MoveTo action to create patrol behavior.
    /// </summary>
    public class SetNextWaypointAction : BTAction
    {
        private readonly string _waypointsKey;
        private readonly string _targetKey;
        private readonly string _indexKey;

        public SetNextWaypointAction(
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
                Debug.LogWarning($"SetNextWaypointAction: No waypoints found at key '{_waypointsKey}'");
                return BTNodeStatus.Failure;
            }

            // Get current index (default to 0)
            if (!Context.TryGet<int>(_indexKey, out var currentIndex))
            {
                currentIndex = 0;
            }

            // Ensure index is valid
            currentIndex = currentIndex % waypoints.Count;

            // Set current waypoint as target
            Context.Set(_targetKey, waypoints[currentIndex]);

            // Advance to next waypoint (wrap around)
            int nextIndex = (currentIndex + 1) % waypoints.Count;
            Context.Set(_indexKey, nextIndex);

            return BTNodeStatus.Success;
        }
    }
}

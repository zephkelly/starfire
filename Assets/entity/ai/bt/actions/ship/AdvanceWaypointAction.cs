using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Advances the waypoint index to the next waypoint in the list.
    /// Should be called AFTER the ship has arrived at the current waypoint.
    ///
    /// Supports two traversal modes:
    /// - Loop: wraps from end back to start (0 → 1 → 2 → 0 → 1 → 2)
    /// - PingPong: reverses direction at endpoints (0 → 1 → 2 → 1 → 0 → 1 → 2)
    ///
    /// Typical usage in a patrol sequence:
    ///   Sequence:
    ///     - SetWaypointTarget
    ///     - MoveTo (returns Success when arrived)
    ///     - AdvanceWaypoint (only runs after MoveTo succeeds)
    /// </summary>
    public class AdvanceWaypointAction : BTAction
    {
        private readonly string _waypointsKey;
        private readonly string _indexKey;
        private readonly WaypointTraversalMode _traversalMode;
        private readonly string _directionKey;

        public AdvanceWaypointAction(
            string waypointsKey = "patrol_waypoints",
            string indexKey = "waypoint_index",
            WaypointTraversalMode traversalMode = WaypointTraversalMode.Loop,
            string directionKey = "waypoint_direction")
        {
            _waypointsKey = waypointsKey;
            _indexKey = indexKey;
            _traversalMode = traversalMode;
            _directionKey = directionKey;
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            // Get waypoints to determine count for wrap-around
            if (!Context.TryGet<List<Vector2>>(_waypointsKey, out var waypoints) || waypoints.Count == 0)
            {
                Debug.LogWarning($"[AdvanceWaypoint] No waypoints found at key '{_waypointsKey}'");
                return BTNodeStatus.Failure;
            }

            // Get current index (default to 0)
            if (!Context.TryGet<int>(_indexKey, out var currentIndex))
            {
                currentIndex = 0;
            }

            int nextIndex;

            if (_traversalMode == WaypointTraversalMode.PingPong)
            {
                nextIndex = AdvancePingPong(waypoints.Count, currentIndex);
            }
            else
            {
                // Loop mode: wrap around
                nextIndex = (currentIndex + 1) % waypoints.Count;
            }

            Context.Set(_indexKey, nextIndex);

            return BTNodeStatus.Success;
        }

        private int AdvancePingPong(int waypointCount, int currentIndex)
        {
            // Handle edge case of 1 or 2 waypoints
            if (waypointCount <= 1)
                return 0;

            if (waypointCount == 2)
                return currentIndex == 0 ? 1 : 0;

            // Get or initialize direction (1 = forward, -1 = backward)
            if (!Context.TryGet<int>(_directionKey, out var direction))
            {
                direction = 1;
            }

            int nextIndex = currentIndex + direction;

            // Reverse at endpoints
            if (nextIndex >= waypointCount)
            {
                nextIndex = waypointCount - 2;
                direction = -1;
            }
            else if (nextIndex < 0)
            {
                nextIndex = 1;
                direction = 1;
            }

            Context.Set(_directionKey, direction);
            return nextIndex;
        }
    }
}

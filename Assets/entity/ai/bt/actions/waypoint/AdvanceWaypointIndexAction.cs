using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Advances the waypoint index to the next waypoint.
    /// Supports Loop, PingPong, and Once traversal modes.
    /// </summary>
    public class AdvanceWaypointIndexAction : BTAction
    {
        private readonly string _waypointsKey;
        private readonly string _indexKey;
        private readonly string _directionKey;
        private readonly WaypointTraversalMode _mode;

        public AdvanceWaypointIndexAction(
            string waypointsKey = "waypoint_list",
            string indexKey = "waypoint_index",
            WaypointTraversalMode mode = WaypointTraversalMode.Loop,
            string directionKey = "waypoint_direction")
        {
            _waypointsKey = waypointsKey;
            _indexKey = indexKey;
            _mode = mode;
            _directionKey = directionKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get waypoint list
            if (!Context.TryGet<List<Vector2>>(_waypointsKey, out var waypoints) || waypoints == null || waypoints.Count == 0)
            {
                return BTNodeStatus.Failure;
            }

            // Get current index
            if (!Context.TryGet<int>(_indexKey, out var index))
            {
                index = 0;
            }

            int count = waypoints.Count;

            switch (_mode)
            {
                case WaypointTraversalMode.Loop:
                    index = (index + 1) % count;
                    break;

                case WaypointTraversalMode.PingPong:
                    // Get or initialize direction
                    if (!Context.TryGet<int>(_directionKey, out var direction))
                    {
                        direction = 1;
                    }

                    index += direction;

                    // Check bounds and reverse direction if needed
                    if (index >= count)
                    {
                        index = count - 2;
                        direction = -1;
                        if (index < 0) index = 0;
                    }
                    else if (index < 0)
                    {
                        index = 1;
                        direction = 1;
                        if (index >= count) index = count - 1;
                    }

                    Context.Set(_directionKey, direction);
                    break;

                case WaypointTraversalMode.Once:
                    if (index < count - 1)
                    {
                        index++;
                    }
                    else
                    {
                        // Already at last waypoint, return failure to signal completion
                        return BTNodeStatus.Failure;
                    }
                    break;
            }

            Context.Set(_indexKey, index);
            return BTNodeStatus.Success;
        }
    }
}

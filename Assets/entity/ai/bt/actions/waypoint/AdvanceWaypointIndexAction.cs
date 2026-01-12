using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Advances the waypoint index to the next waypoint.
    /// Supports both stack-based waypoints (WaypointStackState) and legacy List waypoints.
    /// For stack: advances within the current sequence using its traversal mode.
    /// For legacy: uses the mode specified in parameters.
    /// </summary>
    public class AdvanceWaypointIndexAction : BTAction
    {
        private readonly string _waypointsKey;
        private readonly string _transformsKey;
        private readonly string _indexKey;
        private readonly string _directionKey;
        private readonly string _stackKey;
        private readonly WaypointTraversalMode _mode;

        public AdvanceWaypointIndexAction(
            string waypointsKey = "waypoint_list",
            string indexKey = "waypoint_index",
            WaypointTraversalMode mode = WaypointTraversalMode.Loop,
            string directionKey = "waypoint_direction",
            string stackKey = "waypoint_stack",
            string transformsKey = "waypoint_transforms")
        {
            _waypointsKey = waypointsKey;
            _transformsKey = transformsKey;
            _indexKey = indexKey;
            _mode = mode;
            _directionKey = directionKey;
            _stackKey = stackKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Try stack-based waypoints first
            if (Context.TryGet<WaypointStackState>(_stackKey, out var stack) && !stack.IsEmpty)
            {
                return AdvanceInStack(stack);
            }

            // Fall back to legacy waypoint list
            return AdvanceInLegacyList();
        }

        private BTNodeStatus AdvanceInStack(WaypointStackState stack)
        {
            // Advance within the current sequence (uses sequence's own mode)
            if (stack.AdvanceIndex())
            {
                // Update stack on blackboard (in case internal state changed)
                Context.Set(_stackKey, stack);
                return BTNodeStatus.Success;
            }

            // Sequence complete (Once mode at end)
            // Note: Popping is handled by a separate PopWaypointsAction in the tree
            return BTNodeStatus.Failure;
        }

        private BTNodeStatus AdvanceInLegacyList()
        {
            // Get waypoint count - try static Vector2 list first, then transforms
            int count = 0;

            if (Context.TryGet<List<Vector2>>(_waypointsKey, out var waypoints) && waypoints != null && waypoints.Count > 0)
            {
                count = waypoints.Count;
            }
            else if (Context.TryGet<List<Transform>>(_transformsKey, out var transforms) && transforms != null && transforms.Count > 0)
            {
                count = transforms.Count;
            }
            else
            {
                return BTNodeStatus.Failure;
            }

            // Get current index
            if (!Context.TryGet<int>(_indexKey, out var index))
            {
                index = 0;
            }

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

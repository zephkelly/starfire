using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Pushes a new waypoint sequence onto the stack.
    /// Returns Failure if at max depth or no waypoints provided.
    /// </summary>
    public class PushWaypointsAction : BTAction
    {
        private readonly string _stackKey;
        private readonly string _waypointsKey;
        private readonly WaypointTraversalMode _mode;
        private readonly string _label;

        public PushWaypointsAction(
            string stackKey = "waypoint_stack",
            string waypointsKey = "generated_subwaypoints",
            WaypointTraversalMode mode = WaypointTraversalMode.Once,
            string label = "exploration")
        {
            _stackKey = stackKey;
            _waypointsKey = waypointsKey;
            _mode = mode;
            _label = label;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get stack
            if (!Context.TryGet<WaypointStackState>(_stackKey, out var stack))
            {
                Debug.LogWarning($"PushWaypointsAction: No waypoint stack found at key '{_stackKey}'");
                return BTNodeStatus.Failure;
            }

            // Check max depth
            if (stack.IsAtMaxDepth)
            {
                Debug.LogWarning($"PushWaypointsAction: Stack at max depth ({stack.MaxDepth}), cannot push");
                return BTNodeStatus.Failure;
            }

            // Get waypoints to push
            if (!Context.TryGet<List<Vector2>>(_waypointsKey, out var waypoints) || waypoints.Count == 0)
            {
                Debug.LogWarning($"PushWaypointsAction: No waypoints found at key '{_waypointsKey}'");
                return BTNodeStatus.Failure;
            }

            // Push new sequence
            if (!stack.Push(waypoints, _mode, _label))
            {
                return BTNodeStatus.Failure;
            }

            // Stack was modified, update on blackboard
            Context.Set(_stackKey, stack);

            return BTNodeStatus.Success;
        }
    }
}

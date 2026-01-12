using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Initializes the waypoint stack from waypoints.
    /// Supports both dynamic (Transform) and static (Vector2) waypoints.
    /// Prefers transforms for live tracking when available.
    /// Should be called once at the start of patrol behavior.
    /// If stack already exists and has entries, does nothing (returns Success).
    /// </summary>
    public class InitWaypointStackAction : BTAction
    {
        private readonly string _waypointsKey;
        private readonly string _transformsKey;
        private readonly string _stackKey;
        private readonly WaypointTraversalMode _mode;
        private readonly int _maxDepth;

        public InitWaypointStackAction(
            string waypointsKey = "waypoint_list",
            string stackKey = "waypoint_stack",
            WaypointTraversalMode mode = WaypointTraversalMode.Loop,
            int maxDepth = 5,
            string transformsKey = "waypoint_transforms")
        {
            _waypointsKey = waypointsKey;
            _transformsKey = transformsKey;
            _stackKey = stackKey;
            _mode = mode;
            _maxDepth = maxDepth;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Check if stack already exists and is initialized
            if (Context.TryGet<WaypointStackState>(_stackKey, out var existingStack) && !existingStack.IsEmpty)
            {
                // Already initialized, nothing to do
                return BTNodeStatus.Success;
            }

            // Create new stack state
            var stack = new WaypointStackState
            {
                MaxDepth = _maxDepth
            };

            // Try dynamic transforms first (preferred for live tracking)
            if (Context.TryGet<List<Transform>>(_transformsKey, out var transforms) && transforms != null && transforms.Count > 0)
            {
                stack.Initialize(transforms, _mode, "patrol");
                Context.Set(_stackKey, stack);
                return BTNodeStatus.Success;
            }

            // Fall back to static waypoints
            if (Context.TryGet<List<Vector2>>(_waypointsKey, out var waypoints) && waypoints != null && waypoints.Count > 0)
            {
                stack.Initialize(waypoints, _mode, "patrol");
                Context.Set(_stackKey, stack);
                return BTNodeStatus.Success;
            }

            // No waypoints to initialize from
            return BTNodeStatus.Failure;
        }
    }
}

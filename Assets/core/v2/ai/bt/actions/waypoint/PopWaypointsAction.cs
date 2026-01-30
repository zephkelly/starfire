using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Pops the current waypoint sequence and returns to the previous one.
    /// Returns Failure if at base level (can't pop the base patrol sequence).
    /// </summary>
    public class PopWaypointsAction : BTAction
    {
        private readonly string _stackKey;

        public PopWaypointsAction(string stackKey = "waypoint_stack")
        {
            _stackKey = stackKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get stack
            if (!Context.TryGet<WaypointStackState>(_stackKey, out var stack))
            {
                Debug.LogWarning($"PopWaypointsAction: No waypoint stack found at key '{_stackKey}'");
                return BTNodeStatus.Failure;
            }

            // Check if we can pop
            if (stack.Depth <= 1)
            {
                // At base level, can't pop
                return BTNodeStatus.Failure;
            }

            // Pop the current sequence
            var popped = stack.Pop();
            if (popped == null)
            {
                return BTNodeStatus.Failure;
            }

            // Stack was modified, update on blackboard
            Context.Set(_stackKey, stack);

            return BTNodeStatus.Success;
        }
    }
}

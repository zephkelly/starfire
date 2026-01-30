namespace StarfireV2
{
    /// <summary>
    /// Condition that checks if the current waypoint sequence is complete.
    /// Returns Success if the current sequence is at its last waypoint (for Once mode)
    /// or if the stack is empty.
    /// </summary>
    public class IsSequenceCompleteCondition : BTAction
    {
        private readonly string _stackKey;

        public IsSequenceCompleteCondition(string stackKey = "waypoint_stack")
        {
            _stackKey = stackKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get stack
            if (!Context.TryGet<WaypointStackState>(_stackKey, out var stack))
            {
                // No stack = complete
                return BTNodeStatus.Success;
            }

            // Check if current sequence is complete
            if (stack.IsCurrentSequenceComplete())
            {
                return BTNodeStatus.Success;
            }

            return BTNodeStatus.Failure;
        }
    }
}

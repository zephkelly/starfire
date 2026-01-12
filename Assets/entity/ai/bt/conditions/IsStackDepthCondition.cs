namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that checks the waypoint stack depth.
    /// Use to determine if we're in a sub-sequence or at base patrol level.
    /// </summary>
    public class IsStackDepthCondition : BTAction
    {
        private readonly string _stackKey;
        private readonly int _depth;
        private readonly ComparisonType _comparison;

        public IsStackDepthCondition(
            string stackKey = "waypoint_stack",
            int depth = 1,
            ComparisonType comparison = ComparisonType.GreaterThan)
        {
            _stackKey = stackKey;
            _depth = depth;
            _comparison = comparison;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get stack
            if (!Context.TryGet<WaypointStackState>(_stackKey, out var stack))
            {
                // No stack = depth 0
                return EvaluateComparison(0) ? BTNodeStatus.Success : BTNodeStatus.Failure;
            }

            int currentDepth = stack.Depth;
            return EvaluateComparison(currentDepth) ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }

        private bool EvaluateComparison(int currentDepth)
        {
            return _comparison switch
            {
                ComparisonType.Equal => currentDepth == _depth,
                ComparisonType.GreaterThan => currentDepth > _depth,
                ComparisonType.LessThan => currentDepth < _depth,
                ComparisonType.GreaterThanOrEqual => currentDepth >= _depth,
                ComparisonType.LessThanOrEqual => currentDepth <= _depth,
                _ => false
            };
        }
    }
}

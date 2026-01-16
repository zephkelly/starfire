namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Checks if a heuristic value meets a threshold.
    /// Used for reactive behavior based on ship state.
    /// </summary>
    public class HeuristicThresholdCondition : BTLeafCondition
    {
        private readonly string _heuristicKey;
        private readonly float _threshold;
        private readonly ComparisonOperator _comparison;

        public HeuristicThresholdCondition(
            string heuristicKey,
            float threshold,
            ComparisonOperator comparison = ComparisonOperator.GreaterThan)
        {
            _heuristicKey = heuristicKey;
            _threshold = threshold;
            _comparison = comparison;
        }

        protected override bool CheckCondition()
        {
            if (!Context.TryGet<float>(_heuristicKey, out var value))
            {
                return false;
            }

            return _comparison switch
            {
                ComparisonOperator.GreaterThan => value > _threshold,
                ComparisonOperator.GreaterThanOrEqual => value >= _threshold,
                ComparisonOperator.LessThan => value < _threshold,
                ComparisonOperator.LessThanOrEqual => value <= _threshold,
                ComparisonOperator.Equal => System.Math.Abs(value - _threshold) < 0.001f,
                _ => false
            };
        }
    }
}

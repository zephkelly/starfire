using System;

namespace StarfireV2
{
    /// <summary>
    /// Comparison operators for threshold conditions.
    /// </summary>
    public enum ComparisonOperator
    {
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Equal
    }

    /// <summary>
    /// Parameters for HeuristicThresholdCondition.
    /// Checks if a heuristic value meets a threshold.
    /// </summary>
    [Serializable]
    public class HeuristicThresholdParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the heuristic value to check.
        /// </summary>
        public string heuristicKey = "heuristic_confidence";

        /// <summary>
        /// Threshold value to compare against.
        /// </summary>
        public float threshold = 0.5f;

        /// <summary>
        /// Comparison operator to use.
        /// </summary>
        public ComparisonOperator comparison = ComparisonOperator.GreaterThan;

        /// <summary>
        /// If true, inverts the condition result.
        /// </summary>
        public bool invert = false;
    }
}

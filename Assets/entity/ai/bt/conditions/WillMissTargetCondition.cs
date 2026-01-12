namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that checks if the current trajectory will miss the target.
    /// Returns Success if ship will miss, Failure otherwise.
    /// Requires CalculateTrajectoryPredictionAction to run first.
    /// </summary>
    public class WillMissTargetCondition : BTLeafCondition
    {
        private readonly string _predictionKey;
        private readonly float _missThreshold;

        public WillMissTargetCondition(
            string predictionKey = "trajectory_prediction",
            float missThreshold = 2.0f)
        {
            _predictionKey = predictionKey;
            _missThreshold = missThreshold;
        }

        protected override bool CheckCondition()
        {
            // Get prediction from blackboard
            if (!Context.TryGet<TrajectoryPrediction>(_predictionKey, out var prediction))
            {
                return false;
            }

            // Check if trajectory will miss target
            return prediction.WillMiss && prediction.MissDistance > _missThreshold;
        }
    }
}

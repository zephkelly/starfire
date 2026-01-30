namespace StarfireV2
{
    /// <summary>
    /// Condition that checks if the ship has passed the target and is moving away.
    /// Returns Success if overshot, Failure otherwise.
    /// Requires CalculateTrajectoryPredictionAction to run first.
    /// </summary>
    public class HasOvershotCondition : BTLeafCondition
    {
        private readonly string _predictionKey;
        private readonly float _minDistance;

        public HasOvershotCondition(
            string predictionKey = "trajectory_prediction",
            float minDistance = 0.5f)
        {
            _predictionKey = predictionKey;
            _minDistance = minDistance;
        }

        protected override bool CheckCondition()
        {
            // Get prediction from blackboard
            if (!Context.TryGet<TrajectoryPrediction>(_predictionKey, out var prediction))
            {
                return false;
            }

            // Check if overshot: moving away from target and not too close
            return prediction.HasOvershot && prediction.DistanceToTarget > _minDistance;
        }
    }
}

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that checks if the approach angle to target exceeds a threshold.
    /// Used to detect when ship needs to brake and turn rather than arc toward target.
    /// Returns Success when approach angle > threshold, Failure otherwise.
    /// </summary>
    public class IsHighApproachAngleCondition : BTLeafCondition
    {
        private readonly string _predictionKey;
        private readonly float _angleThreshold;

        public IsHighApproachAngleCondition(
            string predictionKey = "trajectory_prediction",
            float angleThreshold = 90f)
        {
            _predictionKey = predictionKey;
            _angleThreshold = angleThreshold;
        }

        protected override bool CheckCondition()
        {
            // Get trajectory prediction from blackboard
            if (!Context.TryGet<TrajectoryPrediction>(_predictionKey, out var prediction))
            {
                return false;
            }

            // Check if approach angle exceeds threshold
            return prediction.ApproachAngle > _angleThreshold;
        }
    }
}

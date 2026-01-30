using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Calculates the holding position based on sensor's Silhouette range and sets it as steering target.
    /// The holding position is on the line between ship and target, at (SilhouetteRange - buffer) from target.
    /// This allows CalculateSmartArrive to naturally handle approach and stopping.
    /// Reads silhouetteRange from blackboard heuristics.
    /// </summary>
    public class SetHoldingPositionAction : BTAction
    {
        private readonly string _targetEntityKey;
        private readonly string _outputKey;
        private readonly float _bufferDistance;

        public SetHoldingPositionAction(
            string targetEntityKey = "monitored_target",
            string outputKey = "steering_target",
            float bufferDistance = 15f)
        {
            _targetEntityKey = targetEntityKey;
            _outputKey = outputKey;
            _bufferDistance = bufferDistance;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Validate sensor capability via perception layer
            if (!Context.TryGet<bool>(HeuristicKeys.HasSensorModule, out var hasSensor) || !hasSensor)
            {
                return BTNodeStatus.Failure;
            }

            // Get target entity from blackboard
            if (!Context.TryGet<V2DetectedEntity>(_targetEntityKey, out var targetEntity))
            {
                return BTNodeStatus.Failure;
            }

            if (!targetEntity.IsValid)
            {
                return BTNodeStatus.Failure;
            }

            // Get positions
            Vector2 shipPos = (Vector2)Context.Transform.position;
            Vector2 targetPos = targetEntity.Position;

            // Get silhouette range from heuristics
            Context.TryGet<float>(HeuristicKeys.SilhouetteRange, out var silhouetteRange);

            // Calculate holding distance
            float holdingDistance = silhouetteRange - _bufferDistance;
            if (holdingDistance < 1f) holdingDistance = 1f;

            // Calculate direction from target to ship
            Vector2 toShip = shipPos - targetPos;
            float currentDistance = toShip.magnitude;

            Vector2 holdingPosition;

            if (currentDistance < 0.001f)
            {
                // Exactly at target - pick arbitrary direction
                holdingPosition = targetPos + Vector2.up * holdingDistance;
            }
            else
            {
                // Holding position is on the line between target and ship, at holdingDistance from target
                Vector2 directionToShip = toShip.normalized;
                holdingPosition = targetPos + directionToShip * holdingDistance;
            }

            Context.Set(_outputKey, holdingPosition);

#if UNITY_EDITOR
            if (Context.DebugLogging)
            {
                float distToHold = Vector2.Distance(shipPos, holdingPosition);
            }
#endif

            return BTNodeStatus.Success;
        }
    }
}

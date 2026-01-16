using Starfire.Entity.AI.Heuristics;
using Starfire.Entity.Modules.Sensor;
using UnityEngine;

namespace Starfire.Entity.AI.BT
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
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] SetHoldingPosition: FAIL - No sensor module (heuristic)");
#endif
                return BTNodeStatus.Failure;
            }

            // Get target entity from blackboard
            if (!Context.TryGet<DetectedEntity>(_targetEntityKey, out var targetEntity))
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] SetHoldingPosition: FAIL - No target entity at '{_targetEntityKey}'");
#endif
                return BTNodeStatus.Failure;
            }

            if (!targetEntity.IsValid)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] SetHoldingPosition: FAIL - Target entity is not valid");
#endif
                return BTNodeStatus.Failure;
            }

            // Get positions
            Vector2 shipPos = Context.Controller.transform.position;
            Vector2 targetPos = targetEntity.Controller.transform.position;

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
                Debug.Log($"[BT:{Context.EntityName}] SetHoldingPosition: holdDist={holdingDistance:F1}m, distToHoldPos={distToHold:F1}m, currentDist={currentDistance:F1}m");
            }
#endif

            return BTNodeStatus.Success;
        }
    }
}

using Starfire.Entity.AI.Steering;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Calculates steering toward a target using configurable approach strategies.
    /// Does NOT apply any forces - that's the job of ApplySteeringAction.
    ///
    /// Writes to blackboard:
    /// - steering_force (Vector2): Force to apply for physics-based movement
    /// - steering_direction (Vector2): Normalized direction to steer (legacy)
    /// - steering_throttle (float): 0-1 throttle value (legacy)
    /// - steering_arrived (bool): True when arrival conditions are met
    ///
    /// Approach strategies can be set via:
    /// - defaultStrategy parameter (static)
    /// - strategyKey blackboard entry (dynamic, overrides default)
    ///
    /// Returns Running while steering, Success when arrival conditions are met.
    /// </summary>
    public class CalculateSteeringAction : BTAction
    {
        private readonly CalculateSteeringParameters _params;

        public CalculateSteeringAction(CalculateSteeringParameters parameters = null)
        {
            _params = parameters ?? new CalculateSteeringParameters();
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            // 1. Get target from blackboard
            if (!Context.TryGet<Vector2>(_params.targetKey, out var target))
            {
                Debug.LogWarning("[CalculateSteering] No target found in blackboard");
                WriteSteeringOutput(Vector2.zero, 0f, false, Vector2.zero);
                return BTNodeStatus.Failure;
            }

            var controller = Context.Controller;
            if (controller == null || controller.Rigidbody == null)
            {
                Debug.LogWarning("[CalculateSteering] No controller or rigidbody");
                WriteSteeringOutput(Vector2.zero, 0f, false, Vector2.zero);
                return BTNodeStatus.Failure;
            }

            // 2. Get optional next waypoint for flyby strategy
            Context.TryGet<Vector2>(_params.nextWaypointKey, out var nextWaypoint);
            bool hasNextWaypoint = Context.TryGet<Vector2>(_params.nextWaypointKey, out _);

            // 3. Build approach context
            var steeringContext = SteeringContext.FromShip(controller);
            var approachContext = ApproachContext.FromSteering(
                steeringContext,
                target,
                hasNextWaypoint ? nextWaypoint : null,
                _params.arrivalThreshold,
                _params.velocityThreshold,
                _params.arrivalDistanceMultiplier,
                _params.arrivalVelocityMultiplier
            );

            // 4. Get approach strategy (from blackboard or default)
            var strategyType = Context.TryGet<ApproachStrategyType>(_params.strategyKey, out var type)
                ? type
                : _params.defaultStrategy;

            var strategy = ApproachStrategyFactory.Create(strategyType);

            // 5. Calculate steering
            var output = strategy.Calculate(approachContext);
            bool arrived = strategy.IsArrived(approachContext, output);

            // 6. Calculate legacy throttle/direction for backwards compatibility
            float throttle = 0f;
            Vector2 direction = Vector2.zero;

            if (output.SteeringForce.sqrMagnitude > 0.0001f)
            {
                direction = output.SteeringForce.normalized;
                throttle = Mathf.Clamp01(output.SteeringForce.magnitude / steeringContext.MaxAcceleration);
            }

            // 7. Write to blackboard
            WriteSteeringOutput(direction, throttle, arrived, output.SteeringForce);

            return arrived ? BTNodeStatus.Success : BTNodeStatus.Running;
        }

        private void WriteSteeringOutput(Vector2 direction, float throttle, bool arrived, Vector2 steeringForce)
        {
            Context.Set(_params.directionOutputKey, direction);
            Context.Set(_params.throttleOutputKey, throttle);
            Context.Set(_params.arrivedOutputKey, arrived);
            Context.Set(_params.steeringForceOutputKey, steeringForce);
        }

        public override void Reset()
        {
            // Clear steering output on reset
            WriteSteeringOutput(Vector2.zero, 0f, false, Vector2.zero);
        }
    }
}

using System.Linq;
using UnityEngine;
using Starfire.Entity.AI.Steering;
using Starfire.Entity.Modules.Sensor;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Calculates steering force to maintain position inside the sensor's Silhouette range.
    /// The desired distance from target = SilhouetteRange - bufferDistance.
    /// Uses a proportional controller to correct distance errors while damping
    /// lateral velocity to prevent excessive orbiting.
    /// </summary>
    public class CalculateMaintainDistanceAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _outputKey;
        private readonly float _bufferDistance;
        private readonly float _correctionFactor;
        private readonly float _lateralBrakingFactor;

        public CalculateMaintainDistanceAction(
            string targetKey = "steering_target",
            string outputKey = "steering_force",
            float bufferDistance = 15f,
            float correctionFactor = 2f,
            float lateralBrakingFactor = 1.5f)
        {
            _targetKey = targetKey;
            _outputKey = outputKey;
            _bufferDistance = bufferDistance;
            _correctionFactor = correctionFactor;
            _lateralBrakingFactor = lateralBrakingFactor;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Validate propulsion capability
            var propulsion = Context.Systems?.PrimaryImpulse;
            if (propulsion == null)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] MaintainDistance: FAIL - No propulsion");
#endif
                return BTNodeStatus.Failure;
            }

            // Get sensor module to determine Silhouette range
            var sensor = Context.Systems?.GetAllModulesOfType<ISensorShipModule>().FirstOrDefault();
            if (sensor == null)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] MaintainDistance: FAIL - No sensor module");
#endif
                return BTNodeStatus.Failure;
            }

            // Get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] MaintainDistance: FAIL - No target in blackboard");
#endif
                return BTNodeStatus.Failure;
            }

            // Calculate desired distance: stay inside Silhouette range by buffer amount
            float desiredDistance = sensor.SilhouetteRange - _bufferDistance;
            if (desiredDistance < 1f) desiredDistance = 1f; // Minimum safe distance

            var ctx = SteeringContext.FromShip(Context.Controller);
            Vector2 steeringForce = CalculateSteeringForce(ctx, target, desiredDistance);

            Context.Set(_outputKey, steeringForce);

#if UNITY_EDITOR
            if (Context.DebugLogging)
            {
                float distance = Vector2.Distance(ctx.Position, target);
                float error = distance - desiredDistance;
                string zone = error > 0 ? "OUTSIDE (correcting)" : "INSIDE (coasting)";
                Debug.Log($"[BT:{Context.EntityName}] MaintainDistance: {zone} dist={distance:F1}m, desired={desiredDistance:F1}m, error={error:F1}m");
            }
#endif

            return BTNodeStatus.Success;
        }

        private Vector2 CalculateSteeringForce(SteeringContext ctx, Vector2 target, float desiredDistance)
        {
            Vector2 toTarget = target - ctx.Position;
            float distance = toTarget.magnitude;

            // Avoid division by zero
            if (distance < 0.001f)
            {
                // Exactly at target - apply small force away
                return Vector2.up * ctx.MaxAcceleration * 0.1f;
            }

            Vector2 targetDir = toTarget.normalized;

            // Current radial velocity (positive = approaching, negative = retreating)
            float currentRadialSpeed = Vector2.Dot(ctx.Velocity, targetDir);

            // Lateral velocity damping (always apply - prevents sideways drift)
            Vector2 lateralVelocity = ctx.Velocity - (currentRadialSpeed * targetDir);
            Vector2 lateralDamping = -lateralVelocity * _lateralBrakingFactor;

            // Calculate distance error (positive = too far, negative = too close)
            float distanceError = distance - desiredDistance;

            Vector2 steeringForce;

            // Only apply radial correction when OUTSIDE acceptable zone (too far)
            if (distanceError > 0)
            {
                // Too far - apply proportional correction to move back in
                float desiredRadialSpeed = Mathf.Clamp(
                    distanceError * _correctionFactor,
                    0,
                    ctx.MaxSpeed
                );

                float radialError = desiredRadialSpeed - currentRadialSpeed;
                Vector2 radialForce = targetDir * radialError;

                steeringForce = radialForce + lateralDamping;
            }
            else
            {
                // Inside acceptable zone - coast naturally, only damp lateral velocity
                steeringForce = lateralDamping;
            }

            // Clamp to max acceleration
            if (steeringForce.sqrMagnitude > ctx.MaxAcceleration * ctx.MaxAcceleration)
            {
                steeringForce = steeringForce.normalized * ctx.MaxAcceleration;
            }

            return steeringForce;
        }
    }
}

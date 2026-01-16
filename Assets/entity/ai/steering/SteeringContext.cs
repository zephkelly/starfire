using Starfire.Entity.AI.BT;
using Starfire.Entity.AI.Heuristics;
using UnityEngine;

namespace Starfire.Entity.AI.Steering
{
    /// <summary>
    /// Context containing all physics data needed for steering calculations.
    /// </summary>
    public class SteeringContext
    {
        /// <summary>
        /// Current world position.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Current velocity.
        /// </summary>
        public Vector2 Velocity;

        /// <summary>
        /// Maximum speed the agent can achieve.
        /// </summary>
        public float MaxSpeed;

        /// <summary>
        /// Maximum acceleration/deceleration force.
        /// </summary>
        public float MaxAcceleration;

        /// <summary>
        /// Mass of the agent (affects force calculations).
        /// </summary>
        public float Mass;

        /// <summary>
        /// Maximum rotation speed in degrees per second.
        /// </summary>
        public float RotationSpeed;

        /// <summary>
        /// Creates a SteeringContext from a ShipController's current state.
        /// </summary>
        public static SteeringContext FromShip(ShipController controller)
        {
            return FromShip(controller, null, null);
        }

        /// <summary>
        /// Creates a SteeringContext from a ShipController with optional cruise speed/acceleration overrides.
        /// Overrides are clamped to the ship's actual maximum capabilities.
        /// Use -1 or null to indicate "use ship's maximum" (no override).
        /// </summary>
        public static SteeringContext FromShip(ShipController controller, float? cruiseSpeed, float? cruiseAccel)
        {
            var propulsion = controller.ShipSystems.PrimaryImpulse;
            var rotation = controller.ShipSystems.PrimaryRotation;

            float shipMaxSpeed = propulsion?.MaxSpeed ?? 10f;
            float shipMaxAccel = propulsion?.Acceleration ?? 5f;

            // Apply cruise overrides, clamping to ship's actual capabilities
            float effectiveMaxSpeed = shipMaxSpeed;
            float effectiveMaxAccel = shipMaxAccel;

            if (cruiseSpeed.HasValue && cruiseSpeed.Value > 0)
            {
                effectiveMaxSpeed = Mathf.Min(cruiseSpeed.Value, shipMaxSpeed);
            }

            if (cruiseAccel.HasValue && cruiseAccel.Value > 0)
            {
                effectiveMaxAccel = Mathf.Min(cruiseAccel.Value, shipMaxAccel);
            }

            return new SteeringContext
            {
                Position = controller.transform.position,
                Velocity = controller.Rigidbody.linearVelocity,
                MaxSpeed = effectiveMaxSpeed,
                MaxAcceleration = effectiveMaxAccel,
                Mass = controller.Rigidbody.mass,
                RotationSpeed = rotation?.RotationSpeed ?? 180f
            };
        }

        /// <summary>
        /// Creates a SteeringContext reading capabilities from blackboard (perception layer pattern).
        /// Physics state (position, velocity) from controller, capabilities from heuristics.
        /// </summary>
        public static SteeringContext FromBlackboard(ShipController controller, BTContext btContext, float? cruiseSpeed = null, float? cruiseAccel = null)
        {
            // Get capability values from blackboard heuristics
            float shipMaxSpeed = btContext.TryGet<float>(HeuristicKeys.MaxSpeed, out var maxSpeed) ? maxSpeed : 10f;
            float shipMaxAccel = btContext.TryGet<float>(HeuristicKeys.MaxAcceleration, out var maxAccel) ? maxAccel : 5f;

            // Apply cruise overrides, clamping to ship's actual capabilities
            float effectiveMaxSpeed = shipMaxSpeed;
            float effectiveMaxAccel = shipMaxAccel;

            if (cruiseSpeed.HasValue && cruiseSpeed.Value > 0)
            {
                effectiveMaxSpeed = Mathf.Min(cruiseSpeed.Value, shipMaxSpeed);
            }

            if (cruiseAccel.HasValue && cruiseAccel.Value > 0)
            {
                effectiveMaxAccel = Mathf.Min(cruiseAccel.Value, shipMaxAccel);
            }

            // Get rotation from ship systems (TODO: migrate to heuristics if needed)
            var rotation = controller.ShipSystems.PrimaryRotation;

            return new SteeringContext
            {
                Position = controller.transform.position,
                Velocity = controller.Rigidbody.linearVelocity,
                MaxSpeed = effectiveMaxSpeed,
                MaxAcceleration = effectiveMaxAccel,
                Mass = controller.Rigidbody.mass,
                RotationSpeed = rotation?.RotationSpeed ?? 180f
            };
        }
    }
}

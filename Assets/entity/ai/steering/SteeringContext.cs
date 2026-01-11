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
            var propulsion = controller.ShipSystems.Propulsion.Module;
            var rotation = controller.ShipSystems.Rotation.Module;

            return new SteeringContext
            {
                Position = controller.transform.position,
                Velocity = controller.Rigidbody.linearVelocity,
                MaxSpeed = propulsion?.MaxSpeed ?? 10f,
                MaxAcceleration = propulsion?.Acceleration ?? 5f,
                Mass = controller.Rigidbody.mass,
                RotationSpeed = rotation?.RotationSpeed ?? 180f
            };
        }
    }
}

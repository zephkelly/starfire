using System.Linq;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Context containing all physics data needed for steering calculations.
    /// </summary>
    public class SteeringContext
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float MaxSpeed;
        public float MaxAcceleration;
        public float Mass;
        public float RotationSpeed;

        /// <summary>
        /// Creates a SteeringContext from an IEntityController's current state.
        /// </summary>
        public static SteeringContext FromController(IEntityController controller)
        {
            return FromController(controller, null, null);
        }

        /// <summary>
        /// Creates a SteeringContext from an IEntityController with optional cruise speed/acceleration overrides.
        /// </summary>
        public static SteeringContext FromController(IEntityController controller, float? cruiseSpeed, float? cruiseAccel)
        {
            var modules = (controller.Entity as ShipEntity)?.Modules;

            float shipMaxSpeed = 10f;
            float shipMaxAccel = 5f;
            float rotSpeed = 180f;

            if (modules != null)
            {
                var propulsion = modules.GetAllModulesOfType<IShipPropulsionModule>().FirstOrDefault();
                if (propulsion != null)
                {
                    shipMaxSpeed = propulsion.MaxSpeed;
                    shipMaxAccel = propulsion.Acceleration;
                }

                var rotation = modules.GetAllModulesOfType<IShipRotationModule>().FirstOrDefault();
                if (rotation != null)
                {
                    rotSpeed = rotation.RotationSpeed;
                }
            }

            float effectiveMaxSpeed = shipMaxSpeed;
            float effectiveMaxAccel = shipMaxAccel;

            if (cruiseSpeed.HasValue && cruiseSpeed.Value > 0)
                effectiveMaxSpeed = Mathf.Min(cruiseSpeed.Value, shipMaxSpeed);

            if (cruiseAccel.HasValue && cruiseAccel.Value > 0)
                effectiveMaxAccel = Mathf.Min(cruiseAccel.Value, shipMaxAccel);

            return new SteeringContext
            {
                Position = controller.Transform.position,
                Velocity = controller.Rigid2D.linearVelocity,
                MaxSpeed = effectiveMaxSpeed,
                MaxAcceleration = effectiveMaxAccel,
                Mass = controller.Rigid2D.mass,
                RotationSpeed = rotSpeed
            };
        }

        /// <summary>
        /// Creates a SteeringContext reading capabilities from blackboard (perception layer pattern).
        /// Physics state from controller, capabilities from heuristics.
        /// </summary>
        public static SteeringContext FromBlackboard(IEntityController controller, BTContext btContext, float? cruiseSpeed = null, float? cruiseAccel = null)
        {
            float shipMaxSpeed = btContext.TryGet<float>(HeuristicKeys.MaxSpeed, out var maxSpeed) ? maxSpeed : 10f;
            float shipMaxAccel = btContext.TryGet<float>(HeuristicKeys.MaxAcceleration, out var maxAccel) ? maxAccel : 5f;

            float effectiveMaxSpeed = shipMaxSpeed;
            float effectiveMaxAccel = shipMaxAccel;

            if (cruiseSpeed.HasValue && cruiseSpeed.Value > 0)
                effectiveMaxSpeed = Mathf.Min(cruiseSpeed.Value, shipMaxSpeed);

            if (cruiseAccel.HasValue && cruiseAccel.Value > 0)
                effectiveMaxAccel = Mathf.Min(cruiseAccel.Value, shipMaxAccel);

            float rotSpeed = 180f;
            var modules = (controller.Entity as ShipEntity)?.Modules;
            if (modules != null)
            {
                var rotation = modules.GetAllModulesOfType<IShipRotationModule>().FirstOrDefault();
                if (rotation != null)
                    rotSpeed = rotation.RotationSpeed;
            }

            return new SteeringContext
            {
                Position = controller.Transform.position,
                Velocity = controller.Rigid2D.linearVelocity,
                MaxSpeed = effectiveMaxSpeed,
                MaxAcceleration = effectiveMaxAccel,
                Mass = controller.Rigid2D.mass,
                RotationSpeed = rotSpeed
            };
        }

    }
}

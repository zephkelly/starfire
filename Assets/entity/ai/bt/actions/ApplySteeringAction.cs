using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Applies steering by setting the driver's DesiredAcceleration (physics-based steering).
    /// Reads steering_force and steering_arrived from blackboard (written by CalculateSteeringAction).
    ///
    /// Movement is applied through the ship controller's ProcessMovement system,
    /// which detects DesiredAcceleration and applies it directly to physics.
    ///
    /// Use with BTParallel to run alongside CalculateSteeringAction:
    ///   Parallel:
    ///     - CalculateSteering
    ///     - ApplySteering
    ///
    /// Returns Success when steering_arrived is true, Running while moving.
    /// </summary>
    public class ApplySteeringAction : BTAction
    {
        private readonly ApplySteeringParameters _params;

        public ApplySteeringAction(ApplySteeringParameters parameters = null)
        {
            _params = parameters ?? new ApplySteeringParameters();
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            // 1. Read steering force from blackboard
            if (!Context.TryGet<Vector2>(_params.steeringForceKey, out var steeringForce))
            {
                return BTNodeStatus.Failure;
            }

            Context.TryGet<bool>(_params.arrivedKey, out var arrived);

            // 2. Check if arrived
            if (arrived)
            {
                Context.Driver.DesiredAcceleration = Vector2.zero;
                return BTNodeStatus.Success;
            }

            // 3. Apply steering force through the driver (physics-based)
            // The ShipController.ProcessMovement will detect DesiredAcceleration and apply it
            Context.Driver.DesiredAcceleration = steeringForce;

            // 4. Update aim position for rotation
            if (_params.aimAtTarget && Context.TryGet<Vector2>(_params.targetKey, out var target))
            {
                Context.Driver.AimPosition = target;
            }

            return BTNodeStatus.Running;
        }

        public override void Reset()
        {
            if (Context?.Driver != null)
            {
                Context.Driver.DesiredAcceleration = Vector2.zero;
            }
        }
    }
}

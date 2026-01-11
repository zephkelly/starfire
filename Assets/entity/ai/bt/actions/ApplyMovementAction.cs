using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Reads steering data from blackboard and sets Driver movement.
    /// Does NOT control rotation - use AimAtTarget or AimInMovementDirection for that.
    /// Returns Success always (fire-and-forget per frame).
    /// </summary>
    public class ApplyMovementAction : BTAction
    {
        private readonly ApplyMovementParameters _params;

        public ApplyMovementAction(ApplyMovementParameters parameters = null)
        {
            _params = parameters ?? new ApplyMovementParameters();
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            // Read steering data from blackboard
            if (!Context.TryGet<Vector2>(_params.directionKey, out var direction))
            {
                Context.Driver.MovementDirection = Vector2.zero;
                Context.Driver.Throttle = 0f;
                return BTNodeStatus.Success;
            }

            if (!Context.TryGet<float>(_params.throttleKey, out var throttle))
            {
                throttle = 1f;
            }

            // Apply movement through driver
            if (direction.sqrMagnitude > 0.0001f && throttle > 0.001f)
            {
                Context.Driver.MovementDirection = direction.normalized;
                Context.Driver.Throttle = Mathf.Clamp01(throttle);
            }
            else
            {
                Context.Driver.MovementDirection = Vector2.zero;
                Context.Driver.Throttle = 0f;
            }

            return BTNodeStatus.Success;
        }

        public override void Reset()
        {
            if (Context?.Driver != null)
            {
                Context.Driver.MovementDirection = Vector2.zero;
                Context.Driver.Throttle = 0f;
            }
        }
    }
}

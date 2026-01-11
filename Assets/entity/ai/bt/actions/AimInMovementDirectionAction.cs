using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Sets the driver's aim position to match its movement direction.
    /// Ship will face forward in the direction it's moving.
    /// Returns Success always.
    /// </summary>
    public class AimInMovementDirectionAction : BTAction
    {
        private readonly float _lookAheadDistance;

        public AimInMovementDirectionAction(float lookAheadDistance = 10f)
        {
            _lookAheadDistance = lookAheadDistance;
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            var direction = Context.Driver.MovementDirection;

            if (direction.sqrMagnitude > 0.0001f)
            {
                // Set aim position ahead of current position in movement direction
                Vector2 currentPos = Context.Transform.position;
                Context.Driver.AimPosition = currentPos + direction.normalized * _lookAheadDistance;
            }

            return BTNodeStatus.Success;
        }
    }
}

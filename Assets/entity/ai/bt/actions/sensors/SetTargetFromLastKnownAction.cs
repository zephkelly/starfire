using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Sets steering target to last known position of a lost contact.
    /// Returns Success if position exists, Failure if not set.
    /// </summary>
    public class SetTargetFromLastKnownAction : BTAction
    {
        private readonly string _lastPositionKey;
        private readonly string _steeringTargetKey;

        public SetTargetFromLastKnownAction(
            string lastPositionKey = "last_known_position",
            string steeringTargetKey = "steering_target")
        {
            _lastPositionKey = lastPositionKey;
            _steeringTargetKey = steeringTargetKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            if (!Context.TryGet<Vector2>(_lastPositionKey, out var position))
            {
                return BTNodeStatus.Failure;
            }

            Context.Set(_steeringTargetKey, position);
            return BTNodeStatus.Success;
        }
    }
}

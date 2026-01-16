using UnityEngine;
using Starfire.Entity.Modules.Sensor;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Copies position from investigation target to steering target.
    /// Returns Success if target valid, Failure if null.
    /// </summary>
    public class SetTargetFromInvestigationAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _steeringTargetKey;

        public SetTargetFromInvestigationAction(
            string targetKey = "investigation_target",
            string steeringTargetKey = "steering_target")
        {
            _targetKey = targetKey;
            _steeringTargetKey = steeringTargetKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            if (!Context.TryGet<DetectedEntity>(_targetKey, out var target))
            {
                return BTNodeStatus.Failure;
            }

            if (!target.IsValid)
            {
                return BTNodeStatus.Failure;
            }

            Vector2 position = target.Position;
            Context.Set(_steeringTargetKey, position);

            return BTNodeStatus.Success;
        }
    }
}

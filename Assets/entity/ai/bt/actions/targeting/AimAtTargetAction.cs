using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Sets the driver's aim position from a blackboard target.
    /// Allows ship to aim at a different position than it's moving toward.
    /// Returns Success always.
    /// </summary>
    public class AimAtTargetAction : BTAction
    {
        private readonly string _targetKey;

        public AimAtTargetAction(string targetKey = "move_target")
        {
            _targetKey = targetKey;
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            if (Context.TryGet<Vector2>(_targetKey, out var target))
            {
                Context.Driver.AimPosition = target;
            }
            return BTNodeStatus.Success;
        }
    }
}

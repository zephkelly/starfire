using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that checks if the entity is within a threshold distance of a target position.
    /// Returns Success if at target, Failure otherwise.
    ///
    /// Can be used standalone or as part of a sequence:
    ///   Selector:
    ///     - Sequence:
    ///         - IsAtTarget
    ///         - AdvanceWaypoint
    ///     - MoveTo
    /// </summary>
    public class IsAtTargetCondition : BTAction
    {
        private readonly string _targetKey;
        private readonly float _threshold;

        /// <summary>
        /// Creates a new IsAtTargetCondition.
        /// </summary>
        /// <param name="targetKey">Blackboard key for the target position (Vector2).</param>
        /// <param name="threshold">Distance threshold to consider "at target".</param>
        public IsAtTargetCondition(string targetKey = "move_target", float threshold = 1f)
        {
            _targetKey = targetKey;
            _threshold = threshold;
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            // Get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
                return BTNodeStatus.Failure;
            }

            // Get current position
            Vector2 currentPos = Context.Transform.position;

            // Check distance
            float distance = Vector2.Distance(currentPos, target);

            return distance <= _threshold ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }
}

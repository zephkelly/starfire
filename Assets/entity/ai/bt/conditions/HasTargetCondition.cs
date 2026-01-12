namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that checks if a blackboard key exists and has a non-null value.
    /// Returns Success if target exists, Failure otherwise.
    ///
    /// Useful as a guard before actions that require a target:
    ///   Sequence:
    ///     - HasTarget (entityKey: "target_entity")
    ///     - SetEntityAsTarget
    ///     - CalculateSteering
    /// </summary>
    public class HasTargetCondition : BTAction
    {
        private readonly string _targetKey;

        public HasTargetCondition(string targetKey = "target_entity")
        {
            _targetKey = targetKey;
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            // Check if key exists
            if (!Context.Has(_targetKey))
            {
                return BTNodeStatus.Failure;
            }

            // Check if value is non-null
            var value = Context.Get<object>(_targetKey);
            return value != null ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }
}


namespace StarfireV2
{
    /// <summary>
    /// Checks if the current goal matches a specific type.
    /// Used for goal-driven branching in the master BT.
    /// </summary>
    public class IsCurrentGoalCondition : BTLeafCondition
    {
        private readonly GoalType _expectedType;
        private readonly string _goalTypeKey;

        public IsCurrentGoalCondition(GoalType expectedType, string goalTypeKey = "current_goal_type")
        {
            _expectedType = expectedType;
            _goalTypeKey = goalTypeKey;
        }

        protected override bool CheckCondition()
        {
            if (Context.TryGet<GoalType>(_goalTypeKey, out var currentType))
            {
                return currentType == _expectedType;
            }
            return false;
        }
    }
}

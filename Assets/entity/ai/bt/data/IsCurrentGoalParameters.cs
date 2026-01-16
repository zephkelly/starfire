using System;
using Starfire.Entity.AI.Goals;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for IsCurrentGoalCondition.
    /// Checks if the current goal matches a specific type.
    /// </summary>
    [Serializable]
    public class IsCurrentGoalParameters : IBTNodeParameters
    {
        /// <summary>
        /// The goal type to check for.
        /// </summary>
        public GoalType expectedGoalType = GoalType.Patrol;

        /// <summary>
        /// Blackboard key for the current goal type.
        /// </summary>
        public string goalTypeKey = "current_goal_type";

        /// <summary>
        /// If true, inverts the condition result.
        /// </summary>
        public bool invert = false;
    }
}

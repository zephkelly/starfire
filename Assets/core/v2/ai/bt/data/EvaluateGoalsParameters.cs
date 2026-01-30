using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for EvaluateGoalsAction.
    /// Triggers goal evaluation and potential goal switching.
    /// </summary>
    [Serializable]
    public class EvaluateGoalsParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key for the GoalManager reference.
        /// </summary>
        public string goalManagerKey = "goal_manager";
    }
}


namespace StarfireV2
{
    /// <summary>
    /// Base implementation of IGoal.
    /// Provides common functionality for goal activation/deactivation.
    /// </summary>
    public abstract class Goal : IGoal
    {
        public abstract GoalType Type { get; }
        public GoalParameters Parameters { get; }
        public float BasePriority { get; set; } = 1f;
        public bool IsCommanderAssigned { get; set; }

        protected Goal(GoalParameters parameters)
        {
            Parameters = parameters;
        }

        public virtual void Activate(BTContext context)
        {
            Parameters?.WriteToBlackboard(context);
            context.Set(GoalKeys.CurrentGoalType, Type);
            context.Set(GoalKeys.CurrentGoal, this);
            context.Set(GoalKeys.IsCommanderAssigned, IsCommanderAssigned);
        }

        public virtual void Deactivate(BTContext context)
        {
            Parameters?.ClearFromBlackboard(context);
            context.Remove(GoalKeys.CurrentGoalType);
            context.Remove(GoalKeys.CurrentGoal);
            context.Remove(GoalKeys.IsCommanderAssigned);
        }

        /// <summary>
        /// Override to implement situation-specific utility scoring.
        /// </summary>
        public abstract float Evaluate(HeuristicData heuristics, BTContext context);

        /// <summary>
        /// Override to implement goal completion logic.
        /// Returns false by default (goal never completes on its own).
        /// </summary>
        public virtual bool IsComplete(BTContext context)
        {
            return false;
        }
    }
}

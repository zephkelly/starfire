
namespace StarfireV2
{
    /// <summary>
    /// Runtime goal instance. Created from GoalParameters, manages active goal state.
    /// </summary>
    public interface IGoal
    {
        /// <summary>
        /// The type of this goal.
        /// </summary>
        GoalType Type { get; }

        /// <summary>
        /// The parameters asset this goal was created from. May be null for code-created goals.
        /// </summary>
        GoalParameters Parameters { get; }

        /// <summary>
        /// Base priority of this goal. Higher = more important.
        /// Used as a multiplier for utility scoring.
        /// </summary>
        float BasePriority { get; set; }

        /// <summary>
        /// True if this goal was assigned by a commander, not self-selected.
        /// Commander-assigned goals get a priority bonus during evaluation.
        /// </summary>
        bool IsCommanderAssigned { get; set; }

        /// <summary>
        /// Called when this goal becomes active. Writes parameters to blackboard.
        /// </summary>
        void Activate(BTContext context);

        /// <summary>
        /// Called when this goal is deactivated. Cleans up blackboard.
        /// </summary>
        void Deactivate(BTContext context);

        /// <summary>
        /// Calculate utility score for this goal given current heuristics.
        /// Higher scores indicate this goal is more appropriate for the current situation.
        /// </summary>
        float Evaluate(HeuristicData heuristics, BTContext context);

        /// <summary>
        /// Check if goal completion conditions are met.
        /// Some goals (like Patrol) never complete; others (like Flee) complete when safe.
        /// </summary>
        bool IsComplete(BTContext context);
    }
}

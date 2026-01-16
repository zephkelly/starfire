using System.Collections.Generic;
using Starfire.Entity.AI.BT;
using Starfire.Entity.AI.Heuristics;

namespace Starfire.Entity.AI.Goals
{
    /// <summary>
    /// Utility-based goal selection using heuristics.
    /// Evaluates all available goals and returns the best one.
    /// </summary>
    public class GoalEvaluator
    {
        /// <summary>
        /// Priority bonus for commander-assigned goals.
        /// </summary>
        public float CommanderBonus { get; set; } = 100f;

        /// <summary>
        /// Evaluate all goals and return the best one with its score.
        /// </summary>
        public (IGoal goal, float score) SelectBest(
            IEnumerable<IGoal> goals,
            HeuristicData heuristics,
            BTContext context)
        {
            IGoal bestGoal = null;
            float bestScore = float.MinValue;

            foreach (var goal in goals)
            {
                float score = goal.Evaluate(heuristics, context);

                // Apply base priority multiplier
                score *= goal.BasePriority;

                // Commander-assigned goals get a large bonus
                if (goal.IsCommanderAssigned)
                {
                    score += CommanderBonus;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestGoal = goal;
                }
            }

            return (bestGoal, bestScore);
        }
    }
}

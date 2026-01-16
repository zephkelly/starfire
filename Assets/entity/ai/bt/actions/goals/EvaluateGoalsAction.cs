using Starfire.Entity.AI.Goals;
using Starfire.Entity.AI.Heuristics;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Triggers goal evaluation and potential goal switching.
    /// Should run early in the BT, after UpdateHeuristicsAction.
    /// Always returns Success.
    /// </summary>
    public class EvaluateGoalsAction : BTAction
    {
        private readonly string _goalManagerKey;

        public EvaluateGoalsAction(string goalManagerKey = "goal_manager")
        {
            _goalManagerKey = goalManagerKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            if (!Context.TryGet<GoalManager>(_goalManagerKey, out var manager))
            {
                return BTNodeStatus.Failure;
            }

            if (!Context.TryGet<HeuristicData>(HeuristicKeys.HeuristicData, out var heuristics))
            {
                return BTNodeStatus.Failure;
            }

            manager.EvaluateAndUpdate(heuristics);
            return BTNodeStatus.Success;
        }
    }
}

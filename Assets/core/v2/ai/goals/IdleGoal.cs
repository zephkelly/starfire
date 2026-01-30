
namespace StarfireV2
{
    /// <summary>
    /// Fallback idle goal. Always available, lowest priority.
    /// Used when no other goal is appropriate.
    /// </summary>
    public class IdleGoal : Goal
    {
        public override GoalType Type => GoalType.Idle;

        public IdleGoal() : base(null)
        {
            BasePriority = 0.1f; // Very low priority
        }

        public override void Activate(BTContext context)
        {
            base.Activate(context);

            // Stop moving when idle
            context.Set("cruise_speed", 0f);
        }

        public override float Evaluate(HeuristicData heuristics, BTContext context)
        {
            // Idle has a very low base score - only chosen when nothing else applies
            return 0.1f;
        }

        public override bool IsComplete(BTContext context)
        {
            // Idle never completes - it's the fallback state
            return false;
        }
    }
}

using Starfire.Entity.AI.BT;
using Starfire.Entity.AI.Heuristics;

namespace Starfire.Entity.AI.Goals
{
    /// <summary>
    /// Runtime flee goal. Scores high when ship is damaged or threatened.
    /// Completes when ship reaches safety (low threat, reasonable health).
    /// </summary>
    public class FleeGoal : Goal
    {
        public override GoalType Type => GoalType.Flee;

        private FleeGoalParameters FleeParams => Parameters as FleeGoalParameters;

        public FleeGoal(FleeGoalParameters parameters) : base(parameters) { }

        public override float Evaluate(HeuristicData heuristics, BTContext context)
        {
            // Flee when: low health, high threat, low confidence
            // Skittishness directly maps to flee desire
            float score = heuristics.Skittishness * 0.5f + heuristics.ThreatLevel * 0.5f;

            // Critical hull triggers strong flee desire
            if (heuristics.HullPercent < 0.25f)
            {
                score += 0.5f;
            }

            // Shields down with hostiles nearby
            if (heuristics.ShieldPercent < 0.1f && heuristics.NearbyHostileCount > 0)
            {
                score += 0.3f;
            }

            // Multiple hostiles = more reason to flee
            if (heuristics.NearbyHostileCount > 2)
            {
                score += 0.2f;
            }

            return score;
        }

        public override bool IsComplete(BTContext context)
        {
            // Flee completes when we're safe
            if (!context.TryGet<HeuristicData>(HeuristicKeys.HeuristicData, out var h))
            {
                return false;
            }

            float safeConfidence = FleeParams?.safeConfidenceThreshold ?? 0.5f;
            float safeThreat = FleeParams?.safeThreatThreshold ?? 0.1f;

            return h.ThreatLevel < safeThreat && h.Confidence > safeConfidence;
        }
    }
}

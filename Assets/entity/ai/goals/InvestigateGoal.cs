using Starfire.Entity.AI.BT;
using Starfire.Entity.AI.Heuristics;
using Starfire.Entity.Modules.Sensor;
using UnityEngine;

namespace Starfire.Entity.AI.Goals
{
    /// <summary>
    /// Runtime investigate goal. Scores high when there are unidentified contacts.
    /// Completes when target reaches the configured detection level.
    /// </summary>
    public class InvestigateGoal : Goal
    {
        public override GoalType Type => GoalType.Investigate;

        private InvestigateGoalParameters InvestigateParams => Parameters as InvestigateGoalParameters;

        public InvestigateGoal(InvestigateGoalParameters parameters) : base(parameters) { }

        public override float Evaluate(HeuristicData heuristics, BTContext context)
        {
            // Base score starts low - only investigate when there's something to investigate
            float score = 0.1f;

            // Primary driver: unidentified contacts detected by sensors
            if (heuristics.HasUnidentifiedContacts)
            {
                // Strong score boost when there are contacts to investigate
                score = 0.9f;

                // More contacts = more urgency (up to +0.3 for 2+ contacts)
                score += Mathf.Clamp01(heuristics.UnidentifiedContactCount * 0.15f);

                // Closer contacts = more urgency (up to +0.3 for very close)
                if (heuristics.ClosestUnidentifiedDistance < 100f)
                {
                    score += (1f - heuristics.ClosestUnidentifiedDistance / 100f) * 0.3f;
                }
            }

            // Higher score if we already have an active investigation target (continuity)
            if (context.Has("investigation_target"))
            {
                score += 0.2f;
            }

            // Reduce score if we're damaged - self-preservation first
            if (heuristics.HullPercent < 0.5f)
            {
                score *= 0.5f;
            }

            // Reduce score under high threat - focus on survival
            if (heuristics.ThreatLevel > 0.7f)
            {
                score *= 0.3f;
            }

            return score;
        }

        public override bool IsComplete(BTContext context)
        {
            // Investigation completes when target reaches goal detection level
            if (!context.TryGet<DetectedEntity>("investigation_target", out var target))
            {
                // No target - investigation might be complete or need re-evaluation
                return false;
            }

            var goalLevel = InvestigateParams?.goalDetectionLevel ?? DetectionLevel.Silhouette;
            return target.Level >= goalLevel;
        }
    }
}

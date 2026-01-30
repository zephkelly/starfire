using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Runtime patrol goal. Scores high when ship is healthy and threat is low.
    /// Can generate waypoints around current position on activation.
    /// </summary>
    public class PatrolGoal : Goal
    {
        public override GoalType Type => GoalType.Patrol;

        private PatrolGoalParameters PatrolParams => Parameters as PatrolGoalParameters;

        public PatrolGoal(PatrolGoalParameters parameters) : base(parameters) { }

        public override void Activate(BTContext context)
        {
            base.Activate(context);

            // Generate waypoints if configured and none exist
            if (PatrolParams?.generateWaypointsOnActivate == true && !context.Has("waypoint_list"))
            {
                GenerateWaypoints(context);
            }
        }

        public override float Evaluate(HeuristicData heuristics, BTContext context)
        {
            // Patrol is default behavior when safe
            // Score: high confidence + low threat = good for patrol
            float score = heuristics.Confidence * 0.6f + (1f - heuristics.ThreatLevel) * 0.4f;

            // Small bonus if we already have waypoints (continuity)
            if (context.Has("waypoint_list") || context.Has("waypoint_stack"))
            {
                score += 0.05f;
            }

            // Penalty if hull is below 50% (should consider fleeing/repairing instead)
            if (heuristics.HullPercent < 0.5f)
            {
                score *= 0.5f;
            }

            // Penalty when there are unidentified contacts - should investigate them first
            if (heuristics.HasUnidentifiedContacts)
            {
                score *= 0.7f;
            }

            return score;
        }

        public override bool IsComplete(BTContext context)
        {
            // Patrol never completes - it's an ongoing activity
            return false;
        }

        private void GenerateWaypoints(BTContext context)
        {
            // Get center (from blackboard or current position)
            string centerKey = PatrolParams?.patrolCenterKey ?? GoalKeys.PatrolCenter;
            Vector2 center = context.TryGet<Vector2>(centerKey, out var c)
                ? c
                : context.Transform.position;

            float radius = PatrolParams?.patrolRadius ?? 50f;
            int count = PatrolParams?.waypointCount ?? 4;

            // Generate evenly-spaced waypoints in a circle
            var waypoints = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                // Add some randomization to make patterns less predictable
                float r = radius * Random.Range(0.8f, 1.2f);
                waypoints.Add(center + new Vector2(
                    Mathf.Cos(angle) * r,
                    Mathf.Sin(angle) * r
                ));
            }

            context.Set("waypoint_list", waypoints);

            // Store center for reference
            if (!context.Has(centerKey))
            {
                context.Set(centerKey, center);
            }
        }
    }
}

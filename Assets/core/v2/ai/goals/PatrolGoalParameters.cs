using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration parameters for patrol behavior.
    /// Controls patrol area, speed, and waypoint generation.
    /// </summary>
    [CreateAssetMenu(menuName = "StarfireV2/AI/Goals/Patrol Parameters")]
    public class PatrolGoalParameters : GoalParameters
    {
        [Header("Patrol Configuration")]
        [Tooltip("Radius around the patrol center to generate waypoints.")]
        public float patrolRadius = 50f;

        [Tooltip("Speed to cruise during patrol.")]
        public float cruiseSpeed = 30f;

        [Tooltip("Number of waypoints to generate in the patrol pattern.")]
        public int waypointCount = 4;

        [Tooltip("If true, generates waypoints around current position when activated if none exist.")]
        public bool generateWaypointsOnActivate = true;

        [Header("Blackboard Keys")]
        [Tooltip("Key for the patrol center position.")]
        public string patrolCenterKey = GoalKeys.PatrolCenter;

        [Tooltip("Key for the patrol radius.")]
        public string patrolRadiusKey = GoalKeys.PatrolRadius;

        [Tooltip("Key for the cruise speed.")]
        public string cruiseSpeedKey = "cruise_speed";

        public override void WriteToBlackboard(BTContext context)
        {
            context.Set(patrolRadiusKey, patrolRadius);
            context.Set(cruiseSpeedKey, cruiseSpeed);

            // If no center is set, use current position
            if (!context.Has(patrolCenterKey))
            {
                context.Set(patrolCenterKey, (Vector2)context.Transform.position);
            }
        }

        public override void ClearFromBlackboard(BTContext context)
        {
            context.Remove(patrolRadiusKey);
            // Keep patrol_center - might be reused by commander
        }
    }
}

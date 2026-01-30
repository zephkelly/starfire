using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration parameters for flee behavior.
    /// Controls when flee completes and movement during escape.
    /// </summary>
    [CreateAssetMenu(menuName = "StarfireV2/AI/Goals/Flee Parameters")]
    public class FleeGoalParameters : GoalParameters
    {
        [Header("Flee Configuration")]
        [Tooltip("Distance at which the ship considers itself safe from threats.")]
        public float safeDistance = 100f;

        [Tooltip("Speed to flee at (typically max speed).")]
        public float fleeSpeed = 50f;

        [Tooltip("Confidence threshold above which fleeing can end.")]
        public float safeConfidenceThreshold = 0.5f;

        [Tooltip("Threat level below which fleeing can end.")]
        public float safeThreatThreshold = 0.1f;

        [Header("Blackboard Keys")]
        [Tooltip("Key for the safe distance.")]
        public string safeDistanceKey = GoalKeys.SafeDistance;

        [Tooltip("Key for the flee speed.")]
        public string fleeSpeedKey = "flee_speed";

        public override void WriteToBlackboard(BTContext context)
        {
            context.Set(safeDistanceKey, safeDistance);
            context.Set(fleeSpeedKey, fleeSpeed);
            context.Set("cruise_speed", fleeSpeed); // Override cruise speed for steering
        }

        public override void ClearFromBlackboard(BTContext context)
        {
            context.Remove(safeDistanceKey);
            context.Remove(fleeSpeedKey);
        }
    }
}

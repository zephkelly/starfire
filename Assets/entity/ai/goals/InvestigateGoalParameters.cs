using Starfire.Entity.AI.BT;
using Starfire.Entity.Modules.Sensor;
using UnityEngine;

namespace Starfire.Entity.AI.Goals
{
    /// <summary>
    /// Configuration parameters for investigation behavior.
    /// Controls how deep the ship investigates contacts (to which detection level)
    /// and movement parameters during investigation.
    /// </summary>
    [CreateAssetMenu(menuName = "Starfire/AI/Goals/Investigate Parameters")]
    public class InvestigateGoalParameters : GoalParameters
    {
        [Header("Investigation Configuration")]
        [Tooltip("The detection level the ship should reach before considering investigation complete.")]
        public DetectionLevel goalDetectionLevel = DetectionLevel.Silhouette;

        [Tooltip("Speed to approach investigation targets.")]
        public float approachSpeed = 25f;

        [Tooltip("Distance to maintain when holding position near a classified target.")]
        public float holdingDistance = 15f;

        [Tooltip("If true, engage hostiles when detected. If false, just classify and monitor.")]
        public bool engageHostiles = false;

        [Header("Blackboard Keys")]
        [Tooltip("Key for the goal detection level threshold.")]
        public string goalLevelKey = "investigation_goal_level";

        [Tooltip("Key for the approach speed.")]
        public string approachSpeedKey = "investigation_approach_speed";

        [Tooltip("Key for the holding distance.")]
        public string holdingDistanceKey = "investigation_holding_distance";

        [Tooltip("Key for the engage hostiles flag.")]
        public string engageHostilesKey = "investigation_engage_hostiles";

        public override void WriteToBlackboard(BTContext context)
        {
            context.Set(goalLevelKey, goalDetectionLevel);
            context.Set(approachSpeedKey, approachSpeed);
            context.Set(holdingDistanceKey, holdingDistance);
            context.Set(engageHostilesKey, engageHostiles);
        }

        public override void ClearFromBlackboard(BTContext context)
        {
            context.Remove(goalLevelKey);
            context.Remove(approachSpeedKey);
            context.Remove(holdingDistanceKey);
            context.Remove(engageHostilesKey);
        }
    }
}

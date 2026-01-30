using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Bootstrap component for AI-controlled ships in v2.
    /// Finds the AI core module and writes waypoint data to the behavior tree blackboard.
    /// If no waypoints are assigned, the PatrolGoal will auto-generate them.
    /// </summary>
    [RequireComponent(typeof(ShipController))]
    public class V2AISetup : MonoBehaviour
    {
        [Header("Patrol Waypoints (Optional)")]
        [Tooltip("Assign Transform waypoints for manual patrol routes. If empty, the PatrolGoal will auto-generate waypoints around spawn position.")]
        [SerializeField] private List<Transform> waypoints = new();

        [Header("Debug")]
        [Tooltip("Enable behavior tree debug logging in Console")]
        [SerializeField] private bool debugBehaviorTree = false;

        private void Start()
        {
            var shipController = GetComponent<ShipController>();
            if (shipController == null || shipController.Ship == null)
            {
                Debug.LogWarning($"[V2AISetup] {gameObject.name}: No ShipController or Ship found");
                return;
            }

            var aiCore = shipController.Ship.Modules.GetAllModulesOfType<IAICoreModule>().FirstOrDefault() as V2AICoreModule;
            if (aiCore == null)
            {
                Debug.LogWarning($"[V2AISetup] {gameObject.name}: No V2AICoreModule found on ship");
                return;
            }

            // Enable debug logging if toggled
            if (debugBehaviorTree && aiCore.Context != null)
            {
                aiCore.Context.DebugLogging = true;
            }

            // Write waypoints to blackboard if provided
            if (waypoints.Count > 0)
            {
                var validTransforms = waypoints.Where(w => w != null).ToList();

                if (validTransforms.Count > 0)
                {
                    // Static positions (snapshot at init time)
                    var positions = validTransforms
                        .Select(w => (Vector2)w.position)
                        .ToList();

                    aiCore.Context?.Set("waypoint_list", positions);

                    // Transform references (live tracking)
                    aiCore.Context?.Set("waypoint_transforms", validTransforms);

                    Debug.Log($"[V2AISetup] {gameObject.name}: Wrote {validTransforms.Count} waypoints to blackboard");
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Starfire.Core;
using Starfire.Entity.Modules;
using Starfire.Entity.Modules.AICore;
using Starfire.Entity.Modules.Shield;
using Starfire.Entity.Modules.Weapon;
using UnityEngine;

namespace Starfire.Entity
{
    /// <summary>
    /// Bootstrap component for AI-controlled ships.
    /// Initializes ShipController and configures the AICore behavior tree.
    /// </summary>
    [RequireComponent(typeof(ShipController))]
    public class AISetup : MonoBehaviour
    {
        [Header("Ship Class")]
        [SerializeField] private ShipClassDefinition shipClass;

        [Header("Patrol Waypoints")]
        [SerializeField] private List<Transform> waypoints = new();

        [Header("Gizmo Settings")]
        [Tooltip("Effective arrival radius (arrivalThreshold * arrivalDistanceMultiplier from CalculateSteering node)")]
        [SerializeField] private float arrivalRadius = 2f;
        [SerializeField] private bool showArrivalBoundary = true;

        private void Start()
        {
            // Assign to Enemy layer for collision filtering
            gameObject.layer = LayerMask.NameToLayer(GameLayers.Enemy);

            var shipController = GetComponent<ShipController>();

            if (shipClass == null)
            {
                Debug.LogError("AISetup: No ShipClassDefinition assigned!");
                return;
            }

            // Initialize hardpoint registry before ship systems (so weapons can find their hardpoints)
            var hardpointRegistry = GetComponent<HardpointRegistry>();
            hardpointRegistry?.Initialize();

            shipController.Initialize(shipClass);

            var aiCore = shipController.ShipSystems.PrimaryAICore;
            if (aiCore == null)
            {
                Debug.LogWarning("AISetup: Ship has no AICore module equipped!");
                return;
            }

            aiCore.IsAutonomous = true;

            // Store waypoints in blackboard for behavior tree to use
            if (waypoints.Count > 0)
            {
                var validTransforms = waypoints
                    .Where(w => w != null)
                    .ToList();

                // Static positions (snapshot at init time) for SetTargetFromWaypointsAction
                var positions = validTransforms
                    .Select(w => (Vector2)w.position)
                    .ToList();

                aiCore.Context?.Set("waypoint_list", positions);

                // Transform references (live tracking) for SetTargetFromWaypointsDynamicAction
                aiCore.Context?.Set("waypoint_transforms", validTransforms);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw shield boundary
            DrawShieldBoundaryGizmo();

            // Draw waypoints
            if (waypoints == null || waypoints.Count == 0)
                return;

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;

                // Arrival boundary (where ship is considered arrived)
                if (showArrivalBoundary)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                    Gizmos.DrawWireSphere(waypoints[i].position, arrivalRadius);
                }

                // Waypoint center marker
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(waypoints[i].position, 0.5f);

                // Connection lines between waypoints
                int nextIndex = (i + 1) % waypoints.Count;
                if (waypoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
                }
            }
        }

        private void DrawShieldBoundaryGizmo()
        {
            if (shipClass == null) return;

            // Find shield module config in the ship class
            ShieldModuleConfig shieldConfig = null;
            foreach (var slot in shipClass.MultiSlots)
            {
                if (slot.defaultModule is ShieldModuleConfig config)
                {
                    shieldConfig = config;
                    break;
                }
            }

            if (shieldConfig == null || !shieldConfig.EnableBoundary) return;

            // Draw the ellipse boundary
            Vector2 size = shieldConfig.BoundarySize;
            Vector2 offset = shieldConfig.BoundaryOffset;

            Gizmos.color = new Color(0f, 0.8f, 1f, 0.6f);

            int segments = 32;
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 localPoint = new Vector3(
                    Mathf.Cos(angle) * size.x + offset.x,
                    Mathf.Sin(angle) * size.y + offset.y,
                    0f
                );
                Vector3 worldPoint = transform.TransformPoint(localPoint);

                if (i > 0)
                {
                    Gizmos.DrawLine(prevPoint, worldPoint);
                }
                prevPoint = worldPoint;
            }

            // Draw center cross
            Gizmos.color = Color.cyan;
            Vector3 center = transform.TransformPoint(new Vector3(offset.x, offset.y, 0f));
            float markerSize = 0.15f;
            Gizmos.DrawLine(center - Vector3.right * markerSize, center + Vector3.right * markerSize);
            Gizmos.DrawLine(center - Vector3.up * markerSize, center + Vector3.up * markerSize);
        }
    }
}

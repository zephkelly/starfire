#if UNITY_EDITOR
using UnityEngine;
using Starfire.Entity;
using Starfire.Entity.AI.BT;
using Starfire.Entity.AI.Heuristics;
using Starfire.Entity.Modules.AICore;
using Starfire.Entity.Modules.Sensor;

namespace Starfire.Core.Debugging
{
    /// <summary>
    /// Draws gizmo visualizations for AI entities in the Scene view.
    /// Attach to the same GameObject as AIDebugPanel or to a manager object.
    /// </summary>
    public class AIDebugGizmos : MonoBehaviour
    {
        [Header("Selection")]
        [Tooltip("Layers to visualize AI gizmos for")]
        [SerializeField] private LayerMask aiLayers = -1;
        [Tooltip("Only show gizmos for selected entity (click to select)")]
        [SerializeField] private bool selectedOnly = false;

        [Header("Visibility Toggles")]
        [SerializeField] private bool showSensorRanges = true;
        [SerializeField] private bool showSteeringVectors = true;
        [SerializeField] private bool showWaypoints = true;
        [SerializeField] private bool showInvestigationTarget = true;
        [SerializeField] private bool showVelocity = true;

        [Header("Gizmo Colors")]
        [SerializeField] private Color sensorRangeColor = new(0f, 1f, 1f, 0.15f);
        [SerializeField] private Color silhouetteRangeColor = new(0.3f, 0.5f, 1f, 0.15f);
        [SerializeField] private Color steeringTargetColor = Color.yellow;
        [SerializeField] private Color steeringForceColor = new(0.2f, 1f, 0.2f, 1f);
        [SerializeField] private Color velocityColor = new(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color waypointColor = new(1f, 0.6f, 0.2f, 1f);
        [SerializeField] private Color currentWaypointColor = new(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color investigationColor = new(1f, 0f, 1f, 0.8f);

        [Header("Gizmo Sizes")]
        [SerializeField] private float steeringForceScale = 5f;
        [SerializeField] private float velocityScale = 2f;
        [SerializeField] private float waypointSize = 0.5f;
        [SerializeField] private float currentWaypointSize = 0.8f;
        [SerializeField] private float targetCrossSize = 1f;

        [Header("Toggle")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F12;
        [SerializeField] private bool showGizmos = true;

        private EntityControllerBase _selectedEntity;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showGizmos = !showGizmos;
            }

            // Handle click selection for gizmos
            if (Input.GetMouseButtonDown(0) && selectedOnly)
            {
                TrySelectEntity();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _selectedEntity = null;
            }
        }

        private void TrySelectEntity()
        {
            if (Camera.main == null) return;

            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, aiLayers);

            if (hit.collider != null)
            {
                var entity = hit.collider.GetComponentInParent<EntityControllerBase>();
                if (entity != null)
                {
                    _selectedEntity = entity;
                    return;
                }
            }

            _selectedEntity = null;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || !Application.isPlaying) return;

            var ships = FindObjectsByType<ShipController>(FindObjectsSortMode.None);
            foreach (var ship in ships)
            {
                if (selectedOnly && ship != _selectedEntity) continue;
                if ((aiLayers.value & (1 << ship.gameObject.layer)) == 0) continue;

                DrawShipGizmos(ship);
            }
        }

        private void DrawShipGizmos(ShipController ship)
        {
            if (ship.ShipSystems == null) return;

            var aiCore = ship.ShipSystems.PrimaryAICore as BasicAICoreModule;
            if (aiCore == null) return;

            var btContext = aiCore.Context;
            if (btContext == null) return;

            Vector3 shipPos = ship.transform.position;

            // Get heuristics
            btContext.TryGet<HeuristicData>(HeuristicKeys.HeuristicData, out var heuristics);

            // Draw sensor ranges
            if (showSensorRanges)
            {
                DrawSensorRanges(shipPos, heuristics);
            }

            // Draw velocity vector
            if (showVelocity && ship.Rigidbody != null)
            {
                DrawVelocityVector(shipPos, ship.Rigidbody.linearVelocity);
            }

            // Draw steering
            if (showSteeringVectors)
            {
                DrawSteering(shipPos, btContext);
            }

            // Draw waypoints
            if (showWaypoints)
            {
                DrawWaypoints(btContext);
            }

            // Draw investigation target
            if (showInvestigationTarget)
            {
                DrawInvestigationTarget(shipPos, btContext);
            }
        }

        private void DrawSensorRanges(Vector3 shipPos, HeuristicData heuristics)
        {
            if (!heuristics.HasSensorModule) return;

            // Full sensor range
            if (heuristics.SensorRange > 0)
            {
                Gizmos.color = sensorRangeColor;
                DrawCircle(shipPos, heuristics.SensorRange, 48);
            }

            // Silhouette range
            if (heuristics.SilhouetteRange > 0)
            {
                Gizmos.color = silhouetteRangeColor;
                DrawCircle(shipPos, heuristics.SilhouetteRange, 36);
            }
        }

        private void DrawVelocityVector(Vector3 shipPos, Vector2 velocity)
        {
            if (velocity.sqrMagnitude < 0.01f) return;

            Gizmos.color = velocityColor;
            Vector3 velocityEnd = shipPos + (Vector3)(velocity * velocityScale);
            Gizmos.DrawLine(shipPos, velocityEnd);
            DrawArrowHead(shipPos, velocityEnd, 0.3f);
        }

        private void DrawSteering(Vector3 shipPos, BTContext btContext)
        {
            // Draw steering target
            if (btContext.TryGet<Vector2>("steering_target", out var target))
            {
                Gizmos.color = steeringTargetColor;
                DrawCross((Vector3)target, targetCrossSize);

                // Line from ship to target
                Gizmos.color = new Color(steeringTargetColor.r, steeringTargetColor.g, steeringTargetColor.b, 0.3f);
                Gizmos.DrawLine(shipPos, (Vector3)target);
            }

            // Draw steering force
            if (btContext.TryGet<Vector2>("steering_force", out var force) && force.sqrMagnitude > 0.01f)
            {
                Gizmos.color = steeringForceColor;
                Vector3 forceEnd = shipPos + (Vector3)(force * steeringForceScale);
                Gizmos.DrawLine(shipPos, forceEnd);
                DrawArrowHead(shipPos, forceEnd, 0.4f);
            }
        }

        private void DrawWaypoints(BTContext btContext)
        {
            if (!btContext.TryGet<WaypointStackState>("waypoint_stack", out var waypointStack))
                return;

            var sequence = waypointStack.Current;
            if (sequence == null || sequence.Count == 0) return;

            // Draw all waypoints and connections
            for (int i = 0; i < sequence.Count; i++)
            {
                Vector2? waypointPos = GetWaypointPosition(sequence, i);
                if (!waypointPos.HasValue) continue;

                bool isCurrent = (i == sequence.CurrentIndex);
                Vector3 pos = (Vector3)waypointPos.Value;

                // Draw waypoint marker
                if (isCurrent)
                {
                    Gizmos.color = currentWaypointColor;
                    DrawDiamond(pos, currentWaypointSize);
                }
                else
                {
                    Gizmos.color = waypointColor;
                    Gizmos.DrawSphere(pos, waypointSize);
                }

                // Draw connection to next waypoint
                int nextIndex = (i + 1) % sequence.Count;
                if (nextIndex != i || sequence.Mode == WaypointTraversalMode.Loop)
                {
                    Vector2? nextPos = GetWaypointPosition(sequence, nextIndex);
                    if (nextPos.HasValue && (sequence.Mode == WaypointTraversalMode.Loop || nextIndex > i))
                    {
                        Gizmos.color = new Color(waypointColor.r, waypointColor.g, waypointColor.b, 0.5f);
                        Gizmos.DrawLine(pos, (Vector3)nextPos.Value);
                    }
                }
            }

            // Draw stack depth indicator if nested
            if (waypointStack.Depth > 1)
            {
                var currentTarget = sequence.CurrentTarget;
                if (currentTarget.HasValue)
                {
                    Gizmos.color = Color.cyan;
                    DrawCircle((Vector3)currentTarget.Value, 0.3f * waypointStack.Depth, 8);
                }
            }
        }

        private Vector2? GetWaypointPosition(WaypointSequence sequence, int index)
        {
            if (sequence.IsDynamic)
            {
                if (index >= 0 && index < sequence.Transforms.Count)
                {
                    var t = sequence.Transforms[index];
                    return t != null ? (Vector2)t.position : null;
                }
            }
            else
            {
                if (index >= 0 && index < sequence.Waypoints.Count)
                {
                    return sequence.Waypoints[index];
                }
            }
            return null;
        }

        private void DrawInvestigationTarget(Vector3 shipPos, BTContext btContext)
        {
            if (!btContext.TryGet<DetectedEntity>("investigation_target", out var target))
                return;

            if (!target.IsValid) return;

            Vector3 targetPos = target.Controller.transform.position;

            // Draw magenta line to investigation target
            Gizmos.color = investigationColor;
            DrawDashedLine(shipPos, targetPos, 0.5f);

            // Draw marker around target
            Gizmos.color = new Color(investigationColor.r, investigationColor.g, investigationColor.b, 0.5f);
            DrawCircle(targetPos, 1f, 12);
        }

        // Gizmo helper methods
        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }

        private void DrawCross(Vector3 center, float size)
        {
            float half = size / 2f;
            Gizmos.DrawLine(center + new Vector3(-half, 0, 0), center + new Vector3(half, 0, 0));
            Gizmos.DrawLine(center + new Vector3(0, -half, 0), center + new Vector3(0, half, 0));
        }

        private void DrawDiamond(Vector3 center, float size)
        {
            float half = size / 2f;
            Vector3 top = center + new Vector3(0, half, 0);
            Vector3 right = center + new Vector3(half, 0, 0);
            Vector3 bottom = center + new Vector3(0, -half, 0);
            Vector3 left = center + new Vector3(-half, 0, 0);

            Gizmos.DrawLine(top, right);
            Gizmos.DrawLine(right, bottom);
            Gizmos.DrawLine(bottom, left);
            Gizmos.DrawLine(left, top);
        }

        private void DrawArrowHead(Vector3 from, Vector3 to, float size)
        {
            Vector3 direction = (to - from).normalized;
            Vector3 right = Quaternion.Euler(0, 0, 150) * direction * size;
            Vector3 left = Quaternion.Euler(0, 0, -150) * direction * size;

            Gizmos.DrawLine(to, to + right);
            Gizmos.DrawLine(to, to + left);
        }

        private void DrawDashedLine(Vector3 from, Vector3 to, float dashLength)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            direction /= distance;

            float traveled = 0f;
            bool draw = true;

            while (traveled < distance)
            {
                float segmentLength = Mathf.Min(dashLength, distance - traveled);
                Vector3 segmentStart = from + direction * traveled;
                Vector3 segmentEnd = segmentStart + direction * segmentLength;

                if (draw)
                {
                    Gizmos.DrawLine(segmentStart, segmentEnd);
                }

                traveled += dashLength;
                draw = !draw;
            }
        }
    }
}
#endif

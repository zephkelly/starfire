using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Starfire.Entity;
using Starfire.Sim;

namespace Starfire.Demo
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] float smoothSpeed = 10f;
        [SerializeField] float cameraZ = -50f;

        [Header("Tier Gizmos")]
        [SerializeField] bool _showTierGizmos = true;
        [SerializeField] float _gizmoT0 = 5000f;
        [SerializeField] float _gizmoT1 = 20000f;
        [SerializeField] float _gizmoT2 = 100000f;
        [SerializeField] float _gizmoT3 = 200000f;

        [SerializeField] Color _gizmoT0Color = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] Color _gizmoT1Color = new Color(1f, 1f, 0f, 0.4f);
        [SerializeField] Color _gizmoT2Color = new Color(1f, 0.5f, 0f, 0.3f);
        [SerializeField] Color _gizmoT3Color = new Color(1f, 0f, 0f, 0.2f);

        EntityQuery _playerQuery;
        EntityQuery _configQuery;

        void LateUpdate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            var em = world.EntityManager;

            if (_playerQuery == default)
                _playerQuery = em.CreateEntityQuery(typeof(PlayerTag), typeof(LocalTransform));

            if (_playerQuery.IsEmpty)
                return;

            var entity = _playerQuery.GetSingletonEntity();
            var transform = em.GetComponentData<LocalTransform>(entity);

            var targetPos = new Vector3(transform.Position.x, transform.Position.y, cameraZ);
            this.transform.position = Vector3.Lerp(this.transform.position, targetPos, smoothSpeed * Time.deltaTime);

            SyncGizmoDistancesFromConfig();
        }

        void SyncGizmoDistancesFromConfig()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            if (_configQuery == default)
                _configQuery = world.EntityManager.CreateEntityQuery(typeof(SimulationConfig));

            if (_configQuery.IsEmpty) return;

            var config = _configQuery.GetSingleton<SimulationConfig>();
            _gizmoT0 = config.Bounds.Tier0MaxDistance;
            _gizmoT1 = config.Bounds.Tier1MaxDistance;
            _gizmoT2 = config.Bounds.Tier2MaxDistance;
            _gizmoT3 = config.Bounds.Tier3MaxDistance;
        }

        void OnDrawGizmos()
        {
            if (!_showTierGizmos) return;

            var center = Application.isPlaying
                ? new Vector3(transform.position.x, transform.position.y, 0f)
                : Vector3.zero;

            DrawTierCircle(center, _gizmoT0, _gizmoT0Color, "T0");
            DrawTierCircle(center, _gizmoT1, _gizmoT1Color, "T1");
            DrawTierCircle(center, _gizmoT2, _gizmoT2Color, "T2");
            DrawTierCircle(center, _gizmoT3, _gizmoT3Color, "T3");
        }

        void DrawTierCircle(Vector3 center, float radius, Color color, string label)
        {
            Gizmos.color = color;

            int segments = 64;
            float angleStep = 360f / segments * Mathf.Deg2Rad;

            for (int i = 0; i < segments; i++)
            {
                float a0 = i * angleStep;
                float a1 = (i + 1) * angleStep;

                var p0 = center + new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0f);
                var p1 = center + new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0f);

                Gizmos.DrawLine(p0, p1);
            }

#if UNITY_EDITOR
            var labelPos = center + new Vector3(radius + 100f, 100f, 0f);
            UnityEditor.Handles.Label(labelPos, $"{label}: {radius:F0}", new GUIStyle
            {
                normal = { textColor = color },
                fontSize = 12
            });
#endif
        }
    }
}

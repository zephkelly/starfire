using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Starfire.Core;
using Starfire.Sim;

namespace Starfire.Demo
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] float cameraZ = -50f;

        [Header("Look Ahead")]
        [SerializeField] float _maxLookAhead = 150f;
        [SerializeField] float _minLookAhead = 0f;
        [SerializeField] float _innerLimit = 0.1f;
        [SerializeField] float _outerLimit = 0.8f;
        [SerializeField] float _lookAheadSmoothing = 0.15f;

        [Header("Zoom")]
        [SerializeField] float _minZoom = 10f;
        [SerializeField] float _maxZoom = 500f;
        [SerializeField] float _scrollSensitivity = 0.001f;
        [SerializeField] float _gamepadZoomSpeed = 1.5f;
        [SerializeField] float _zoomSmoothing = 0.15f;

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
        EntityQuery _networkIdQuery;
        EntityQuery _configQuery;
        World _cachedWorld;
        float _lastLogTime;
        bool _foundPlayerOnce;

        Camera _camera;
        Vector2 _currentAimOffset;
        Vector2 _aimOffsetVelocity;
        float _targetZoom;
        float _zoomVelocity;
        bool _zoomInitialized;

        void LateUpdate()
        {
            bool shouldLog = !_foundPlayerOnce && Time.time - _lastLogTime > 2f;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                if (shouldLog) { Debug.Log($"[CameraFollow] No world. DefaultGameObjectInjectionWorld={world?.Name ?? "null"}, IsCreated={world?.IsCreated}"); _lastLogTime = Time.time; }
                return;
            }

            if (_cachedWorld != world)
            {
                _playerQuery = default;
                _networkIdQuery = default;
                _configQuery = default;
                _foundPlayerOnce = false;
                _currentAimOffset = Vector2.zero;
                _aimOffsetVelocity = Vector2.zero;
                _cachedWorld = world;
            }

            var em = world.EntityManager;

            if (_networkIdQuery == default)
                _networkIdQuery = em.CreateEntityQuery(typeof(NetworkId));

            if (_networkIdQuery.IsEmpty)
            {
                if (shouldLog) { Debug.Log($"[CameraFollow] World='{world.Name}' — no NetworkId entity (client not connected yet?)"); _lastLogTime = Time.time; }
                return;
            }

            int localNetId = _networkIdQuery.GetSingleton<NetworkId>().Value;

            if (_playerQuery == default)
                _playerQuery = em.CreateEntityQuery(typeof(GhostOwner), typeof(LocalTransform));

            int ghostOwnerCount = _playerQuery.CalculateEntityCount();
            if (_playerQuery.IsEmpty)
            {
                if (shouldLog) { Debug.Log($"[CameraFollow] World='{world.Name}' localNetId={localNetId} — no entities with GhostOwner+LocalTransform"); _lastLogTime = Time.time; }
                return;
            }

            var entities = _playerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            Unity.Entities.Entity playerEntity = Unity.Entities.Entity.Null;

            for (int i = 0; i < entities.Length; i++)
            {
                int ownerNetId = em.GetComponentData<GhostOwner>(entities[i]).NetworkId;
                if (shouldLog && i < 5)
                    Debug.Log($"[CameraFollow] GhostOwner entity[{i}]={entities[i]} NetworkId={ownerNetId} (looking for {localNetId})");
                if (ownerNetId == localNetId)
                {
                    playerEntity = entities[i];
                    break;
                }
            }

            entities.Dispose();

            if (playerEntity == Unity.Entities.Entity.Null)
            {
                if (shouldLog) { Debug.Log($"[CameraFollow] Found {ghostOwnerCount} GhostOwner entities but none match localNetId={localNetId}"); _lastLogTime = Time.time; }
                return;
            }

            if (!_foundPlayerOnce)
            {
                _foundPlayerOnce = true;
                Debug.Log($"[CameraFollow] Found player entity={playerEntity} in world='{world.Name}'");
            }

            if (_camera == null)
                _camera = GetComponent<Camera>();

            var lt = em.GetComponentData<LocalTransform>(playerEntity);
            if (float.IsNaN(lt.Position.x) || float.IsNaN(lt.Position.y))
                return;

            Vector2 playerPos = new Vector2(lt.Position.x, lt.Position.y);
            Vector2 aimOffset = ComputeAimOffset();

            if (_lookAheadSmoothing > 0f)
            {
                _currentAimOffset = Vector2.SmoothDamp(
                    _currentAimOffset, aimOffset, ref _aimOffsetVelocity, _lookAheadSmoothing);
            }
            else
            {
                _currentAimOffset = aimOffset;
            }

            Vector2 finalPos = playerPos + _currentAimOffset;
            transform.position = new Vector3(finalPos.x, finalPos.y, cameraZ);
            transform.rotation = Quaternion.identity;

            if (PlayerController.Instance != null)
                PlayerController.Instance.SetCamera(_camera);

            UpdateZoom();

            if (Time.time - _lastLogTime > 5f)
            {
                Debug.Log($"[CameraFollow] Tracking player at ({lt.Position.x:F1}, {lt.Position.y:F1}) AimOffset=({_currentAimOffset.x:F1}, {_currentAimOffset.y:F1}) Zoom={_camera.orthographicSize:F1}");
                _lastLogTime = Time.time;
            }

            SyncGizmoDistancesFromConfig();
        }

        void UpdateZoom()
        {
            if (_camera == null) return;

            if (!_zoomInitialized)
            {
                _targetZoom = Mathf.Clamp(_camera.orthographicSize, _minZoom, _maxZoom);
                _zoomInitialized = true;
            }

            var pc = PlayerController.Instance;
            if (pc == null) return;

            float scrollInput = pc.ScrollDelta * _scrollSensitivity;
            float gamepadInput = pc.GamepadZoomInput * _gamepadZoomSpeed * Time.deltaTime;
            float totalInput = scrollInput + gamepadInput;

            if (Mathf.Abs(totalInput) > 0.0001f)
            {
                float zoomDelta = -totalInput * _targetZoom;
                float newTargetZoom = Mathf.Clamp(_targetZoom + zoomDelta, _minZoom, _maxZoom);

                float currentDirection = _targetZoom - _camera.orthographicSize;
                float newDirection = newTargetZoom - _camera.orthographicSize;
                if (currentDirection * newDirection < 0f)
                    _zoomVelocity = 0f;

                _targetZoom = newTargetZoom;
            }

            if (_zoomSmoothing > 0f)
            {
                float diff = Mathf.Abs(_camera.orthographicSize - _targetZoom);
                if (diff > 0.01f)
                {
                    _camera.orthographicSize = Mathf.SmoothDamp(
                        _camera.orthographicSize, _targetZoom, ref _zoomVelocity, _zoomSmoothing);
                }
                else
                {
                    _camera.orthographicSize = _targetZoom;
                    _zoomVelocity = 0f;
                }
            }
            else
            {
                _camera.orthographicSize = _targetZoom;
            }
        }

        Vector2 ComputeAimOffset()
        {
            if (_camera == null || _maxLookAhead <= 0f)
                return Vector2.zero;

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var pc = PlayerController.Instance;
            if (pc == null) return Vector2.zero;
            Vector2 mouseOffset = pc.MouseScreenPosition - screenCenter;
            float halfScreen = Mathf.Min(Screen.width, Screen.height) * 0.5f;

            if (halfScreen < 1f)
                return Vector2.zero;

            float normalizedDist = mouseOffset.magnitude / halfScreen;
            float t = Mathf.InverseLerp(_innerLimit, _outerLimit, normalizedDist);
            t = Mathf.Clamp01(t);

            float lookAhead = Mathf.Lerp(_minLookAhead, _maxLookAhead, t);

            if (lookAhead > 0f && mouseOffset.sqrMagnitude > 0.001f)
                return mouseOffset.normalized * lookAhead;

            return Vector2.zero;
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

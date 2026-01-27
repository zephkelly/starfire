using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Background.Layers;
using Starfire.Core.V2.World;

namespace Starfire.Core.Background
{
    /// <summary>
    /// Manages multiple starfield layers, each rendered on its own quad.
    /// Attach this to your main camera or an empty GameObject in the scene.
    /// </summary>
    [ExecuteAlways]
    public class StarfieldManager : MonoBehaviour
    {
        [Header("Camera")]
        [Tooltip("Camera to use for sizing. If not set, uses Camera.main")]
        [SerializeField] private Camera targetCamera;

        [Header("Settings")]
        [Tooltip("Base Z position for the background quads (should be far behind other objects)")]
        [SerializeField] private float backgroundDepth = 100f;

        [Tooltip("Extra scale multiplier for the quad size")]
        [SerializeField] private float scaleMultiplier = 1.1f;

        [Header("Zoom Response")]
        [Tooltip("Reference orthographic size for zoom scaling (default camera zoom)")]
        [SerializeField] private float referenceZoom = 10f;

        [Header("Editor")]
        [Tooltip("Enable to see the starfield in Scene view without entering Play mode")]
        [SerializeField] private bool enableEditorPreview = false;

        [Header("Layers")]
        [SerializeReference]
        private List<StarfieldLayer> layers = new List<StarfieldLayer>();

        private Camera _camera;
        private Mesh _sharedQuadMesh;
        private bool _initialized = false;

        // Floating origin tracking
        [System.NonSerialized] private Vector2 _originShiftAccumulator = Vector2.zero;
        [System.NonSerialized] private bool _subscribedToOriginShift = false;
        private const float WRAP_PERIOD = 100000f; // Large enough to avoid visible tiling

        private static readonly int CameraWorldPosID = Shader.PropertyToID("_CameraWorldPos");
        private static readonly int ScreenAspectID = Shader.PropertyToID("_ScreenAspect");
        private static readonly int CameraOrthoSizeID = Shader.PropertyToID("_CameraOrthoSize");
        private static readonly int ReferenceZoomID = Shader.PropertyToID("_ReferenceZoom");

        private void Awake()
        {
            CleanupStaleQuads();
        }

        private void OnEnable()
        {
            UpdateCameraReference();
            TrySubscribeToOriginShift();

            if (ShouldRender())
            {
                InitializeLayers();
            }
        }

        private void UpdateCameraReference()
        {
            if (targetCamera != null)
            {
                _camera = targetCamera;
            }
            else
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    _camera = FindFirstObjectByType<Camera>();
                }
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromOriginShift();
            CleanupLayers();
        }

        private bool ShouldRender()
        {
            return Application.isPlaying || enableEditorPreview;
        }

        private void TrySubscribeToOriginShift()
        {
            if (_subscribedToOriginShift) return;
            if (WorldGenerationService.Instance == null) return;

            WorldGenerationService.Instance.OnOriginShift += HandleOriginShift;
            _subscribedToOriginShift = true;
        }

        private void UnsubscribeFromOriginShift()
        {
            if (!_subscribedToOriginShift) return;
            if (WorldGenerationService.Instance == null) return;

            WorldGenerationService.Instance.OnOriginShift -= HandleOriginShift;
            _subscribedToOriginShift = false;
        }

        private void HandleOriginShift(Vector2 shiftAmount)
        {
            // Add inverse of shift to maintain visual continuity
            _originShiftAccumulator -= shiftAmount;

            // Wrap symmetrically to prevent precision loss at extreme values
            // Uses [-WRAP_PERIOD/2, +WRAP_PERIOD/2) range to avoid zero-crossing discontinuity
            _originShiftAccumulator.x = WrapCoordinateSymmetric(_originShiftAccumulator.x, WRAP_PERIOD);
            _originShiftAccumulator.y = WrapCoordinateSymmetric(_originShiftAccumulator.y, WRAP_PERIOD);

            // Notify layers with runtime state
            foreach (var layer in layers)
            {
                if (layer is ShootingStarLayer shootingLayer)
                    shootingLayer.OnOriginShift(shiftAmount);
                else if (layer is CometLayer cometLayer)
                    cometLayer.OnOriginShift(shiftAmount);
            }
        }

        /// <summary>
        /// Symmetric wrapping: keeps value in [-halfPeriod, +halfPeriod) range.
        /// This prevents discontinuity when crossing zero, unlike asymmetric [0, period) wrapping.
        /// </summary>
        private static float WrapCoordinateSymmetric(float value, float period)
        {
            float halfPeriod = period * 0.5f;
            value = value % period;
            if (value < -halfPeriod) value += period;
            else if (value >= halfPeriod) value -= period;
            return value;
        }

        private void InitializeLayers()
        {
            if (_initialized) return;
            if (_camera == null) return;

            _sharedQuadMesh = CreateQuadMesh();

            // Sort layers by parallax depth (farthest first = lowest depth values first)
            var sortedLayers = new List<StarfieldLayer>(layers);
            sortedLayers.Sort((a, b) => a.parallaxDepth.CompareTo(b.parallaxDepth));

            for (int i = 0; i < sortedLayers.Count; i++)
            {
                var layer = sortedLayers[i];
                if (layer != null)
                {
                    layer.Initialize(transform, _sharedQuadMesh, i);
                }
            }

            _initialized = true;
            UpdateAllLayerTransforms();
        }

        private void CleanupLayers()
        {
            foreach (var layer in layers)
            {
                layer?.Cleanup();
            }
            _initialized = false;

            if (_sharedQuadMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_sharedQuadMesh);
                }
                else
                {
                    DestroyImmediate(_sharedQuadMesh);
                }
                _sharedQuadMesh = null;
            }
        }

        private void CleanupStaleQuads()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("StarfieldLayer_"))
                {
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (!ShouldRender()) return;

            // Retry subscription if not yet subscribed (handles script execution order)
            if (!_subscribedToOriginShift)
                TrySubscribeToOriginShift();

            // Refresh camera reference each frame to stay in sync
            UpdateCameraReference();

            if (!_initialized)
            {
                InitializeLayers();
            }

            if (_camera != null && _initialized)
            {
                UpdateShaderGlobals();
                UpdateAllLayers();
                UpdateAllLayerTransforms();
            }
        }

        private void UpdateShaderGlobals()
        {
            if (_camera == null) return;

            Vector3 camPos = _camera.transform.position;

            // Calculate virtual position (actual + accumulated offset) for floating origin continuity
            // Do NOT wrap virtualPos every frame - this caused zero-crossing discontinuity!
            // The position stays bounded because:
            // - camPos is always near origin (floating origin resets at ~2560 units)
            // - accumulator is wrapped symmetrically during origin shifts
            // - Combined result stays well within float precision limits
            Vector2 virtualPos = new Vector2(camPos.x, camPos.y) + _originShiftAccumulator;

            Shader.SetGlobalVector(CameraWorldPosID, new Vector4(virtualPos.x, virtualPos.y, 0, 0));
            Shader.SetGlobalFloat(ScreenAspectID, _camera.aspect);
            Shader.SetGlobalFloat(CameraOrthoSizeID, _camera.orthographicSize);
            Shader.SetGlobalFloat(ReferenceZoomID, referenceZoom);
        }

        private void UpdateAllLayers()
        {
            foreach (var layer in layers)
            {
                if (layer != null && layer.IsInitialized)
                {
                    layer.Update();
                }
            }
        }

        private void UpdateAllLayerTransforms()
        {
            if (_camera == null) return;

            Vector3 cameraPos = _camera.transform.position;

            float height, width;
            if (_camera.orthographic)
            {
                height = _camera.orthographicSize * 2f;
                width = height * _camera.aspect;
            }
            else
            {
                height = 2f * backgroundDepth * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                width = height * _camera.aspect;
            }

            width *= scaleMultiplier;
            height *= scaleMultiplier;

            // Sort layers by parallax depth for proper Z ordering
            var sortedLayers = new List<StarfieldLayer>(layers);
            sortedLayers.Sort((a, b) => a.parallaxDepth.CompareTo(b.parallaxDepth));

            for (int i = 0; i < sortedLayers.Count; i++)
            {
                var layer = sortedLayers[i];
                if (layer != null && layer.IsInitialized)
                {
                    layer.UpdateTransform(cameraPos, width, height, backgroundDepth, i);
                }
            }
        }

        private Mesh CreateQuadMesh()
        {
            var mesh = new Mesh();
            mesh.name = "StarfieldQuadMesh";
            mesh.hideFlags = HideFlags.DontSave;

            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0)
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Defer initialization/cleanup to avoid errors during OnValidate
            UnityEditor.EditorApplication.delayCall += OnValidateDeferred;
#endif
        }

#if UNITY_EDITOR
        private void OnValidateDeferred()
        {
            // Guard against destroyed objects
            if (this == null) return;

            // Handle preview toggle changes in editor
            if (!Application.isPlaying)
            {
                if (enableEditorPreview && !_initialized)
                {
                    if (_camera == null)
                    {
                        _camera = Camera.main;
                        if (_camera == null)
                        {
                            _camera = FindFirstObjectByType<Camera>();
                        }
                    }
                    InitializeLayers();
                }
                else if (!enableEditorPreview && _initialized)
                {
                    CleanupLayers();
                }
            }

            // Update existing layers
            if (_initialized && _camera != null)
            {
                UpdateAllLayers();
                UpdateAllLayerTransforms();
            }
        }
#endif

        /// <summary>
        /// Force re-initialization of all layers.
        /// </summary>
        public void RefreshLayers()
        {
            CleanupLayers();
            CleanupStaleQuads();
            if (ShouldRender())
            {
                InitializeLayers();
            }
        }

        /// <summary>
        /// Add a new layer at runtime.
        /// </summary>
        public void AddLayer(StarfieldLayer layer)
        {
            layers.Add(layer);
            if (_initialized)
            {
                layer.Initialize(transform, _sharedQuadMesh, layers.Count - 1);
            }
        }

        /// <summary>
        /// Remove a layer at runtime.
        /// </summary>
        public void RemoveLayer(StarfieldLayer layer)
        {
            layer.Cleanup();
            layers.Remove(layer);
        }

        /// <summary>
        /// Get all layers.
        /// </summary>
        public IReadOnlyList<StarfieldLayer> Layers => layers;

        /// <summary>
        /// Draw gizmos for all layers that support it (e.g., ShootingStarLayer debug visualization).
        /// </summary>
        private void OnDrawGizmos()
        {
            if (layers == null) return;

            foreach (var layer in layers)
            {
                if (layer is ShootingStarLayer shootingStarLayer)
                {
                    shootingStarLayer.DrawGizmos();
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Add Star Layer")]
        private void AddStarLayer()
        {
            var layer = new StarLayer
            {
                layerName = $"Stars {layers.Count + 1}",
                renderBackground = layers.Count == 0 // First layer renders background
            };
            layers.Add(layer);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Add Shaped Star Layer")]
        private void AddShapedStarLayer()
        {
            var layer = new ShapedStarLayer
            {
                layerName = $"Shaped Stars {layers.Count + 1}",
                renderBackground = layers.Count == 0 // First layer renders background
            };
            layers.Add(layer);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Add Nebula Layer")]
        private void AddNebulaLayer()
        {
            var layer = new NebulaLayer
            {
                layerName = $"Nebula {layers.Count + 1}",
                renderBackground = false // Nebula typically layers on top
            };
            layers.Add(layer);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Refresh All Layers")]
        private void EditorRefreshLayers()
        {
            RefreshLayers();
        }

        [ContextMenu("Clear All Layers")]
        private void ClearAllLayers()
        {
            CleanupLayers();
            CleanupStaleQuads();
            layers.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

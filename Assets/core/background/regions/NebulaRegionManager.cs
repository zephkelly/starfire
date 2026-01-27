using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Background.Layers;
using Starfire.Core.Background.Presets;
using Starfire.Core.V2.World;

namespace Starfire.Core.Background.Regions
{
    /// <summary>
    /// Central manager for nebula regions. Handles creation, destruction, and rendering
    /// of spatial nebula regions.
    /// </summary>
    [ExecuteAlways]
    public class NebulaRegionManager : MonoBehaviour
    {
        public static NebulaRegionManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Maximum number of regions that can be rendered simultaneously")]
        [SerializeField] private int maxVisibleRegions = 8;

        [Tooltip("Extra padding around camera frustum for culling (world units)")]
        [SerializeField] private float frustumPadding = 50f;

        [Tooltip("Base Z position for region quads")]
        [SerializeField] private float backgroundDepth = 100f;

        [Tooltip("Scale multiplier for quad size")]
        [SerializeField] private float scaleMultiplier = 1.1f;

        [Header("Shaders")]
        [Tooltip("Shader for basic nebula regions")]
        [SerializeField] private Shader nebulaShader;

        [Tooltip("Shader for stylized nebula regions")]
        [SerializeField] private Shader stylizedNebulaShader;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool logRegionEvents = true;

        /// <summary>
        /// Whether debug gizmos are enabled (exposed for editor scripts).
        /// </summary>
        public bool ShowDebugGizmos => showDebugGizmos;

        // Runtime state
        private readonly List<NebulaRegion> _allRegions = new List<NebulaRegion>();
        private readonly List<NebulaRegion> _visibleRegions = new List<NebulaRegion>();
        private readonly Queue<Material> _basicMaterialPool = new Queue<Material>();
        private readonly Queue<Material> _stylizedMaterialPool = new Queue<Material>();

        private Camera _camera;
        private Mesh _sharedQuadMesh;
        private bool _initialized = false;

        // Floating origin tracking (matches StarfieldManager pattern)
        private Vector2 _originShiftAccumulator = Vector2.zero;
        private bool _subscribedToOriginShift = false;
        private const float WRAP_PERIOD = 100000f;

        // Events
        public event Action<NebulaRegion> OnRegionCreated;
        public event Action<NebulaRegion> OnRegionDestroyed;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple NebulaRegionManagers detected. Destroying duplicate.");
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            LoadDefaultShaders();
        }

        private void OnEnable()
        {
            Instance = this;
            _camera = Camera.main;
            if (_camera == null)
            {
                _camera = FindFirstObjectByType<Camera>();
            }

            Initialize();
            TrySubscribeToOriginShift();
        }

        private void OnDisable()
        {
            UnsubscribeFromOriginShift();
            Cleanup();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || !_initialized) return;

            // Retry origin shift subscription if needed (handles script execution order)
            if (!_subscribedToOriginShift)
            {
                TrySubscribeToOriginShift();
            }

            UpdateVisibleRegions();
            UpdateRegionTransforms();
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            foreach (var region in _allRegions)
            {
                if (!region.IsActive) continue;

                Vector3 center = new Vector3(region.WorldPosition.x, region.WorldPosition.y, 0);

                // Draw center marker
                DrawCenterMarker(center);

                // Draw boundaries based on edge behavior
                switch (region.Config.edgeBehavior)
                {
                    case NebulaEdgeBehavior.SmoothFalloff:
                        DrawSmoothFalloffGizmo(center, region.Radius, region.Config);
                        break;
                    case NebulaEdgeBehavior.SharpBoundary:
                        DrawSharpBoundaryGizmo(center, region.Radius);
                        break;
                    case NebulaEdgeBehavior.InverseFalloff:
                        DrawInverseFalloffGizmo(center, region.Radius, region.Config);
                        break;
                }
            }
        }

        private void DrawCenterMarker(Vector3 center)
        {
            float size = 5f;
            Gizmos.color = Color.white;
            Gizmos.DrawLine(center - Vector3.right * size, center + Vector3.right * size);
            Gizmos.DrawLine(center - Vector3.up * size, center + Vector3.up * size);
        }

        private void DrawSmoothFalloffGizmo(Vector3 center, float radius, NebulaRegionConfig config)
        {
            float innerRadius = Mathf.Max(0f, radius - config.falloffDistance);
            Color baseColor = new Color(0.2f, 0.6f, 1f, 1f);

            // Outer edge (where fade ends, density = 0)
            Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.8f);
            DrawCircleGizmo(center, radius, 64);

            // Inner edge (where fade starts, density = 1)
            Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
            DrawCircleGizmo(center, innerRadius, 64);

            // Gradient rings showing expected density
            DrawGradientRings(center, innerRadius, radius, baseColor, 4);
        }

        private void DrawInverseFalloffGizmo(Vector3 center, float radius, NebulaRegionConfig config)
        {
            float outerRadius = radius + config.falloffDistance;
            Color baseColor = new Color(0.8f, 0.2f, 0.8f, 1f);

            // Inner edge (clear zone boundary)
            Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.8f);
            DrawCircleGizmo(center, radius, 64);

            // Outer edge (full nebula density)
            Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
            DrawCircleGizmo(center, outerRadius, 64);

            // Gradient rings showing expected density
            DrawGradientRings(center, radius, outerRadius, baseColor, 4);
        }

        private void DrawSharpBoundaryGizmo(Vector3 center, float radius)
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
            DrawCircleGizmo(center, radius, 64);
        }

        private void DrawGradientRings(Vector3 center, float inner, float outer, Color baseColor, int count)
        {
            for (int i = 1; i < count; i++)
            {
                float t = i / (float)count;
                float ringRadius = Mathf.Lerp(inner, outer, t);
                float alpha = 0.1f + (1f - t) * 0.15f;
                Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                DrawCircleGizmo(center, ringRadius, 32);
            }
        }

        #endregion

        #region Initialization

        private void LoadDefaultShaders()
        {
            if (nebulaShader == null)
                nebulaShader = Shader.Find("Starfire/Nebula");

            if (stylizedNebulaShader == null)
                stylizedNebulaShader = Shader.Find("Starfire/StylizedNebula");
        }

        private void Initialize()
        {
            if (_initialized) return;

            _sharedQuadMesh = CreateQuadMesh();
            _initialized = true;

            // Re-initialize existing regions
            foreach (var region in _allRegions)
            {
                InitializeRegionRendering(region);
            }
        }

        private void Cleanup()
        {
            // Cleanup all regions
            foreach (var region in _allRegions)
            {
                CleanupRegionRendering(region);
            }

            // Clear material pools
            while (_basicMaterialPool.Count > 0)
            {
                var mat = _basicMaterialPool.Dequeue();
                if (mat != null) DestroyMaterial(mat);
            }

            while (_stylizedMaterialPool.Count > 0)
            {
                var mat = _stylizedMaterialPool.Dequeue();
                if (mat != null) DestroyMaterial(mat);
            }

            // Cleanup mesh
            if (_sharedQuadMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_sharedQuadMesh);
                else
                    DestroyImmediate(_sharedQuadMesh);
                _sharedQuadMesh = null;
            }

            _initialized = false;
        }

        private Mesh CreateQuadMesh()
        {
            var mesh = new Mesh
            {
                name = "NebulaRegionQuadMesh",
                hideFlags = HideFlags.DontSave
            };

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

        #endregion

        #region Public API

        /// <summary>
        /// Creates a new nebula region at the specified position.
        /// </summary>
        public NebulaRegion CreateRegion(Vector2 position, float radius, NebulaRegionConfig config)
        {
            if (config == null)
            {
                Debug.LogError("NebulaRegionConfig is required to create a region.");
                return null;
            }

            var region = new NebulaRegion(position, radius, config);
            _allRegions.Add(region);

            if (_initialized)
            {
                InitializeRegionRendering(region);
            }

            if (logRegionEvents)
            {
                Debug.Log($"[Nebula] Created region {region.Id} at {position}, radius={radius}, falloff={config.falloffDistance}, edgeBehavior={config.edgeBehavior}");
            }

            OnRegionCreated?.Invoke(region);
            return region;
        }

        /// <summary>
        /// Destroys a nebula region.
        /// </summary>
        public void DestroyRegion(NebulaRegion region)
        {
            if (region == null) return;

            if (logRegionEvents)
            {
                Debug.Log($"[Nebula] Destroying region {region.Id} at {region.WorldPosition}");
            }

            CleanupRegionRendering(region);
            _allRegions.Remove(region);
            _visibleRegions.Remove(region);

            OnRegionDestroyed?.Invoke(region);
        }

        /// <summary>
        /// Destroys a region by its ID.
        /// </summary>
        public void DestroyRegion(string regionId)
        {
            var region = _allRegions.Find(r => r.Id == regionId);
            if (region != null)
            {
                DestroyRegion(region);
            }
        }

        /// <summary>
        /// Gets all registered regions.
        /// </summary>
        public IReadOnlyList<NebulaRegion> GetAllRegions() => _allRegions;

        /// <summary>
        /// Gets all currently visible regions.
        /// </summary>
        public IReadOnlyList<NebulaRegion> GetVisibleRegions() => _visibleRegions;

        /// <summary>
        /// Gets the region at a specific point, or null if no region contains the point.
        /// </summary>
        public NebulaRegion GetRegionAtPoint(Vector2 point)
        {
            foreach (var region in _allRegions)
            {
                if (region.IsActive && region.ContainsPoint(point))
                {
                    return region;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all regions that contain a specific point.
        /// </summary>
        public List<NebulaRegion> GetRegionsAtPoint(Vector2 point)
        {
            var result = new List<NebulaRegion>();
            foreach (var region in _allRegions)
            {
                if (region.IsActive && region.ContainsPoint(point))
                {
                    result.Add(region);
                }
            }
            return result;
        }

        #endregion

        #region Internal - Zone Registration

        internal void RegisterZone(NebulaRegionZone zone)
        {
            if (zone == null || zone.RuntimeRegion != null) return;

            var region = CreateRegion(
                (Vector2)zone.transform.position,
                zone.Radius,
                zone.Config
            );

            zone.SetRuntimeRegion(region);
        }

        internal void UnregisterZone(NebulaRegionZone zone)
        {
            if (zone == null || zone.RuntimeRegion == null) return;

            DestroyRegion(zone.RuntimeRegion);
            zone.SetRuntimeRegion(null);
        }

        #endregion

        #region Rendering

        private void InitializeRegionRendering(NebulaRegion region)
        {
            if (region.QuadObject != null) return;

            // Create quad GameObject
            region.QuadObject = new GameObject($"NebulaRegion_{region.Id}")
            {
                hideFlags = HideFlags.DontSave
            };
            region.QuadObject.transform.SetParent(transform);

            // Add MeshFilter and MeshRenderer
            var meshFilter = region.QuadObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = _sharedQuadMesh;

            region.Renderer = region.QuadObject.AddComponent<MeshRenderer>();
            region.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            region.Renderer.receiveShadows = false;

            // Create or get material from pool
            region.Material = GetOrCreateMaterial(region.Config.useStylizedNebula);
            region.Renderer.sharedMaterial = region.Material;

            // Apply preset to material
            ApplyPresetToMaterial(region);

            // Update region-specific properties
            region.UpdateMaterialProperties();
        }

        private void CleanupRegionRendering(NebulaRegion region)
        {
            if (region.Material != null)
            {
                // Return material to pool
                var pool = region.Config.useStylizedNebula ? _stylizedMaterialPool : _basicMaterialPool;
                pool.Enqueue(region.Material);
                region.Material = null;
            }

            if (region.QuadObject != null)
            {
                if (Application.isPlaying)
                    Destroy(region.QuadObject);
                else
                    DestroyImmediate(region.QuadObject);
                region.QuadObject = null;
            }

            region.Renderer = null;
        }

        private Material GetOrCreateMaterial(bool useStylized)
        {
            var pool = useStylized ? _stylizedMaterialPool : _basicMaterialPool;
            var shader = useStylized ? stylizedNebulaShader : nebulaShader;

            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            return new Material(shader)
            {
                hideFlags = HideFlags.DontSave
            };
        }

        private void ApplyPresetToMaterial(NebulaRegion region)
        {
            if (region.Material == null) return;

            if (region.Config.useStylizedNebula && region.Config.stylizedPreset != null)
            {
                ApplyStylizedPresetToMaterial(region.Material, region.Config.stylizedPreset);
            }
            else if (!region.Config.useStylizedNebula && region.Config.nebulaPreset != null)
            {
                ApplyBasicPresetToMaterial(region.Material, region.Config.nebulaPreset);
            }
        }

        private void ApplyBasicPresetToMaterial(Material material, NebulaLayerPreset preset)
        {
            material.SetFloat("_NoiseScale", preset.noiseScale);
            material.SetInt("_Octaves", preset.octaves);
            material.SetFloat("_Persistence", preset.persistence);
            material.SetFloat("_Lacunarity", preset.lacunarity);
            material.SetFloat("_WarpStrength", preset.warpStrength);
            material.SetFloat("_WarpScale", preset.warpScale);
            material.SetInt("_ColorCount", preset.colorCount);
            material.SetColor("_Color1", preset.color1);
            material.SetColor("_Color2", preset.color2);
            material.SetColor("_Color3", preset.color3);
            material.SetColor("_Color4", preset.color4);
            material.SetFloat("_GradientBias", preset.gradientBias);
            material.SetFloat("_GradientContrast", preset.gradientContrast);
            material.SetFloat("_EmissionIntensity", preset.emissionIntensity);
            material.SetFloat("_CoreEmissionBoost", preset.coreEmissionBoost);
            material.SetFloat("_Density", preset.density);
            material.SetFloat("_EdgeSoftness", preset.edgeSoftness);
            material.SetFloat("_Threshold", preset.threshold);
            material.SetFloat("_DetailFrequency", preset.detailFrequency);
            material.SetFloat("_RenderBackground", preset.renderBackground ? 1f : 0f);
            material.SetColor("_BackgroundColor", preset.backgroundColor);
            material.SetFloat("_Seed", preset.seed);
        }

        private void ApplyStylizedPresetToMaterial(Material material, StylizedNebulaLayerPreset preset)
        {
            // Style features
            material.SetFloat("_EnablePillars", preset.enablePillars ? 1f : 0f);
            material.SetFloat("_EnablePainterly", preset.enablePainterly ? 1f : 0f);
            material.SetFloat("_EnableTendrils", preset.enableTendrils ? 1f : 0f);
            material.SetFloat("_EnableCellular", preset.enableCellular ? 1f : 0f);

            // Pillar settings
            material.SetFloat("_PillarStretch", preset.pillarStretch);
            material.SetFloat("_PillarAngle", preset.pillarAngle);
            material.SetFloat("_PillarWarpBias", preset.pillarWarpBias);

            // Painterly settings
            material.SetInt("_PosterizeLevels", preset.posterizeLevels);
            material.SetFloat("_BrushStrokeScale", preset.brushStrokeScale);
            material.SetFloat("_BrushWarpAmount", preset.brushWarpAmount);

            // Tendril settings
            material.SetFloat("_CurlStrength", preset.curlStrength);
            material.SetFloat("_CurlScale", preset.curlScale);
            material.SetFloat("_TendrilLength", preset.tendrilLength);

            // Cellular settings
            material.SetFloat("_VoronoiScale", preset.voronoiScale);
            material.SetFloat("_CellEdgeWidth", preset.cellEdgeWidth);
            material.SetFloat("_BubbleInvert", preset.bubbleInvert ? 1f : 0f);

            // Internal structure
            material.SetFloat("_EnableDenseCores", preset.enableDenseCores ? 1f : 0f);
            material.SetFloat("_CoreIntensity", preset.coreIntensity);
            material.SetFloat("_HaloSize", preset.haloSize);
            material.SetFloat("_EnableEdgeLit", preset.enableEdgeLit ? 1f : 0f);
            material.SetFloat("_RimLightStrength", preset.rimLightStrength);
            material.SetColor("_RimLightColor", preset.rimLightColor);
            material.SetFloat("_EnableDepthBands", preset.enableDepthBands ? 1f : 0f);
            material.SetInt("_BandCount", preset.bandCount);
            material.SetFloat("_BandContrast", preset.bandContrast);
            material.SetFloat("_EnableBrightSpots", preset.enableBrightSpots ? 1f : 0f);
            material.SetFloat("_SpotDensity", preset.spotDensity);
            material.SetFloat("_SpotIntensity", preset.spotIntensity);

            // Edge style
            material.SetInt("_EdgeMode", (int)preset.edgeMode);
            material.SetFloat("_SilhouetteSharpness", preset.silhouetteSharpness);
            material.SetFloat("_InternalSoftness", preset.internalSoftness);

            // Base noise
            material.SetFloat("_NoiseScale", preset.noiseScale);
            material.SetInt("_Octaves", preset.octaves);
            material.SetFloat("_Persistence", preset.persistence);
            material.SetFloat("_Lacunarity", preset.lacunarity);
            material.SetFloat("_WarpStrength", preset.warpStrength);
            material.SetFloat("_WarpScale", preset.warpScale);

            // Color gradient
            material.SetInt("_ColorCount", preset.colorCount);
            material.SetColor("_Color1", preset.color1);
            material.SetColor("_Color2", preset.color2);
            material.SetColor("_Color3", preset.color3);
            material.SetColor("_Color4", preset.color4);
            material.SetFloat("_GradientBias", preset.gradientBias);
            material.SetFloat("_GradientContrast", preset.gradientContrast);

            // Emission
            material.SetFloat("_EmissionIntensity", preset.emissionIntensity);
            material.SetFloat("_CoreEmissionBoost", preset.coreEmissionBoost);

            // Density
            material.SetFloat("_Density", preset.density);
            material.SetFloat("_Threshold", preset.threshold);

            // Background
            material.SetFloat("_RenderBackground", preset.renderBackground ? 1f : 0f);
            material.SetColor("_BackgroundColor", preset.backgroundColor);

            // Seed
            material.SetFloat("_Seed", preset.seed);
        }

        private void UpdateVisibleRegions()
        {
            _visibleRegions.Clear();

            // Calculate camera frustum rect
            var frustumRect = GetCameraFrustumRect();

            // Find visible regions
            foreach (var region in _allRegions)
            {
                if (!region.IsActive) continue;
                if (!region.IntersectsFrustum(frustumRect)) continue;

                _visibleRegions.Add(region);

                // Cap visible regions
                if (_visibleRegions.Count >= maxVisibleRegions) break;
            }

            // Sort by priority (lower priority renders first = behind)
            _visibleRegions.Sort((a, b) => a.Config.sortingPriority.CompareTo(b.Config.sortingPriority));

            // Update visibility and rendering
            foreach (var region in _allRegions)
            {
                bool isVisible = _visibleRegions.Contains(region);

                if (region.QuadObject != null)
                {
                    region.QuadObject.SetActive(isVisible);
                }

                if (isVisible && region.IsDirty)
                {
                    region.UpdateMaterialProperties();
                }
            }
        }

        private void UpdateRegionTransforms()
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

            for (int i = 0; i < _visibleRegions.Count; i++)
            {
                var region = _visibleRegions[i];
                if (region.QuadObject == null) continue;

                // Position at camera with Z offset
                float zOffset = backgroundDepth + i * 0.01f;
                region.QuadObject.transform.position = new Vector3(cameraPos.x, cameraPos.y, cameraPos.z + zOffset);
                region.QuadObject.transform.rotation = Quaternion.identity;
                region.QuadObject.transform.localScale = new Vector3(width, height, 1f);

                // Set sorting order
                if (region.Renderer != null)
                {
                    region.Renderer.sortingOrder = -1000 + i;
                }
            }
        }

        private Rect GetCameraFrustumRect()
        {
            if (_camera == null) return new Rect();

            Vector3 camPos = _camera.transform.position;
            float halfHeight = _camera.orthographicSize + frustumPadding;
            float halfWidth = halfHeight * _camera.aspect;

            return new Rect(
                camPos.x - halfWidth,
                camPos.y - halfHeight,
                halfWidth * 2f,
                halfHeight * 2f
            );
        }

        private void DestroyMaterial(Material mat)
        {
            if (Application.isPlaying)
                Destroy(mat);
            else
                DestroyImmediate(mat);
        }

        private void DrawCircleGizmo(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }

        #endregion

        #region Floating Origin Support

        private void TrySubscribeToOriginShift()
        {
            if (_subscribedToOriginShift) return;

            var service = WorldGenerationService.Instance;
            if (service != null)
            {
                service.OnOriginShift += HandleOriginShift;
                _subscribedToOriginShift = true;

                if (logRegionEvents)
                {
                    Debug.Log("[Nebula] Subscribed to origin shift events");
                }
            }
        }

        private void UnsubscribeFromOriginShift()
        {
            if (!_subscribedToOriginShift) return;

            var service = WorldGenerationService.Instance;
            if (service != null)
            {
                service.OnOriginShift -= HandleOriginShift;
            }
            _subscribedToOriginShift = false;
        }

        private void HandleOriginShift(Vector2 shiftAmount)
        {
            // Add inverse of shift to maintain visual continuity (same pattern as StarfieldManager)
            _originShiftAccumulator -= shiftAmount;

            // Wrap symmetrically to prevent precision loss at extreme values
            // Uses [-WRAP_PERIOD/2, +WRAP_PERIOD/2) range to avoid zero-crossing discontinuity
            _originShiftAccumulator.x = WrapCoordinateSymmetric(_originShiftAccumulator.x, WRAP_PERIOD);
            _originShiftAccumulator.y = WrapCoordinateSymmetric(_originShiftAccumulator.y, WRAP_PERIOD);

            if (logRegionEvents)
            {
                Debug.Log($"[Nebula] Origin shift: {shiftAmount}, accumulator now: {_originShiftAccumulator}");
            }

            // Mark all regions dirty so they update their shader properties with new virtual positions
            foreach (var region in _allRegions)
            {
                region.MarkDirty();
            }
        }

        /// <summary>
        /// Converts an actual Unity world position to virtual position for shader use.
        /// This ensures nebula region positions match the camera's virtual position
        /// used by StarfieldManager for floating origin compatibility.
        /// Do NOT wrap the result - accumulator is already wrapped symmetrically during origin shifts.
        /// </summary>
        public Vector2 GetVirtualPosition(Vector2 actualWorldPos)
        {
            return actualWorldPos + _originShiftAccumulator;
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

        #endregion
    }
}

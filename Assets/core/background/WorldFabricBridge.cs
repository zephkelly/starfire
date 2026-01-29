using UnityEngine;
using StarfireV2;
using Starfire.Core.V2.World;
using System.Collections.Generic;

namespace Starfire.Core.Background
{
    /// <summary>
    /// Bridges the World Fabric system to starfield layers by providing per-depth
    /// fabric sampling with smooth transitions.
    ///
    /// Each layer queries its own smoothed sample based on parallax depth, creating
    /// depth-aware zone transitions (deep layers respond differently than foreground).
    ///
    /// Also pushes shader globals as fallback for any shaders still reading globals.
    /// Place on the same GameObject as StarfieldManager.
    /// </summary>
    [ExecuteAlways]
    public class WorldFabricBridge : MonoBehaviour
    {
        [Header("Transition")]
        [Tooltip("How fast fabric values transition (seconds for full change)")]
        [Range(0.5f, 10f)]
        [SerializeField] private float transitionSpeed = 3f;

        [Header("Depth Influence")]
        [Tooltip("How much parallax depth offsets the fabric sample point. Higher = more variation between layers.")]
        [Range(0f, 50000f)]
        [SerializeField] private float fabricDepthInfluence = 10000f;

        [Header("Void Response")]
        [Tooltip("How much void factor dims star brightness (0 = no effect, 1 = full fade)")]
        [Range(0f, 1f)]
        [SerializeField] private float voidStarFade = 0.85f;

        [Tooltip("How much void darkens the background color")]
        [Range(0f, 1f)]
        [SerializeField] private float voidBackgroundDarken = 0.6f;

        [Header("Nebula Response")]
        [Tooltip("How much nebula tints star color")]
        [Range(0f, 1f)]
        [SerializeField] private float nebulaStarTint = 0.15f;

        [Tooltip("Nebula tint color applied to stars")]
        [SerializeField] private Color nebulaTintColor = new Color(0.6f, 0.3f, 0.7f, 1f);

        [Header("Anomaly Response")]
        [Tooltip("How much anomaly affects star color shift")]
        [Range(0f, 1f)]
        [SerializeField] private float anomalyColorShift = 0.3f;

        [Header("Color Distribution")]
        [Tooltip("Enable fabric-based color distribution for nebulae")]
        [SerializeField] private bool enableColorDistribution = true;

        [Tooltip("How fast color palettes transition (seconds for full change)")]
        [Range(0.5f, 10f)]
        [SerializeField] private float colorTransitionSpeed = 2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugValues = false;

        // Per-depth smoothed sample cache
        // Key = parallax depth (rounded to avoid float key issues), Value = smoothed sample
        private readonly Dictionary<int, SmoothedFabricSample> _depthSamples = new();

        // Per-depth smoothed palette cache
        private readonly Dictionary<int, SmoothedColorPalette> _depthPalettes = new();

        // Primary smoothed palette for globals
        private ColorPalette _smoothPalette = ColorPalette.Default;

        // Primary (camera-position) smoothed values for globals fallback
        private float _smoothNebulaDensity;
        private float _smoothAsteroidDensity;
        private float _smoothVoidFactor;
        private float _smoothAnomalyStrength;

        // Shader property IDs (cached) — kept for global fallback
        private static readonly int FabricNebulaDensityID = Shader.PropertyToID("_FabricNebulaDensity");
        private static readonly int FabricAsteroidDensityID = Shader.PropertyToID("_FabricAsteroidDensity");
        private static readonly int FabricVoidFactorID = Shader.PropertyToID("_FabricVoidFactor");
        private static readonly int FabricAnomalyStrengthID = Shader.PropertyToID("_FabricAnomalyStrength");
        private static readonly int FabricVoidStarFadeID = Shader.PropertyToID("_FabricVoidStarFade");
        private static readonly int FabricVoidBgDarkenID = Shader.PropertyToID("_FabricVoidBgDarken");
        private static readonly int FabricNebulaTintID = Shader.PropertyToID("_FabricNebulaTint");
        private static readonly int FabricNebulaTintStrengthID = Shader.PropertyToID("_FabricNebulaTintStrength");
        private static readonly int FabricAnomalyShiftID = Shader.PropertyToID("_FabricAnomalyShift");

        // Material property IDs (same names, used for per-material setting)
        public static readonly int MatFabricNebulaDensityID = Shader.PropertyToID("_FabricNebulaDensity");
        public static readonly int MatFabricAsteroidDensityID = Shader.PropertyToID("_FabricAsteroidDensity");
        public static readonly int MatFabricVoidFactorID = Shader.PropertyToID("_FabricVoidFactor");
        public static readonly int MatFabricAnomalyStrengthID = Shader.PropertyToID("_FabricAnomalyStrength");
        public static readonly int MatFabricVoidStarFadeID = Shader.PropertyToID("_FabricVoidStarFade");
        public static readonly int MatFabricVoidBgDarkenID = Shader.PropertyToID("_FabricVoidBgDarken");
        public static readonly int MatFabricNebulaTintID = Shader.PropertyToID("_FabricNebulaTint");
        public static readonly int MatFabricNebulaTintStrengthID = Shader.PropertyToID("_FabricNebulaTintStrength");
        public static readonly int MatFabricAnomalyShiftID = Shader.PropertyToID("_FabricAnomalyShift");

        // Color distribution property IDs
        public static readonly int MatFabricColor1ID = Shader.PropertyToID("_FabricColor1");
        public static readonly int MatFabricColor2ID = Shader.PropertyToID("_FabricColor2");
        public static readonly int MatFabricColor3ID = Shader.PropertyToID("_FabricColor3");
        public static readonly int MatFabricColor4ID = Shader.PropertyToID("_FabricColor4");
        public static readonly int MatFabricColorBlendID = Shader.PropertyToID("_FabricColorBlend");

        private Camera _camera;
        private float _lastDt;
        private float _lastLerpRate;
        private Vector2 _lastCamWorldPos;
        private float _nextLogTime;
        private bool _loggedInit;

        /// <summary>Singleton-style access for layers to find the bridge.</summary>
        public static WorldFabricBridge Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
            _camera = Camera.main;
            if (_camera == null)
                _camera = FindFirstObjectByType<Camera>();
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            _camera = Camera.main;
            if (_camera == null)
            {
                if (!_loggedInit) { Debug.LogWarning("[WorldFabricBridge] Camera.main is null"); _loggedInit = true; }
                return;
            }

            var fabricService = WorldFabricService.Instance;
            if (fabricService == null)
            {
                if (!_loggedInit) { Debug.LogWarning("[WorldFabricBridge] WorldFabricService.Instance is null — fabric values will be 0"); _loggedInit = true; }
                return;
            }

            if (!_loggedInit)
            {
                Debug.Log("[WorldFabricBridge] Initialized — fabricService found, camera found");
                _loggedInit = true;
            }

            // Cache common values for this frame
            _lastCamWorldPos = (Vector2)_camera.transform.position;
            _lastDt = Time.deltaTime;
            _lastLerpRate = 1f - Mathf.Exp(-_lastDt / Mathf.Max(transitionSpeed * 0.33f, 0.01f));

            // Sample at camera position for globals fallback
            SpaceFabricSample sample = fabricService.SampleFabricAtWorldPosition(_lastCamWorldPos);

            _smoothNebulaDensity = Mathf.Lerp(_smoothNebulaDensity, sample.NebulaDensity, _lastLerpRate);
            _smoothAsteroidDensity = Mathf.Lerp(_smoothAsteroidDensity, sample.AsteroidDensity, _lastLerpRate);
            _smoothVoidFactor = Mathf.Lerp(_smoothVoidFactor, sample.VoidFactor, _lastLerpRate);
            _smoothAnomalyStrength = Mathf.Lerp(_smoothAnomalyStrength, sample.AnomalyStrength, _lastLerpRate);

            // Diagnostic: log fabric values every 2 seconds
            if (Time.time >= _nextLogTime)
            {
                Debug.Log($"[WorldFabricBridge] cam=({_lastCamWorldPos.x:F0},{_lastCamWorldPos.y:F0}) raw nebula={sample.NebulaDensity:F3} smooth={_smoothNebulaDensity:F3} void={_smoothVoidFactor:F3} anomaly={_smoothAnomalyStrength:F3}");
                _nextLogTime = Time.time + 2f;
            }

            // Update all cached per-depth samples
            foreach (var kvp in _depthSamples)
            {
                kvp.Value.Update(fabricService, _lastCamWorldPos, fabricDepthInfluence, _lastLerpRate);
            }

            // Update color palettes if enabled
            if (enableColorDistribution)
            {
                float colorLerpRate = 1f - Mathf.Exp(-_lastDt / Mathf.Max(colorTransitionSpeed * 0.33f, 0.01f));

                // Sample colors at camera position for globals
                var colorSample = fabricService.SampleColorsAtWorldPosition(_lastCamWorldPos);
                _smoothPalette = ColorPalette.Lerp(_smoothPalette, colorSample.Palette, colorLerpRate);

                // Update all cached per-depth palettes
                foreach (var kvp in _depthPalettes)
                {
                    kvp.Value.Update(fabricService, _lastCamWorldPos, fabricDepthInfluence, colorLerpRate);
                }
            }

            // Push globals as fallback
            Shader.SetGlobalFloat(FabricNebulaDensityID, _smoothNebulaDensity);
            Shader.SetGlobalFloat(FabricAsteroidDensityID, _smoothAsteroidDensity);
            Shader.SetGlobalFloat(FabricVoidFactorID, _smoothVoidFactor);
            Shader.SetGlobalFloat(FabricAnomalyStrengthID, _smoothAnomalyStrength);

            Shader.SetGlobalFloat(FabricVoidStarFadeID, voidStarFade);
            Shader.SetGlobalFloat(FabricVoidBgDarkenID, voidBackgroundDarken);
            Shader.SetGlobalVector(FabricNebulaTintID, new Vector4(nebulaTintColor.r, nebulaTintColor.g, nebulaTintColor.b, 1f));
            Shader.SetGlobalFloat(FabricNebulaTintStrengthID, nebulaStarTint);
            Shader.SetGlobalFloat(FabricAnomalyShiftID, anomalyColorShift);
        }

        /// <summary>
        /// Get a smoothed fabric sample for a specific parallax depth.
        /// Layers at different depths get slightly different samples, creating depth-aware transitions.
        /// </summary>
        public SpaceFabricSample GetSmoothedSampleForDepth(float parallaxDepth)
        {
            // Quantize depth to avoid dictionary explosion (bucket to 4 decimal places)
            int depthKey = Mathf.RoundToInt(parallaxDepth * 10000f);

            if (!_depthSamples.TryGetValue(depthKey, out var smoothed))
            {
                smoothed = new SmoothedFabricSample(parallaxDepth);
                _depthSamples[depthKey] = smoothed;
            }

            return smoothed.Sample;
        }

        /// <summary>
        /// Get a smoothed color palette for a specific parallax depth.
        /// Different parallax depths get subtly different colors, creating depth-aware transitions.
        /// </summary>
        public ColorPalette GetSmoothedPaletteForDepth(float parallaxDepth)
        {
            if (!enableColorDistribution)
                return ColorPalette.Default;

            int depthKey = Mathf.RoundToInt(parallaxDepth * 10000f);

            if (!_depthPalettes.TryGetValue(depthKey, out var smoothed))
            {
                smoothed = new SmoothedColorPalette(parallaxDepth);
                _depthPalettes[depthKey] = smoothed;
            }

            return smoothed.Palette;
        }

        /// <summary>
        /// Whether color distribution is enabled.
        /// </summary>
        public bool IsColorDistributionEnabled => enableColorDistribution;

        /// <summary>
        /// Apply fabric properties to a material for a given parallax depth.
        /// Call this from StarfieldLayer.ConfigureMaterial().
        /// </summary>
        public void ApplyFabricToMaterial(Material material, float parallaxDepth)
        {
            var sample = GetSmoothedSampleForDepth(parallaxDepth);

            material.SetFloat(MatFabricNebulaDensityID, sample.NebulaDensity);
            material.SetFloat(MatFabricAsteroidDensityID, sample.AsteroidDensity);
            material.SetFloat(MatFabricVoidFactorID, sample.VoidFactor);
            material.SetFloat(MatFabricAnomalyStrengthID, sample.AnomalyStrength);

            material.SetFloat(MatFabricVoidStarFadeID, voidStarFade);
            material.SetFloat(MatFabricVoidBgDarkenID, voidBackgroundDarken);
            material.SetVector(MatFabricNebulaTintID, new Vector4(nebulaTintColor.r, nebulaTintColor.g, nebulaTintColor.b, 1f));
            material.SetFloat(MatFabricNebulaTintStrengthID, nebulaStarTint);
            material.SetFloat(MatFabricAnomalyShiftID, anomalyColorShift);
        }

        /// <summary>
        /// Apply fabric properties including color palette to a material.
        /// Call this from nebula layers that support dynamic coloring.
        /// </summary>
        /// <param name="material">The material to apply properties to.</param>
        /// <param name="parallaxDepth">The layer's parallax depth.</param>
        /// <param name="colorBlend">How much to blend toward fabric colors (0 = authored, 1 = fabric).</param>
        public void ApplyFabricToMaterialWithColors(Material material, float parallaxDepth, float colorBlend)
        {
            // Apply standard fabric properties
            ApplyFabricToMaterial(material, parallaxDepth);

            // Apply color palette if enabled
            if (enableColorDistribution && colorBlend > 0f)
            {
                var palette = GetSmoothedPaletteForDepth(parallaxDepth);

                material.SetColor(MatFabricColor1ID, palette.Color1);
                material.SetColor(MatFabricColor2ID, palette.Color2);
                material.SetColor(MatFabricColor3ID, palette.Color3);
                material.SetColor(MatFabricColor4ID, palette.Color4);
                material.SetFloat(MatFabricColorBlendID, colorBlend);
            }
            else
            {
                material.SetFloat(MatFabricColorBlendID, 0f);
            }
        }

        /// <summary>
        /// Current smoothed fabric sample at camera position (for other scripts).
        /// </summary>
        public SpaceFabricSample SmoothedSample => new SpaceFabricSample
        {
            NebulaDensity = _smoothNebulaDensity,
            AsteroidDensity = _smoothAsteroidDensity,
            VoidFactor = _smoothVoidFactor,
            AnomalyStrength = _smoothAnomalyStrength,
        };

        /// <summary>Response parameters (read by layers for reference).</summary>
        public float VoidStarFade => voidStarFade;
        public float VoidBackgroundDarken => voidBackgroundDarken;
        public float NebulaStarTint => nebulaStarTint;
        public Color NebulaTintColor => nebulaTintColor;
        public float AnomalyColorShift => anomalyColorShift;

        /// <summary>
        /// Current smoothed color palette at camera position.
        /// </summary>
        public ColorPalette SmoothedPalette => _smoothPalette;

        /// <summary>
        /// Internal smoothed sample tracker for a specific parallax depth.
        /// </summary>
        private class SmoothedFabricSample
        {
            public float ParallaxDepth;
            public SpaceFabricSample Sample;

            public SmoothedFabricSample(float parallaxDepth)
            {
                ParallaxDepth = parallaxDepth;
                Sample = default;
            }

            public void Update(WorldFabricService fabricService, Vector2 camWorldPos, float depthInfluence, float lerpRate)
            {
                // Offset sample position by parallax depth — deeper layers sample at a slightly different position
                // This creates a subtle parallax effect in zone transitions
                Vector2 samplePos = camWorldPos + camWorldPos.normalized * ParallaxDepth * depthInfluence;
                SpaceFabricSample raw = fabricService.SampleFabricAtWorldPosition(samplePos);

                Sample = new SpaceFabricSample
                {
                    NebulaDensity = Mathf.Lerp(Sample.NebulaDensity, raw.NebulaDensity, lerpRate),
                    AsteroidDensity = Mathf.Lerp(Sample.AsteroidDensity, raw.AsteroidDensity, lerpRate),
                    VoidFactor = Mathf.Lerp(Sample.VoidFactor, raw.VoidFactor, lerpRate),
                    AnomalyStrength = Mathf.Lerp(Sample.AnomalyStrength, raw.AnomalyStrength, lerpRate),
                };
            }
        }

        /// <summary>
        /// Internal smoothed color palette tracker for a specific parallax depth.
        /// </summary>
        private class SmoothedColorPalette
        {
            public float ParallaxDepth;
            public ColorPalette Palette;

            public SmoothedColorPalette(float parallaxDepth)
            {
                ParallaxDepth = parallaxDepth;
                Palette = ColorPalette.Default;
            }

            public void Update(WorldFabricService fabricService, Vector2 camWorldPos, float depthInfluence, float lerpRate)
            {
                // Offset sample position by parallax depth
                Vector2 samplePos = camWorldPos + camWorldPos.normalized * ParallaxDepth * depthInfluence;
                var colorSample = fabricService.SampleColorsAtWorldPosition(samplePos);

                // Smooth interpolation using HSV for natural color transitions
                Palette = ColorPalette.Lerp(Palette, colorSample.Palette, lerpRate);
            }
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugValues || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 10, 250, 120));
            GUILayout.Label($"Nebula: {_smoothNebulaDensity:F3}");
            GUILayout.Label($"Asteroid: {_smoothAsteroidDensity:F3}");
            GUILayout.Label($"Void: {_smoothVoidFactor:F3}");
            GUILayout.Label($"Anomaly: {_smoothAnomalyStrength:F3}");
            GUILayout.EndArea();
        }
#endif
    }
}

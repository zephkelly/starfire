using UnityEngine;
using StarfireV2;
using Starfire.Core.V2.World;

namespace Starfire.Core.Background
{
    /// <summary>
    /// Bridges the World Fabric system to the StarfieldManager by sampling
    /// fabric data at the camera position and feeding it to shaders as globals.
    ///
    /// Smoothly lerps values to prevent visual popping when crossing zone boundaries.
    /// Place on the same GameObject as StarfieldManager.
    /// </summary>
    [ExecuteAlways]
    public class WorldFabricBridge : MonoBehaviour
    {
        [Header("Transition")]
        [Tooltip("How fast fabric values transition (seconds for full change)")]
        [Range(0.5f, 10f)]
        [SerializeField] private float transitionSpeed = 3f;

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

        [Header("Debug")]
        [SerializeField] private bool showDebugValues = false;

        // Smoothed values
        private float _smoothNebulaDensity;
        private float _smoothAsteroidDensity;
        private float _smoothVoidFactor;
        private float _smoothAnomalyStrength;

        // Shader property IDs (cached)
        private static readonly int FabricNebulaDensityID = Shader.PropertyToID("_FabricNebulaDensity");
        private static readonly int FabricAsteroidDensityID = Shader.PropertyToID("_FabricAsteroidDensity");
        private static readonly int FabricVoidFactorID = Shader.PropertyToID("_FabricVoidFactor");
        private static readonly int FabricAnomalyStrengthID = Shader.PropertyToID("_FabricAnomalyStrength");
        private static readonly int FabricVoidStarFadeID = Shader.PropertyToID("_FabricVoidStarFade");
        private static readonly int FabricVoidBgDarkenID = Shader.PropertyToID("_FabricVoidBgDarken");
        private static readonly int FabricNebulaTintID = Shader.PropertyToID("_FabricNebulaTint");
        private static readonly int FabricNebulaTintStrengthID = Shader.PropertyToID("_FabricNebulaTintStrength");
        private static readonly int FabricAnomalyShiftID = Shader.PropertyToID("_FabricAnomalyShift");

        private Camera _camera;

        private void OnEnable()
        {
            _camera = Camera.main;
            if (_camera == null)
                _camera = FindFirstObjectByType<Camera>();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            _camera = Camera.main;
            if (_camera == null) return;

            var fabricService = WorldFabricService.Instance;
            if (fabricService == null) return;

            // Sample fabric at camera world position
            Vector2 camPos = _camera.transform.position;
            SpaceFabricSample sample = fabricService.SampleFabricAtWorldPosition(camPos);

            // Smooth transitions
            float dt = Time.deltaTime;
            float lerpRate = 1f - Mathf.Exp(-dt / Mathf.Max(transitionSpeed * 0.33f, 0.01f));

            _smoothNebulaDensity = Mathf.Lerp(_smoothNebulaDensity, sample.NebulaDensity, lerpRate);
            _smoothAsteroidDensity = Mathf.Lerp(_smoothAsteroidDensity, sample.AsteroidDensity, lerpRate);
            _smoothVoidFactor = Mathf.Lerp(_smoothVoidFactor, sample.VoidFactor, lerpRate);
            _smoothAnomalyStrength = Mathf.Lerp(_smoothAnomalyStrength, sample.AnomalyStrength, lerpRate);

            // Push to shader globals
            Shader.SetGlobalFloat(FabricNebulaDensityID, _smoothNebulaDensity);
            Shader.SetGlobalFloat(FabricAsteroidDensityID, _smoothAsteroidDensity);
            Shader.SetGlobalFloat(FabricVoidFactorID, _smoothVoidFactor);
            Shader.SetGlobalFloat(FabricAnomalyStrengthID, _smoothAnomalyStrength);

            // Push response parameters
            Shader.SetGlobalFloat(FabricVoidStarFadeID, voidStarFade);
            Shader.SetGlobalFloat(FabricVoidBgDarkenID, voidBackgroundDarken);
            Shader.SetGlobalVector(FabricNebulaTintID, new Vector4(nebulaTintColor.r, nebulaTintColor.g, nebulaTintColor.b, 1f));
            Shader.SetGlobalFloat(FabricNebulaTintStrengthID, nebulaStarTint);
            Shader.SetGlobalFloat(FabricAnomalyShiftID, anomalyColorShift);
        }

        /// <summary>
        /// Current smoothed fabric sample (for other scripts to read).
        /// </summary>
        public SpaceFabricSample SmoothedSample => new SpaceFabricSample
        {
            NebulaDensity = _smoothNebulaDensity,
            AsteroidDensity = _smoothAsteroidDensity,
            VoidFactor = _smoothVoidFactor,
            AnomalyStrength = _smoothAnomalyStrength,
        };

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

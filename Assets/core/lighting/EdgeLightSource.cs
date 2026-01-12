using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Starfire.Core.Lighting
{
    /// <summary>
    /// Attach this component to a Light2D to mark it as an edge light source.
    /// Only Light2D components with this script will contribute to the edge lighting effect.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    [ExecuteAlways]
    public class EdgeLightSource : MonoBehaviour
    {
        [Header("Edge Light Settings")]
        [Tooltip("Multiplier for this light's contribution to edge lighting")]
        [SerializeField, Range(0f, 2f)] private float _edgeIntensityMultiplier = 1f;

        [Tooltip("Override the light's range for edge lighting calculations (0 = use light's range)")]
        [SerializeField, Min(0f)] private float _rangeOverride = 0f;

        private Light2D _light2D;

        /// <summary>
        /// World position of the light source.
        /// </summary>
        public Vector3 WorldPosition => transform.position;

        /// <summary>
        /// Color of the light.
        /// </summary>
        public Color LightColor => _light2D != null ? _light2D.color : Color.white;

        /// <summary>
        /// Effective intensity for edge lighting (light intensity * multiplier).
        /// </summary>
        public float Intensity => (_light2D != null ? _light2D.intensity : 1f) * _edgeIntensityMultiplier;

        /// <summary>
        /// Range of the light for falloff calculations.
        /// </summary>
        public float Range
        {
            get
            {
                if (_rangeOverride > 0f)
                {
                    return _rangeOverride;
                }

                if (_light2D == null)
                {
                    return 10f;
                }

                // Use outer radius for point/spot lights, or a default for global lights
                return _light2D.lightType == Light2D.LightType.Global
                    ? 100f
                    : _light2D.pointLightOuterRadius;
            }
        }

        /// <summary>
        /// Whether this light source should contribute to edge lighting.
        /// </summary>
        public bool IsEnabled => enabled && _light2D != null && _light2D.enabled;

        private void Awake()
        {
            _light2D = GetComponent<Light2D>();
        }

        private void OnEnable()
        {
            _light2D ??= GetComponent<Light2D>();
            RegisterWithManager();
        }

        private void RegisterWithManager()
        {
            // Try Instance first
            if (EdgeLightManager.Instance != null)
            {
                EdgeLightManager.Instance.Register(this);
                return;
            }

            // Fallback: find manager in scene (handles script execution order issues)
            var manager = FindAnyObjectByType<EdgeLightManager>();
            if (manager != null)
            {
                manager.Register(this);
            }
        }

        private void OnDisable()
        {
            EdgeLightManager.Instance?.Unregister(this);
        }

        private void OnValidate()
        {
            _light2D ??= GetComponent<Light2D>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw the effective range for edge lighting
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, Range);
        }
#endif
    }
}

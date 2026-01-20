using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for thruster visual effects.
    /// Defines how thruster exhaust appears when firing.
    /// </summary>
    [CreateAssetMenu(fileName = "ThrusterVisual", menuName = "StarfireV2/Modules/ThrusterVisualConfig")]
    public class ThrusterVisualConfig : ScriptableObject
    {
        [Header("Prefab (Optional)")]
        [Tooltip("Visual prefab to spawn at each thruster position. Leave empty to use code-generated particles.")]
        [SerializeField] private GameObject visualPrefab;

        [Header("Code-Generated Particle Settings")]
        [Tooltip("Particle lifetime in seconds")]
        [SerializeField] private float particleLifetime = 0.15f;

        [Tooltip("Particle emission speed")]
        [SerializeField] private float particleSpeed = 5f;

        [Tooltip("Particle start size")]
        [SerializeField] private float particleStartSize = 0.04f;

        [Tooltip("Particle end size multiplier")]
        [Range(0f, 1f)]
        [SerializeField] private float particleEndSizeMultiplier = 0.1f;

        [Tooltip("Emission cone angle in degrees")]
        [Range(0f, 90f)]
        [SerializeField] private float particleConeAngle = 3f;

        [Tooltip("Particle start color")]
        [SerializeField] private Color particleStartColor = new Color(0.7f, 0.7f, 0.7f, 0.9f);

        [Tooltip("Particle end color (fades to this)")]
        [SerializeField] private Color particleEndColor = new Color(0.4f, 0.4f, 0.4f, 0f);

        [Tooltip("Enable Light2D for thruster glow")]
        [SerializeField] private bool enableLight = true;

        [Header("Response")]
        [Tooltip("How quickly visuals respond to thrust changes (higher = faster)")]
        [Range(1f, 50f)]
        [SerializeField] private float responseSpeed = 15f;

        [Header("Particle Settings")]
        [Tooltip("Base particle emission rate when thruster is at minimum power")]
        [SerializeField] private float baseEmissionRate = 20f;

        [Tooltip("Maximum particle emission rate at full thrust")]
        [SerializeField] private float maxEmissionRate = 150f;

        [Header("Light Settings")]
        [Tooltip("Color of the thruster light")]
        [SerializeField] private Color lightColor = new Color(0.9f, 0.85f, 0.8f, 1f);

        [Tooltip("Light intensity at minimum thrust")]
        [SerializeField] private float baseLightIntensity = 0.2f;

        [Tooltip("Light intensity at full thrust")]
        [SerializeField] private float maxLightIntensity = 1.5f;

        [Tooltip("Light radius/range")]
        [SerializeField] private float lightRadius = 0.5f;

        [Header("Glow Sprite Settings")]
        [Tooltip("Scale of glow sprite at minimum thrust")]
        [SerializeField] private float minGlowScale = 0.3f;

        [Tooltip("Scale of glow sprite at full thrust")]
        [SerializeField] private float maxGlowScale = 1.2f;

        [Tooltip("Alpha of glow sprite at full thrust")]
        [Range(0f, 1f)]
        [SerializeField] private float maxGlowAlpha = 0.8f;

        [Header("Color Variation")]
        [Tooltip("Glow color at minimum thrust")]
        [SerializeField] private Color minThrustColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Tooltip("Glow color at full thrust")]
        [SerializeField] private Color maxThrustColor = new Color(0.85f, 0.82f, 0.78f, 1f);

        [Header("Particle Physics")]
        [Tooltip("Velocity inheritance at low ship speed (0-1). Higher values create more exhaust drift.")]
        [Range(0f, 1f)]
        [SerializeField] private float velocityInheritanceLow = 0.3f;

        [Tooltip("Velocity inheritance at high ship speed (0-1). Lower values keep particles attached to ship.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float velocityInheritanceHigh = 0.01f;

        [Tooltip("Ship speed at which inheritance reaches minimum (units/sec)")]
        [SerializeField] private float velocityInheritanceSpeedThreshold = 20f;

        // Properties
        public GameObject VisualPrefab => visualPrefab;
        public bool UseCodeGenerated => visualPrefab == null;
        public float ResponseSpeed => responseSpeed;

        // Code-generated particle properties
        public float ParticleLifetime => particleLifetime;
        public float ParticleSpeed => particleSpeed;
        public float ParticleStartSize => particleStartSize;
        public float ParticleEndSizeMultiplier => particleEndSizeMultiplier;
        public float ParticleConeAngle => particleConeAngle;
        public Color ParticleStartColor => particleStartColor;
        public Color ParticleEndColor => particleEndColor;
        public bool EnableLight => enableLight;

        public float BaseEmissionRate => baseEmissionRate;
        public float MaxEmissionRate => maxEmissionRate;

        public Color LightColor => lightColor;
        public float BaseLightIntensity => baseLightIntensity;
        public float MaxLightIntensity => maxLightIntensity;
        public float LightRadius => lightRadius;

        public float MinGlowScale => minGlowScale;
        public float MaxGlowScale => maxGlowScale;
        public float MaxGlowAlpha => maxGlowAlpha;

        public Color MinThrustColor => minThrustColor;
        public Color MaxThrustColor => maxThrustColor;

        // Particle physics
        public float VelocityInheritanceLow => velocityInheritanceLow;
        public float VelocityInheritanceHigh => velocityInheritanceHigh;
        public float VelocityInheritanceSpeedThreshold => velocityInheritanceSpeedThreshold;

        /// <summary>
        /// Interpolate emission rate based on normalized thrust.
        /// </summary>
        public float GetEmissionRate(float normalizedThrust)
        {
            return Mathf.Lerp(baseEmissionRate, maxEmissionRate, normalizedThrust);
        }

        /// <summary>
        /// Interpolate light intensity based on normalized thrust.
        /// </summary>
        public float GetLightIntensity(float normalizedThrust)
        {
            return Mathf.Lerp(baseLightIntensity, maxLightIntensity, normalizedThrust);
        }

        /// <summary>
        /// Interpolate glow scale based on normalized thrust.
        /// </summary>
        public float GetGlowScale(float normalizedThrust)
        {
            return Mathf.Lerp(minGlowScale, maxGlowScale, normalizedThrust);
        }

        /// <summary>
        /// Interpolate glow color based on normalized thrust.
        /// </summary>
        public Color GetGlowColor(float normalizedThrust)
        {
            return Color.Lerp(minThrustColor, maxThrustColor, normalizedThrust);
        }

        /// <summary>
        /// Calculate velocity inheritance based on ship speed.
        /// At low speeds, particles drift more. At high speeds, they stay attached.
        /// </summary>
        public float GetVelocityInheritance(float shipSpeed)
        {
            float t = Mathf.Clamp01(shipSpeed / velocityInheritanceSpeedThreshold);
            return Mathf.Lerp(velocityInheritanceLow, velocityInheritanceHigh, t);
        }
    }
}

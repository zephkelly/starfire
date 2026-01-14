using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Configuration for visual and audio effects when a projectile impacts a target.
    /// </summary>
    [CreateAssetMenu(fileName = "ImpactConfig", menuName = "Starfire/Weapons/Impact Config")]
    public class ImpactConfig : ScriptableObject
    {
        [Header("Particle Effects")]
        [Tooltip("Particle system prefab spawned at impact point")]
        public GameObject particlePrefab;

        [Tooltip("Scale multiplier for the particle effect")]
        public float particleScale = 1f;

        [Header("Light Settings")]
        [Tooltip("Enable a flash of light on impact")]
        public bool enableLight = true;

        [Tooltip("Color of the impact light")]
        public Color lightColor = Color.yellow;

        [Tooltip("Initial intensity of the impact light")]
        public float lightIntensity = 2f;

        [Tooltip("Radius of the impact light")]
        public float lightRadius = 3f;

        [Header("Timing")]
        [Tooltip("Total duration of the impact effect before cleanup")]
        public float duration = 0.5f;

        [Tooltip("Time in seconds before fade begins")]
        public float fadeStartTime = 0.1f;

        [Tooltip("Curve controlling how light fades over time (0-1 normalized)")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Audio")]
        [Tooltip("Sound played on impact")]
        public AudioClip impactSound;

        [Tooltip("Volume of the impact sound")]
        [Range(0f, 1f)]
        public float soundVolume = 0.5f;

        [Header("Shield Reflection")]
        [Tooltip("Enable particle trails when projectile is reflected by shields")]
        public bool enableReflection = true;

        [Tooltip("Number of reflection trail particles to spawn")]
        [Range(3, 20)]
        public int reflectionParticleCount = 8;

        [Tooltip("Base speed of reflection particles")]
        public float reflectionSpeed = 15f;

        [Tooltip("Random speed variation (0-1 multiplier range)")]
        [Range(0f, 1f)]
        public float reflectionSpeedVariation = 0.3f;

        [Tooltip("Spread angle in degrees from reflected direction")]
        [Range(0f, 90f)]
        public float reflectionSpreadAngle = 25f;

        [Tooltip("Color of reflection particles")]
        public Color reflectionColor = new Color(1f, 0.8f, 0.2f, 1f);

        [Tooltip("Lifetime of each reflection particle")]
        public float reflectionLifetime = 0.3f;

        [Tooltip("Width of the trail streak")]
        public float reflectionTrailWidth = 0.08f;

        [Tooltip("Length multiplier for trail (based on particle speed)")]
        public float reflectionTrailLengthMultiplier = 0.4f;

        [Tooltip("Overall intensity multiplier for the reflection effect")]
        [Range(0f, 2f)]
        public float reflectionIntensity = 1f;
    }
}

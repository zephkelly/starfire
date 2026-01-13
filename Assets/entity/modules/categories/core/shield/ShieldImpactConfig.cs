using UnityEngine;

namespace Starfire.Entity.Modules.Shield
{
    /// <summary>
    /// Configuration for visual and audio effects when a projectile impacts a shield boundary.
    /// </summary>
    [CreateAssetMenu(fileName = "ShieldImpactConfig", menuName = "Starfire/Shield/Impact Config")]
    public class ShieldImpactConfig : ScriptableObject
    {
        [Header("Particle Effects")]
        [Tooltip("Particle system prefab spawned at impact point")]
        public GameObject particlePrefab;

        [Tooltip("Scale multiplier for the particle effect")]
        public float particleScale = 1f;

        [Header("Shield Ripple")]
        [Tooltip("Enable a ripple effect on the shield when hit")]
        public bool enableRipple = true;

        [Tooltip("Color of the shield ripple")]
        public Color rippleColor = new Color(0.3f, 0.6f, 1f, 0.8f);

        [Tooltip("Duration of the ripple effect")]
        public float rippleDuration = 0.3f;

        [Tooltip("Size of the ripple relative to shield boundary")]
        public float rippleSize = 1f;

        [Header("Light Flash")]
        [Tooltip("Enable a flash of light on impact")]
        public bool enableLight = true;

        [Tooltip("Color of the impact light")]
        public Color lightColor = Color.cyan;

        [Tooltip("Initial intensity of the impact light")]
        public float lightIntensity = 2f;

        [Tooltip("Radius of the impact light")]
        public float lightRadius = 2f;

        [Tooltip("Duration of the light flash")]
        public float lightDuration = 0.2f;

        [Header("Timing")]
        [Tooltip("Total duration of the impact effect before cleanup")]
        public float duration = 0.5f;

        [Tooltip("Curve controlling how effects fade over time (0-1 normalized)")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Audio")]
        [Tooltip("Sound played on shield impact")]
        public AudioClip impactSound;

        [Tooltip("Volume of the impact sound")]
        [Range(0f, 1f)]
        public float soundVolume = 0.5f;
    }
}

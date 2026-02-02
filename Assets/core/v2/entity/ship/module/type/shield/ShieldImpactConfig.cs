using UnityEngine;

namespace StarfireV2
{
    [CreateAssetMenu(fileName = "ShieldImpactConfig", menuName = "StarfireV2/Shield/Impact Config")]
    public class ShieldImpactConfig : ScriptableObject
    {
        [Header("Particle Effects")]
        [Tooltip("Particle system prefab spawned at impact point")]
        public GameObject particlePrefab;
        public float particleScale = 1f;

        [Header("Shield Ripple")]
        public bool enableRipple = true;
        public Color rippleColor = new Color(0.3f, 0.6f, 1f, 0.8f);
        public float rippleDuration = 0.3f;
        public float rippleSize = 1f;

        [Header("Light Flash")]
        public bool enableLight = true;
        public Color lightColor = Color.cyan;
        public float lightIntensity = 2f;
        public float lightRadius = 2f;
        public float lightDuration = 0.2f;

        [Header("Timing")]
        public float duration = 0.5f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Audio")]
        public AudioClip impactSound;
        [Range(0f, 1f)] public float soundVolume = 0.5f;
    }
}

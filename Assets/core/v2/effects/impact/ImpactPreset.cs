using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// ScriptableObject preset defining all parameters for a specific impact type.
    /// Supports up to 3 particle layers (sparks, glow, debris).
    /// </summary>
    [CreateAssetMenu(fileName = "ImpactPreset", menuName = "StarfireV2/Impact/Preset")]
    public class ImpactPreset : ScriptableObject
    {
        [Header("Primary Particles (Sparks)")]
        public ImpactParticleLayer primaryLayer = new ImpactParticleLayer();

        [Header("Secondary Particles (Glow)")]
        public bool enableSecondaryLayer = true;
        public ImpactParticleLayer secondaryLayer = new ImpactParticleLayer();

        [Header("Tertiary Particles (Debris)")]
        public bool enableTertiaryLayer;
        public ImpactParticleLayer tertiaryLayer = new ImpactParticleLayer();

        [Header("Impact Light")]
        public bool enableLight = true;
        public ImpactLightConfig lightConfig = new ImpactLightConfig();

        [Header("Audio")]
        public AudioClip impactSound;

        [Range(0f, 1f)]
        public float soundVolume = 0.7f;

        [Header("Screen Shake")]
        public V2ScreenShakeConfig screenShakeConfig;
    }
}

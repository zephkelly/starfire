using UnityEngine;

namespace StarfireV2
{
    public enum ShieldVisibilityMode
    {
        AlwaysSubtle,
        OnlyOnHit,
        Both
    }

    public enum PatternType
    {
        None,
        Fresnel,
        Hexagon,
        Grid,
        Noise,
        Cells
    }

    [CreateAssetMenu(fileName = "ShieldVisualConfig", menuName = "StarfireV2/Shield/Visual Config")]
    public class ShieldVisualConfig : ScriptableObject
    {
        [Header("Visibility Mode")]
        [Tooltip("When to show the shield barrier")]
        public ShieldVisibilityMode visibilityMode = ShieldVisibilityMode.AlwaysSubtle;

        [Tooltip("Base opacity when idle (for AlwaysSubtle/Both modes)")]
        [Range(0f, 1f)] public float idleOpacity = 0.4f;

        [Tooltip("Opacity when hit/active")]
        [Range(0f, 1f)] public float activeOpacity = 1.0f;

        [Tooltip("How long shield stays bright after hit")]
        public float hitFadeDuration = 0.5f;

        [Header("Colors")]
        public Color shieldColor = new Color(0.3f, 0.6f, 1f, 0.7f);
        public Color edgeColor = new Color(0.5f, 0.8f, 1f, 0.8f);
        public Color hitFlashColor = new Color(1f, 1f, 1f, 1f);

        [Tooltip("Color the shield transitions to when damaged (below 50% health)")]
        public Color damagedColor = new Color(1f, 0.3f, 0.1f, 1f);

        [Header("Edge Glow")]
        [Range(0f, 5f)] public float edgeIntensity = 2f;
        [Range(0.5f, 5f)] public float fresnelPower = 2f;
        [Range(0.05f, 0.5f)] public float edgeThickness = 0.12f;
        [Range(0f, 1f)] public float edgeDither = 0f;

        [Header("Energy Pattern")]
        public PatternType patternType = PatternType.Hexagon;
        [Range(0.5f, 10f)] public float patternScale = 3.0f;
        [Range(0f, 2f)] public float patternSpeed = 0.3f;
        [Range(0f, 1f)] public float patternIntensity = 0.5f;

        [Header("Animation")]
        [Range(0f, 1f)] public float pulseAmount = 0.1f;
        [Range(0f, 2f)] public float pulseSpeed = 1f;

        [Header("Impact Ripple")]
        public float rippleSpeed = 3f;
        public float rippleDuration = 0.5f;
        public float rippleWidth = 0.1f;
        [Range(0f, 5f)] public float rippleIntensity = 2f;
        [Range(0f, 1f)] public float rippleOpacity = 0.7f;
        [Range(0f, 1f)] public float rippleSoftness = 0.5f;
        [Range(0f, 1f)] public float ripplePerspective = 0.4f;
        public int maxSimultaneousImpacts = 8;

        [Header("Impact Visibility (for OnlyOnHit/Both modes)")]
        [Range(0.1f, 3f)] public float impactVisibilityRadius = 1.0f;
        [Range(0.1f, 5f)] public float impactVisibilityFalloff = 2.0f;
        [Range(0.2f, 5f)] public float impactVisibilitySpeed = 1.0f;

        [Header("Dome Curvature (3D appearance)")]
        [Range(0f, 1f)] public float domeCurvature = 0.5f;
        [Range(0f, 1f)] public float domeHighlight = 0.3f;
        [Range(0f, 1f)] public float domeShadow = 0.2f;
    }
}

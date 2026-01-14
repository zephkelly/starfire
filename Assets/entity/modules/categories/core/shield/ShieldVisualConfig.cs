using UnityEngine;

namespace Starfire.Entity.Modules.Shield
{
    /// <summary>
    /// Determines when the shield visual is displayed.
    /// </summary>
    public enum ShieldVisibilityMode
    {
        /// <summary>Always visible at low opacity</summary>
        AlwaysSubtle,

        /// <summary>Invisible until struck</summary>
        OnlyOnHit,

        /// <summary>Subtle always + intensify on hit (tactical mode)</summary>
        Both
    }

    /// <summary>
    /// Available energy pattern types for the shield barrier.
    /// </summary>
    public enum PatternType
    {
        None,
        Fresnel,
        Hexagon,
        Grid,
        Noise,
        Cells
    }

    /// <summary>
    /// Configuration for shield visual appearance including energy barrier shader settings,
    /// visibility modes, patterns, and impact effects.
    /// </summary>
    [CreateAssetMenu(fileName = "ShieldVisualConfig", menuName = "Starfire/Shield/Visual Config")]
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
        [Tooltip("Intensity of the edge glow effect")]
        [Range(0f, 5f)] public float edgeIntensity = 2f;

        [Tooltip("Controls how sharp the edge falloff is")]
        [Range(0.5f, 5f)] public float fresnelPower = 2f;

        [Tooltip("Thickness of the visible edge band (0.12 = 12% of radius)")]
        [Range(0.05f, 0.5f)] public float edgeThickness = 0.12f;

        [Tooltip("Dithering amount at the edge-to-transparent transition (0 = smooth, 1 = scattered)")]
        [Range(0f, 1f)] public float edgeDither = 0f;

        [Header("Energy Pattern")]
        public PatternType patternType = PatternType.Hexagon;

        [Tooltip("Scale of the pattern - higher values = more/smaller tiles")]
        [Range(0.5f, 10f)] public float patternScale = 3.0f;

        [Tooltip("Animation speed of the pattern")]
        [Range(0f, 2f)] public float patternSpeed = 0.3f;

        [Tooltip("How visible the pattern is (0 = invisible, 1 = maximum contrast)")]
        [Range(0f, 1f)] public float patternIntensity = 0.5f;

        [Header("Animation")]
        [Tooltip("Amount of pulsing effect")]
        [Range(0f, 1f)] public float pulseAmount = 0.1f;

        [Tooltip("Speed of the pulse animation")]
        [Range(0f, 2f)] public float pulseSpeed = 1f;

        [Header("Impact Ripple")]
        [Tooltip("How fast ripples expand from impact point")]
        public float rippleSpeed = 3f;

        [Tooltip("How long ripples last")]
        public float rippleDuration = 0.5f;

        [Tooltip("Width of the ripple ring")]
        public float rippleWidth = 0.1f;

        [Tooltip("Brightness multiplier for ripples")]
        [Range(0f, 5f)] public float rippleIntensity = 2f;

        [Tooltip("Opacity of the ripple effect (0 = invisible, 1 = full visibility)")]
        [Range(0f, 1f)] public float rippleOpacity = 0.7f;

        [Tooltip("Softness of ripple edges (0 = hard edge, 1 = very soft gradient)")]
        [Range(0f, 1f)] public float rippleSoftness = 0.5f;

        [Tooltip("Perspective bending amount (0 = flat, 1 = strong curve effect)")]
        [Range(0f, 1f)] public float ripplePerspective = 0.4f;

        [Tooltip("Maximum simultaneous impact ripples")]
        public int maxSimultaneousImpacts = 8;

        [Header("Impact Visibility (for OnlyOnHit/Both modes)")]
        [Tooltip("How far the shield becomes visible around an impact point (in local units)")]
        [Range(0.1f, 3f)] public float impactVisibilityRadius = 1.0f;

        [Tooltip("How quickly visibility falls off with distance from impact (higher = sharper falloff)")]
        [Range(0.1f, 5f)] public float impactVisibilityFalloff = 2.0f;

        [Tooltip("How fast the impact visibility fades (1 = normal, 2 = twice as fast, 0.5 = half speed)")]
        [Range(0.2f, 5f)] public float impactVisibilitySpeed = 1.0f;

        [Header("Dome Curvature (3D appearance)")]
        [Tooltip("How curved/domed the shield appears (0 = flat, 1 = full hemisphere)")]
        [Range(0f, 1f)] public float domeCurvature = 0.5f;

        [Tooltip("Intensity of specular highlight on the dome surface")]
        [Range(0f, 1f)] public float domeHighlight = 0.3f;

        [Tooltip("How much the dome darkens on the side away from virtual light")]
        [Range(0f, 1f)] public float domeShadow = 0.2f;
    }
}

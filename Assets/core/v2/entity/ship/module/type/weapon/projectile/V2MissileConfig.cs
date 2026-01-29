using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// ScriptableObject configuration for homing missile behavior.
    /// Defines launch pattern, tracking, weaving, and acceleration parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "MissileConfig", menuName = "StarfireV2/Weapon/MissileConfig")]
    public class V2MissileConfig : ScriptableObject
    {
        [Header("Launch Settings")]
        [Tooltip("Total spread angle for missile volleys in degrees. E.g., 60 means missiles spread from -30 to +30 degrees.")]
        [Range(0f, 180f)]
        public float bloomAngle = 60f;

        [Tooltip("Speed multiplier at launch before acceleration kicks in (0-1 of projectile base speed).")]
        [Range(0.1f, 1f)]
        public float initialSpeedMultiplier = 0.5f;

        [Tooltip("Time in seconds after launch before homing begins. Allows missiles to spread out first.")]
        public float homingDelay = 0.3f;

        [Header("Tracking")]
        [Tooltip("How the missile acquires its target.")]
        public V2MissileTargetingMode targetingMode = V2MissileTargetingMode.AutoAcquire;

        [Tooltip("Maximum turn rate in degrees per second.")]
        public float turnRate = 180f;

        [Tooltip("Range in units to scan for targets when using AutoAcquire mode.")]
        public float acquisitionRange = 50f;

        [Tooltip("Layers that can be targeted by this missile.")]
        public LayerMask targetLayers = ~0;

        [Tooltip("Whether to predict and lead the target based on its velocity.")]
        public bool predictTargetPosition = true;

        [Tooltip("Time in seconds between target re-evaluation. 0 = every frame.")]
        public float targetUpdateInterval = 0.1f;

        [Header("Weaving Pattern")]
        [Tooltip("Enable sinusoidal weaving motion during flight.")]
        public bool enableWeaving = true;

        [Tooltip("Amplitude of the weave perpendicular to travel direction in units.")]
        [Range(0.1f, 5f)]
        public float weaveAmplitude = 1.25f;

        [Tooltip("Frequency of the weave in cycles per second.")]
        [Range(0.5f, 10f)]
        public float weaveFrequency = 2.5f;

        [Tooltip("How quickly the weave amplitude decays as missile approaches target. 0 = no decay, 1 = full decay.")]
        [Range(0f, 1f)]
        public float weaveDecayRate = 0.5f;

        [Tooltip("Random phase offset range for varied missile patterns. Creates visual variety in volleys.")]
        [Range(0f, 6.28f)]
        public float phaseRandomization = 3.14f;

        [Header("Acceleration")]
        [Tooltip("Enable speed increase over missile lifetime.")]
        public bool enableAcceleration = true;

        [Tooltip("Speed multiplier over lifetime curve. X = normalized lifetime (0-1), Y = speed multiplier (relative to base speed).")]
        public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.2f);

        [Header("Survivability")]
        [Tooltip("Maximum health of the missile. Set to 0 for invulnerable missiles.")]
        public float maxHealth = 10f;

        [Tooltip("Whether the missile can be targeted and damaged by enemy defenses.")]
        public bool canBeTargeted = true;

        /// <summary>
        /// Calculates the launch angle offset for a specific missile in a volley.
        /// </summary>
        /// <param name="missileIndex">Zero-based index of this missile in the volley.</param>
        /// <param name="volleyCount">Total number of missiles in the volley.</param>
        /// <returns>Angle offset in degrees from the aim direction.</returns>
        public float GetLaunchAngleOffset(int missileIndex, int volleyCount)
        {
            if (volleyCount <= 1 || bloomAngle <= 0f)
            {
                return 0f;
            }

            // Calculate spread fraction: -1 to +1 across the volley
            float spreadFraction = (missileIndex - (volleyCount - 1) / 2f) / Mathf.Max(1f, (volleyCount - 1) / 2f);

            // Apply to half the bloom angle (since bloomAngle is total spread)
            return spreadFraction * (bloomAngle / 2f);
        }
    }
}

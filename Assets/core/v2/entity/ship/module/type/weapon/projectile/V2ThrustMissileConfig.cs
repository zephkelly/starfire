using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// ScriptableObject configuration for physics-based thrust missiles.
    /// Missiles use a main rear thruster for forward acceleration and
    /// two side steering thrusters that apply lateral forces for turning.
    /// </summary>
    [CreateAssetMenu(fileName = "ThrustMissileConfig", menuName = "StarfireV2/Weapon/ThrustMissileConfig")]
    public class V2ThrustMissileConfig : ScriptableObject
    {
        [Header("Launch Settings")]
        [Tooltip("Total spread angle for missile volleys in degrees.")]
        [Range(0f, 180f)]
        public float bloomAngle = 60f;

        [Tooltip("Speed multiplier at launch before thrusters take over (0-1 of projectile base speed).")]
        [Range(0.1f, 1f)]
        public float initialVelocityMultiplier = 0.5f;

        [Tooltip("Time in seconds after launch before steering thrusters activate.")]
        public float homingDelay = 0.3f;

        [Header("Main Thruster")]
        [Tooltip("Forward thrust force in Newtons.")]
        public float mainThrustForce = 150f;

        [Tooltip("Local position of the main thruster relative to missile center.")]
        public Vector2 mainThrusterLocalPosition = new Vector2(0f, -0.2f);

        [Header("Steering Thrusters")]
        [Tooltip("Maximum lateral steering force in Newtons per thruster.")]
        public float steeringThrustForce = 80f;

        [Tooltip("Local position of the left steering thruster.")]
        public Vector2 leftThrusterLocalPosition = new Vector2(-0.15f, 0f);

        [Tooltip("Local position of the right steering thruster.")]
        public Vector2 rightThrusterLocalPosition = new Vector2(0.15f, 0f);

        [Tooltip("Time in seconds for steering thrusters to ramp to target thrust.")]
        [Range(0f, 0.5f)]
        public float steeringResponseTime = 0.05f;

        [Header("Physics")]
        [Tooltip("Mass of the missile rigidbody.")]
        public float missileMass = 0.5f;

        [Tooltip("Linear drag applied to the rigidbody. Prevents runaway acceleration.")]
        public float missileLinearDrag = 0.5f;

        [Tooltip("Angular drag applied to the rigidbody. Prevents uncontrolled spinning.")]
        public float missileAngularDrag = 2f;

        [Header("Tracking")]
        [Tooltip("How the missile acquires its target.")]
        public V2MissileTargetingMode targetingMode = V2MissileTargetingMode.AutoAcquire;

        [Tooltip("Range in units to scan for targets.")]
        public float acquisitionRange = 50f;

        [Tooltip("Layers that can be targeted.")]
        public LayerMask targetLayers = ~0;

        [Tooltip("Whether to predict and lead the target based on its velocity.")]
        public bool predictTargetPosition = true;

        [Tooltip("Time in seconds between target re-evaluation.")]
        public float targetUpdateInterval = 0.1f;

        [Header("Weaving - Force Based")]
        [Tooltip("Enable sinusoidal lateral force during flight.")]
        public bool enableWeaving = true;

        [Tooltip("Peak lateral force amplitude for weaving in Newtons.")]
        public float weaveForceAmplitude = 30f;

        [Tooltip("Frequency of the weave in cycles per second.")]
        [Range(0.5f, 10f)]
        public float weaveFrequency = 2f;

        [Tooltip("How quickly weave amplitude decays as missile approaches target. 0 = no decay.")]
        [Range(0f, 1f)]
        public float weaveDecayRate = 0.5f;

        [Tooltip("Random phase offset range for varied missile patterns.")]
        [Range(0f, 6.28f)]
        public float phaseRandomization = 3.14f;

        [Header("Survivability")]
        [Tooltip("Maximum health of the missile. 0 = invulnerable.")]
        public float maxHealth = 10f;

        [Tooltip("Whether the missile can be targeted and damaged by enemy defenses.")]
        public bool canBeTargeted = true;

        [Header("Visuals")]
        [Tooltip("Visual configuration for thruster particle effects. Shared across all three thrusters.")]
        public ThrusterVisualConfig thrusterVisualConfig;

        /// <summary>
        /// Calculates the launch angle offset for a specific missile in a volley.
        /// </summary>
        public float GetLaunchAngleOffset(int missileIndex, int volleyCount)
        {
            if (volleyCount <= 1 || bloomAngle <= 0f)
                return 0f;

            float spreadFraction = (missileIndex - (volleyCount - 1) / 2f) / Mathf.Max(1f, (volleyCount - 1) / 2f);
            return spreadFraction * (bloomAngle / 2f);
        }
    }
}

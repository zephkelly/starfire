using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for a single particle layer within an impact effect.
    /// Each impact preset can have up to 3 layers (primary sparks, secondary glow, tertiary debris).
    /// </summary>
    [Serializable]
    public class ImpactParticleLayer
    {
        [Header("Emission")]
        [Tooltip("Number of particles to emit per impact.")]
        [Range(1, 100)]
        public int particleCount = 15;

        [Header("Spread & Direction")]
        [Tooltip("Cone angle for particle spread in degrees.")]
        [Range(0f, 180f)]
        public float spreadAngle = 90f;

        [Tooltip("Bias toward impact normal direction. 0 = hemisphere, 1 = narrow cone.")]
        [Range(0f, 1f)]
        public float directionBias = 0.3f;

        [Header("Velocity")]
        [Tooltip("Min/max particle speed.")]
        public Vector2 speedRange = new Vector2(2f, 8f);

        [Header("Visual")]
        public Gradient colorOverLifetime;

        public AnimationCurve sizeOverLifetime = AnimationCurve.Linear(0, 1, 1, 0.3f);

        public Vector2 startSizeRange = new Vector2(0.1f, 0.3f);

        [Range(0.1f, 3f)]
        public float lifetime = 0.8f;

        [Header("Collision & Deflection")]
        [Tooltip("Enable particle collision for visual deflection off ship hulls.")]
        public bool enableCollision;

        [Tooltip("Bounce damping. 0 = elastic, 1 = absorb.")]
        [Range(0f, 1f)]
        public float bounceDamping = 0.3f;

        [Tooltip("Collision detection quality.")]
        public ParticleSystemCollisionQuality collisionQuality = ParticleSystemCollisionQuality.Low;

        [Header("Rendering")]
        public Sprite particleSprite;

        public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;

        [Range(-100, 100)]
        public int sortingOrder = 5;

        public ImpactParticleLayer()
        {
            colorOverLifetime = new Gradient();
            colorOverLifetime.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.gray, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
        }
    }
}

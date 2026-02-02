using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Manager-level configuration for the ImpactEffectManager.
    /// </summary>
    [CreateAssetMenu(fileName = "ImpactEffectConfig", menuName = "StarfireV2/Impact/Manager Config")]
    public class ImpactEffectConfig : ScriptableObject
    {
        [Header("Particle System Settings")]
        [Tooltip("Maximum particles per ParticleSystem instance.")]
        public int maxParticlesPerSystem = 1000;

        [Tooltip("Layers that particles can collide with for visual deflection.")]
        public LayerMask collisionLayers = ~0;

        [Tooltip("Default material for impact particles. Should be an additive or alpha-blended particle material.")]
        public Material defaultParticleMaterial;

        [Header("Light Pool")]
        [Tooltip("Number of pre-created Light2D objects in the pool.")]
        public int lightPoolSize = 50;
    }
}

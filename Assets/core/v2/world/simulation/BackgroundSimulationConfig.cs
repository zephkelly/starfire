using UnityEngine;

namespace Starfire.Core.V2.World.Simulation
{
    [CreateAssetMenu(fileName = "BackgroundSimulationConfig", menuName = "Starfire/Simulation/Background Config")]
    public class BackgroundSimulationConfig : ScriptableObject
    {
        [Header("Tier 1 - Active Simulation")]
        [Tooltip("Number of chunk rings beyond the loaded area to actively simulate")]
        public int tier1ChunkRadius = 2;

        [Tooltip("Maximum entities in Tier 1 before dropping the farthest")]
        public int tier1MaxEntities = 500;

        [Tooltip("Minimum velocity (sqr magnitude) for an entity to enter simulation on chunk unload")]
        public float minVelocitySqrToSimulate = 0.01f;

        [Header("Tier 2 - Ballistic Prediction")]
        [Tooltip("Chunk distance (Chebyshev) where Tier 1 entities demote to Tier 2")]
        public int tier2StartDistance = 5;

        [Tooltip("Maximum snapshots tracked in Tier 2")]
        public int tier2MaxEntities = 2000;

        [Header("Physics Defaults")]
        [Tooltip("Default mass for asteroids entering simulation")]
        public float defaultAsteroidMass = 100f;

        [Tooltip("Default linear drag for simulated entities")]
        public float defaultDrag = 0f;

        [Header("Collision Detection")]
        [Tooltip("Cell size for spatial hash grid (larger = fewer cells, more comparisons per cell)")]
        public float spatialHashCellSize = 100f;

        [Tooltip("Coefficient of restitution for collisions (1 = perfectly elastic, 0 = inelastic)")]
        [Range(0f, 1f)]
        public float collisionRestitution = 0.8f;

        [Tooltip("Minimum relative velocity to record a collision event")]
        public float minCollisionIntensity = 1f;

        [Header("Destruction")]
        [Tooltip("Structural integrity = mass * this multiplier. Higher = harder to destroy.")]
        public float structuralIntegrityMultiplier = 10f;

        [Tooltip("Minimum impact energy required for destruction to occur")]
        public float minDestructionEnergy = 100f;

        [Tooltip("Whether to spawn debris when entities are destroyed")]
        public bool spawnDebrisOnDestruction = false;

        [Tooltip("Maximum debris entities to spawn per destruction")]
        public int maxDebrisPerDestruction = 3;

        [Header("Event Storage")]
        [Tooltip("Maximum number of events to store in the log")]
        public int maxEvents = 1000;

        [Tooltip("How long to retain events (seconds of game time)")]
        public float eventRetentionSeconds = 600f;
    }
}

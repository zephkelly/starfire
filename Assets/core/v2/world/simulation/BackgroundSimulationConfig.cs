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
    }
}

using UnityEngine;
using Starfire.Core.V2.World.Generation.Generators;

namespace Starfire.Core.V2.World
{
    /// <summary>
    /// Master configuration for the world generation system.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldGenerationConfig", menuName = "Starfire/World/World Generation Config")]
    public class WorldGenerationConfig : ScriptableObject
    {
        [Header("World Settings")]
        [Tooltip("Size of each chunk in world units")]
        [Min(10f)]
        public float chunkSize = 500f;

        [Tooltip("Master seed for world generation (0 = random on start)")]
        public float worldSeed = 0f;

        [Header("Floating Origin")]
        [Tooltip("Distance from origin at which to reset (shift world back to origin)")]
        [Min(100f)]
        public float floatingOriginLimit = 2560f;

        [Header("Chunk Loading")]
        [Tooltip("Radius in chunks to keep loaded around camera")]
        [Range(1, 10)]
        public int loadRadius = 3;

        [Tooltip("Radius in chunks at which to unload (should be > loadRadius)")]
        [Range(2, 15)]
        public int unloadRadius = 5;

        [Tooltip("Maximum chunks to process (load/unload) per frame")]
        [Range(1, 10)]
        public int chunksPerFrame = 2;

        [Tooltip("Maximum loaded chunks (memory limit)")]
        [Min(10)]
        public int maxLoadedChunks = 100;

        [Header("Generators")]
        [Tooltip("Nebula generation configuration")]
        public NebulaGenerationConfig nebulaConfig;

        // Future generator configs
        // public AsteroidGenerationConfig asteroidConfig;
        // public POIGenerationConfig poiConfig;

        [Header("Debug")]
        [Tooltip("Enable debug visualization in scene view")]
        public bool enableDebugGizmos = false;

        [Tooltip("Color for chunk boundaries")]
        public Color chunkBoundsColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Tooltip("Color for loaded chunks")]
        public Color loadedChunkColor = new Color(0f, 1f, 0f, 0.2f);

        [Tooltip("Color for loading chunks")]
        public Color loadingChunkColor = new Color(1f, 1f, 0f, 0.2f);

        [Tooltip("Log chunk events to console")]
        public bool logChunkEvents = false;

        private void OnValidate()
        {
            // Ensure unload radius is greater than load radius
            if (unloadRadius <= loadRadius)
            {
                unloadRadius = loadRadius + 2;
            }
        }
    }
}

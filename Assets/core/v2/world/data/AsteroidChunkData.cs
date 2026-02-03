using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Core.V2.World.Data
{
    public enum AsteroidSource
    {
        Belt,
        PlanetaryRing,
        Scatter
    }

    public struct AsteroidDefinition
    {
        /// <summary>Position relative to chunk center.</summary>
        public Vector2 LocalPosition;
        public float Size;
        public float Rotation;
        public int Variant;
        public float Seed;
        public AsteroidSource Source;
    }

    /// <summary>
    /// Chunk data containing procedurally placed asteroids.
    /// </summary>
    public class AsteroidChunkData : ChunkData
    {
        public List<AsteroidDefinition> Asteroids = new List<AsteroidDefinition>();

        /// <summary>Active pooled instances: (instance, source prefab) for correct pool returns.</summary>
        public List<(GameObject instance, GameObject prefab)> RuntimeObjects = new List<(GameObject, GameObject)>();

        public override void OnUnload()
        {
            // Consumer handles pool returns; just clear the tracking list.
            RuntimeObjects.Clear();
        }
    }
}

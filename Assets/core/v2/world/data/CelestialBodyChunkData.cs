using System.Collections.Generic;
using UnityEngine;
using StarfireV2;

namespace Starfire.Core.V2.World.Data
{
    /// <summary>
    /// Chunk data containing celestial bodies whose influence overlaps this chunk.
    /// </summary>
    public class CelestialBodyChunkData : ChunkData
    {
        public List<CelestialBodyDefinition> Bodies = new List<CelestialBodyDefinition>();

        /// <summary>Active pooled instances: (instance, source prefab) for correct pool returns.</summary>
        public List<(GameObject instance, GameObject prefab)> RuntimeObjects = new List<(GameObject, GameObject)>();

        public override void OnUnload()
        {
            // Consumer handles pool returns; just clear the tracking list.
            RuntimeObjects.Clear();
        }
    }

    public struct CelestialBodyDefinition
    {
        /// <summary>Position relative to chunk center.</summary>
        public Vector2 LocalPosition;

        /// <summary>Full body info from the fabric layer.</summary>
        public CelestialBodyInfo Info;
    }
}

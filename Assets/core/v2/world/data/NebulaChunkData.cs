using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Background.Regions;

namespace Starfire.Core.V2.World.Data
{
    /// <summary>
    /// Chunk data containing nebula region definitions and runtime references.
    /// </summary>
    public class NebulaChunkData : ChunkData
    {
        /// <summary>
        /// Nebula region definitions generated for this chunk.
        /// These are serializable data, not runtime objects.
        /// </summary>
        public List<NebulaRegionDefinition> Regions { get; } = new List<NebulaRegionDefinition>();

        /// <summary>
        /// Runtime references to created NebulaRegion instances.
        /// Populated by NebulaRegionConsumer when chunk is loaded.
        /// </summary>
        public List<NebulaRegion> RuntimeRegions { get; } = new List<NebulaRegion>();

        public override void OnUnload()
        {
            // Clear runtime references (actual cleanup done by consumer)
            RuntimeRegions.Clear();
        }
    }

    /// <summary>
    /// Serializable definition of a nebula region to be created.
    /// Contains position relative to chunk center for floating origin support.
    /// </summary>
    [Serializable]
    public class NebulaRegionDefinition
    {
        /// <summary>
        /// Position relative to the chunk center (not absolute world position).
        /// This ensures the definition remains valid across origin shifts.
        /// </summary>
        public Vector2 LocalPosition;

        /// <summary>
        /// Radius of the nebula region in world units.
        /// </summary>
        public float Radius;

        /// <summary>
        /// Configuration defining the nebula's appearance.
        /// </summary>
        public NebulaRegionConfig Config;

        /// <summary>
        /// Unique seed for this nebula's procedural patterns.
        /// </summary>
        public float Seed;

        public NebulaRegionDefinition() { }

        public NebulaRegionDefinition(Vector2 localPosition, float radius, NebulaRegionConfig config, float seed)
        {
            LocalPosition = localPosition;
            Radius = radius;
            Config = config;
            Seed = seed;
        }
    }
}

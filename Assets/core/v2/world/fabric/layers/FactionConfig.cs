using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for faction territory generation.
    /// </summary>
    [CreateAssetMenu(fileName = "FactionConfig", menuName = "Starfire/World Fabric/Faction Config")]
    public class FactionConfig : ScriptableObject
    {
        [Header("Enable")]
        public bool enabled = true;

        [Header("Territory Size")]
        [Tooltip("Base size of Voronoi cells for territory borders (smaller = more border detail)")]
        public float majorFactionCellSize = 100000f;
        [Tooltip("Jitter for cell centers (0 = grid, 1 = random)")]
        [Range(0f, 1f)]
        public float cellJitter = 0.8f;

        [Header("Border Settings")]
        [Tooltip("Width of contested border zones. Scale with cell size - try 1-5% of majorFactionCellSize.")]
        public float contestedBorderWidth = 5000f;
        [Tooltip("Distance at which control starts to fade. Scale with cell size - try 5-10% of majorFactionCellSize.")]
        public float controlFadeDistance = 10000f;

        [Header("Border Warping (Natural Look)")]
        [Tooltip("How far borders can deviate from geometric centers (0 = straight hexagonal lines). Scale with cell size - try 10-30% of majorFactionCellSize.")]
        public float borderWarpStrength = 15000f;
        [Tooltip("Scale of warp pattern (smaller = gentler curves, larger = more frequent). Try 1/cellSize for base frequency.")]
        public float borderWarpScale = 0.00001f;
        [Tooltip("Complexity of warp noise (more octaves = more detail)")]
        [Range(1, 6)]
        public int borderWarpOctaves = 3;

        [Header("Border Edge Noise (Fine Detail)")]
        [Tooltip("Strength of high-frequency edge noise for jagged borders. Scale with cell size - try 1-5% of majorFactionCellSize.")]
        public float edgeNoiseStrength = 2000f;
        [Tooltip("Scale of edge noise (smaller = finer detail). Should be higher frequency than warp scale.")]
        public float edgeNoiseScale = 0.0001f;

        [Header("Empire Clustering")]
        [Tooltip("Enable hierarchical territories for larger contiguous faction regions")]
        public bool enableEmpireClustering = true;
        [Tooltip("Size of empire super-cells (should be 3-6x territory cell size)")]
        public float empireCellSize = 600000f;
        [Tooltip("Jitter for empire cell centers")]
        [Range(0f, 1f)]
        public float empireJitter = 0.5f;
        [Tooltip("Warp strength for empire cell borders. Scale with empireCellSize - try 10-20% of empireCellSize.")]
        public float empireWarpStrength = 60000f;

        [Header("Major Factions")]
        public List<FactionDefinition> majorFactions = new();

        [Header("Minor Factions")]
        [Tooltip("Enable procedural minor factions")]
        public bool enableMinorFactions = true;
        [Tooltip("Chance that a Voronoi cell becomes a minor faction instead of major")]
        [Range(0f, 0.5f)]
        public float minorFactionChance = 0.15f;
    }

    [Serializable]
    public class FactionDefinition
    {
        public string factionId;
        public string displayName;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;
        [Range(0f, 2f)]
        public float aggressionLevel = 1f;
        [Range(0f, 2f)]
        public float territoryWeight = 1f; // Higher = more likely to control cells
    }

    [Serializable]
    public struct FactionId : IEquatable<FactionId>
    {
        public string Value;

        public static FactionId Neutral => new() { Value = "neutral" };
        public static FactionId Minor(int cellId) => new() { Value = $"minor_{cellId}" };

        public bool IsNeutral => string.IsNullOrEmpty(Value) || Value == "neutral";
        public bool IsMinor => Value?.StartsWith("minor_") ?? false;

        public bool Equals(FactionId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is FactionId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? "neutral";

        public static bool operator ==(FactionId a, FactionId b) => a.Value == b.Value;
        public static bool operator !=(FactionId a, FactionId b) => a.Value != b.Value;
    }
}

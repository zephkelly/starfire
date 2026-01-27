using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for faction territory generation using MetaballFields.
    /// Each faction gets its own implicit blob field; territory = highest density wins.
    /// </summary>
    [CreateAssetMenu(fileName = "FactionConfig", menuName = "Starfire/World Fabric/Faction Config")]
    public class FactionConfig : ScriptableObject
    {
        [Header("Enable")]
        public bool enabled = true;

        [Header("Territory Metaball Settings")]
        [Tooltip("Distance between blob centers (controls region spacing)")]
        public float factionBlobSpacing = 300000f;
        [Tooltip("Minimum blob radius")]
        public float factionBlobRadiusMin = 100000f;
        [Tooltip("Maximum blob radius")]
        public float factionBlobRadiusMax = 400000f;
        [Tooltip("Contribution strength per blob")]
        [Range(0.1f, 2f)]
        public float factionBlobStrength = 1.0f;
        [Tooltip("Falloff curve power (higher = sharper edges)")]
        [Range(1f, 5f)]
        public float factionFalloffPower = 2.0f;
        [Tooltip("Density below which territory is unclaimed")]
        [Range(0f, 1f)]
        public float factionActivationThreshold = 0.3f;

        [Header("Border Detection")]
        [Tooltip("If winner density minus runner-up density is below this, the zone is contested")]
        public float contestedDensityGap = 0.15f;
        [Tooltip("Density range over which control strength fades from 0 to 1 (above threshold)")]
        public float controlFadeDensityRange = 0.3f;

        [Header("Major Factions")]
        public List<FactionDefinition> majorFactions = new();

        [Header("Unclaimed Space")]
        [Tooltip("Chance that a blob spawns as neutral rather than faction-owned")]
        [Range(0f, 1f)]
        public float neutralCellChance = 0.65f;
        [Tooltip("Extra neutral suppression in void-dominated regions (multiplied by void factor)")]
        [Range(0f, 0.5f)]
        public float voidNeutralBonus = 0.25f;
        [Tooltip("Extra neutral suppression in anomaly-dominated regions")]
        [Range(0f, 0.5f)]
        public float anomalyNeutralBonus = 0.3f;

        [Header("Resource Border Influence")]
        [Tooltip("How much resource density boosts all faction densities (expanding territory toward resources)")]
        [Range(0f, 0.5f)]
        public float resourceDensityBoost = 0.3f;
        [Tooltip("Reduction to neutral chance per unit of OverallResourceValue")]
        [Range(0f, 1f)]
        public float resourceClaimingBoost = 0.4f;

        [Header("Minor Factions")]
        [Tooltip("Enable procedural minor factions")]
        public bool enableMinorFactions = true;
        [Tooltip("Chance that a blob becomes a minor faction instead of major")]
        [Range(0f, 0.5f)]
        public float minorFactionChance = 0.15f;

        /// <summary>
        /// Build a MetaballFieldConfig from the faction territory settings.
        /// </summary>
        internal MetaballFieldConfig GetFactionFieldConfig()
        {
            return new MetaballFieldConfig
            {
                blobSpacing = factionBlobSpacing,
                blobRadiusMin = factionBlobRadiusMin,
                blobRadiusMax = factionBlobRadiusMax,
                blobStrength = factionBlobStrength,
                falloffPower = factionFalloffPower,
                threshold = factionActivationThreshold,
            };
        }
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
        public float territoryWeight = 1f;
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

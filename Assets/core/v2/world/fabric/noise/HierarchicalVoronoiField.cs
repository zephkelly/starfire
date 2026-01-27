using System;
using UnityEngine;
using Starfire.Core.V2.World;

namespace StarfireV2
{
    /// <summary>
    /// Two-tier hierarchical Voronoi field for faction territory generation.
    /// Empire tier: Large cells that determine faction ownership (creates contiguous territories)
    /// Territory tier: Smaller cells that provide border detail and variation
    /// Supports faceted (low-poly) borders and resource-biased distance.
    /// </summary>
    [Serializable]
    public class HierarchicalVoronoiField
    {
        private readonly VoronoiNoiseField _empireTier;
        private readonly VoronoiNoiseField _territoryTier;

        public HierarchicalVoronoiField(
            float empireCellSize,
            float empireJitter,
            float empireWarpStrength,
            float territoryCellSize,
            float territoryJitter,
            float territoryWarpStrength,
            float warpScale,
            int warpOctaves,
            float edgeNoiseStrength,
            float edgeNoiseScale)
            : this(empireCellSize, empireJitter, empireWarpStrength,
                   territoryCellSize, territoryJitter, territoryWarpStrength,
                   warpScale, warpOctaves, edgeNoiseStrength, edgeNoiseScale,
                   false, 8)
        {
        }

        public HierarchicalVoronoiField(
            float empireCellSize,
            float empireJitter,
            float empireWarpStrength,
            float territoryCellSize,
            float territoryJitter,
            float territoryWarpStrength,
            float warpScale,
            int warpOctaves,
            float edgeNoiseStrength,
            float edgeNoiseScale,
            bool useFacetedBorders,
            int facetAngularSteps)
        {
            // Empire tier: large cells for faction assignment
            // Uses gentler warping to create smooth large-scale borders
            _empireTier = new VoronoiNoiseField(
                empireCellSize,
                empireJitter,
                empireWarpStrength,
                warpScale * 0.5f,  // Lower frequency for empire borders
                warpOctaves,
                0f, 0f,  // No edge noise on empire tier
                useFacetedBorders,
                facetAngularSteps
            );

            // Territory tier: smaller cells for border detail
            _territoryTier = new VoronoiNoiseField(
                territoryCellSize,
                territoryJitter,
                territoryWarpStrength,
                warpScale,
                warpOctaves,
                edgeNoiseStrength,
                edgeNoiseScale,
                useFacetedBorders,
                facetAngularSteps
            );
        }

        /// <summary>
        /// Sample the hierarchical Voronoi field.
        /// Returns the empire cell ID (for faction assignment) and territory-level border detail.
        /// </summary>
        public HierarchicalVoronoiResult SampleDetailed(Vector2D position, float seed)
        {
            return SampleDetailedInternal(position, seed, 0f);
        }

        /// <summary>
        /// Sample with resource bias for border attraction.
        /// Positive resourceBias causes borders to bulge toward resource-rich areas.
        /// </summary>
        public HierarchicalVoronoiResult SampleDetailed(Vector2D position, float seed, float resourceBias)
        {
            return SampleDetailedInternal(position, seed, resourceBias);
        }

        private HierarchicalVoronoiResult SampleDetailedInternal(Vector2D position, float seed, float resourceBias)
        {
            // Empire tier determines faction ownership
            var empireResult = _empireTier.SampleDetailed(position, seed, resourceBias);

            // Territory tier provides border detail
            // Use a different seed offset to decorrelate the two tiers
            var territoryResult = _territoryTier.SampleDetailed(position, seed + 5000f, resourceBias);

            // The final distance-to-border is primarily from the territory tier
            // but we also consider the empire border for large-scale transitions
            float combinedDistanceToEdge = Mathf.Min(
                territoryResult.DistanceToEdge,
                empireResult.DistanceToEdge * 0.5f  // Empire borders are "softer"
            );

            return new HierarchicalVoronoiResult
            {
                EmpireCellId = empireResult.CellId,
                TerritoryCellId = territoryResult.CellId,
                DistanceToEdge = combinedDistanceToEdge,
                DistanceToEmpireBorder = empireResult.DistanceToEdge,
                DistanceToTerritoryBorder = territoryResult.DistanceToEdge,
                EmpireCellCenter = empireResult.CellCenter,
                TerritoryCellCenter = territoryResult.CellCenter
            };
        }

        /// <summary>
        /// Get just the empire cell ID at a position (for fast faction lookups).
        /// </summary>
        public int GetEmpireCellIdAt(Vector2D position, float seed)
        {
            return _empireTier.GetCellIdAt(position, seed);
        }

        /// <summary>
        /// Get both empire and territory cell IDs at a position.
        /// </summary>
        public (int empireId, int territoryId) GetCellIdsAt(Vector2D position, float seed)
        {
            int empireId = _empireTier.GetCellIdAt(position, seed);
            int territoryId = _territoryTier.GetCellIdAt(position, seed + 5000f);
            return (empireId, territoryId);
        }
    }

    public struct HierarchicalVoronoiResult
    {
        /// <summary>
        /// The empire cell ID - use this for faction assignment.
        /// All positions within the same empire cell belong to the same faction.
        /// </summary>
        public int EmpireCellId;

        /// <summary>
        /// The territory cell ID - provides local variation within an empire.
        /// </summary>
        public int TerritoryCellId;

        /// <summary>
        /// Combined distance to the nearest border (minimum of empire and territory).
        /// </summary>
        public float DistanceToEdge;

        /// <summary>
        /// Distance to the nearest empire border (large-scale faction boundary).
        /// </summary>
        public float DistanceToEmpireBorder;

        /// <summary>
        /// Distance to the nearest territory border (local detail boundary).
        /// </summary>
        public float DistanceToTerritoryBorder;

        public Vector2D EmpireCellCenter;
        public Vector2D TerritoryCellCenter;
    }
}

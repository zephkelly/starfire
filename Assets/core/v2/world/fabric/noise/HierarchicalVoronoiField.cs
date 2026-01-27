using System;
using UnityEngine;
using Starfire.Core.V2.World;

namespace StarfireV2
{
    /// <summary>
    /// Two-tier hierarchical Voronoi field for faction territory generation.
    /// Empire tier: Large cells that determine faction ownership (creates contiguous territories)
    /// Territory tier: Smaller cells that provide border detail and variation
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
        {
            // Empire tier: large cells for faction assignment
            // Uses gentler warping to create smooth large-scale borders
            _empireTier = new VoronoiNoiseField(
                empireCellSize,
                empireJitter,
                empireWarpStrength,
                warpScale * 0.5f,  // Lower frequency for empire borders
                warpOctaves
            );

            // Territory tier: smaller cells for border detail
            // Uses full warping + edge noise for organic fine-grained borders
            _territoryTier = new VoronoiNoiseField(
                territoryCellSize,
                territoryJitter,
                territoryWarpStrength,
                warpScale,
                warpOctaves,
                edgeNoiseStrength,
                edgeNoiseScale
            );
        }

        /// <summary>
        /// Sample the hierarchical Voronoi field.
        /// Returns the empire cell ID (for faction assignment) and territory-level border detail.
        /// </summary>
        public HierarchicalVoronoiResult SampleDetailed(Vector2D position, float seed)
        {
            // Empire tier determines faction ownership
            var empireResult = _empireTier.SampleDetailed(position, seed);

            // Territory tier provides border detail
            // Use a different seed offset to decorrelate the two tiers
            var territoryResult = _territoryTier.SampleDetailed(position, seed + 5000f);

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

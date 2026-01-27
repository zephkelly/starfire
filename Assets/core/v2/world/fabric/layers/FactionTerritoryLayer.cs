using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

namespace StarfireV2
{
    public class FactionTerritoryLayer : IWorldLayer
    {
        public string LayerId => "FactionTerritory";
        public int Priority => 10;
        public bool IsEnabled => _config != null && _config.enabled;
        public Type DataType => typeof(FactionTerritoryData);
        public IReadOnlyList<Type> Dependencies => new[] { typeof(SpaceZoneLayerData) };

        private readonly FactionConfig _config;
        private readonly VoronoiNoiseField _legacyVoronoi;
        private readonly HierarchicalVoronoiField _hierarchicalVoronoi;
        private readonly bool _useHierarchical;

        public FactionTerritoryLayer(FactionConfig config)
        {
            _config = config;
            _useHierarchical = config.enableEmpireClustering;

            if (_useHierarchical)
            {
                _hierarchicalVoronoi = new HierarchicalVoronoiField(
                    empireCellSize: config.empireCellSize,
                    empireJitter: config.empireJitter,
                    empireWarpStrength: config.empireWarpStrength,
                    territoryCellSize: config.majorFactionCellSize,
                    territoryJitter: config.cellJitter,
                    territoryWarpStrength: config.borderWarpStrength,
                    warpScale: config.borderWarpScale,
                    warpOctaves: config.borderWarpOctaves,
                    edgeNoiseStrength: config.edgeNoiseStrength,
                    edgeNoiseScale: config.edgeNoiseScale
                );
            }
            else
            {
                // Legacy mode: single-tier Voronoi with edge noise
                _legacyVoronoi = new VoronoiNoiseField(
                    config.majorFactionCellSize,
                    config.cellJitter,
                    config.borderWarpStrength,
                    config.borderWarpScale,
                    config.borderWarpOctaves,
                    config.edgeNoiseStrength,
                    config.edgeNoiseScale
                );
            }
        }

        public WorldLayerData Generate(Chunk chunk, WorldFabricContext context)
        {
            var data = new FactionTerritoryData();
            var zoneData = context.GetLayerData<SpaceZoneLayerData>();

            Vector2D center = chunk.Coord.ToAbsoluteCenter(context.ChunkSize);

            if (_useHierarchical)
            {
                var result = _hierarchicalVoronoi.SampleDetailed(center, context.WorldSeed);

                // Use empire cell for faction assignment (creates contiguous territories)
                data.ControllingFaction = MapCellToFaction(result.EmpireCellId, context.WorldSeed);
                data.ControlStrength = CalculateControlStrength(result.DistanceToEdge, zoneData);
                data.DistanceToBorder = result.DistanceToEdge;
                data.IsContestedZone = result.DistanceToEdge < _config.contestedBorderWidth;
                data.VoronoiCellId = result.TerritoryCellId;
                data.EmpireCellId = result.EmpireCellId;
            }
            else
            {
                var voronoiResult = _legacyVoronoi.SampleDetailed(center, context.WorldSeed);

                data.ControllingFaction = MapCellToFaction(voronoiResult.CellId, context.WorldSeed);
                data.ControlStrength = CalculateControlStrength(voronoiResult.DistanceToEdge, zoneData);
                data.DistanceToBorder = voronoiResult.DistanceToEdge;
                data.IsContestedZone = voronoiResult.DistanceToEdge < _config.contestedBorderWidth;
                data.VoronoiCellId = voronoiResult.CellId;
                data.EmpireCellId = voronoiResult.CellId; // Same as territory in legacy mode
            }

            return data;
        }

        public IWorldLayerQuery CreateQuery()
        {
            if (_useHierarchical)
                return new HierarchicalFactionTerritoryQuery(_config, _hierarchicalVoronoi);
            else
                return new FactionTerritoryQuery(_config, _legacyVoronoi);
        }

        public void OnChunkUnloading(Chunk chunk) { }

        private FactionId MapCellToFaction(int cellId, float worldSeed)
        {
            if (_config.majorFactions == null || _config.majorFactions.Count == 0)
                return FactionId.Neutral;

            var rng = new System.Random(cellId ^ (int)(worldSeed * 1000));

            // Check for minor faction
            if (_config.enableMinorFactions && rng.NextDouble() < _config.minorFactionChance)
                return FactionId.Minor(cellId);

            // Weight-based selection of major faction
            float totalWeight = 0f;
            foreach (var faction in _config.majorFactions)
                totalWeight += faction.territoryWeight;

            float roll = (float)rng.NextDouble() * totalWeight;
            float cumulative = 0f;

            foreach (var faction in _config.majorFactions)
            {
                cumulative += faction.territoryWeight;
                if (roll <= cumulative)
                    return new FactionId { Value = faction.factionId };
            }

            return new FactionId { Value = _config.majorFactions[0].factionId };
        }

        private float CalculateControlStrength(float distanceToEdge, SpaceZoneLayerData zoneData)
        {
            float baseControl = Mathf.InverseLerp(0, _config.controlFadeDistance, distanceToEdge);

            if (zoneData != null)
            {
                baseControl *= zoneData.PrimaryZoneType switch
                {
                    SpaceZoneType.NebulaDense => 0.5f,
                    SpaceZoneType.Anomaly => 0.3f,
                    SpaceZoneType.DeepVoid => 0.7f,
                    _ => 1f
                };
            }

            return Mathf.Clamp01(baseControl);
        }
    }

    public class FactionTerritoryData : WorldLayerData
    {
        public override string LayerId => "FactionTerritory";

        public FactionId ControllingFaction;
        public float ControlStrength;
        public float DistanceToBorder;
        public bool IsContestedZone;
        public int VoronoiCellId;
        public int EmpireCellId;
    }

    public struct FactionTerritoryInfo
    {
        public FactionId ControllingFaction;
        public float ControlStrength;
        public bool IsContestedZone;
        public float DistanceToBorder;
        public int EmpireCellId;

        public static FactionTerritoryInfo Neutral => new()
        {
            ControllingFaction = FactionId.Neutral,
            ControlStrength = 0f,
            IsContestedZone = false,
            DistanceToBorder = float.MaxValue,
            EmpireCellId = 0
        };
    }

    /// <summary>
    /// Query for legacy single-tier Voronoi territories.
    /// </summary>
    public class FactionTerritoryQuery : IWorldLayerQuery<FactionTerritoryInfo>
    {
        private readonly FactionConfig _config;
        private readonly VoronoiNoiseField _voronoi;

        public FactionTerritoryQuery(FactionConfig config, VoronoiNoiseField voronoi)
        {
            _config = config;
            _voronoi = voronoi;
        }

        public FactionTerritoryInfo QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            var voronoiResult = _voronoi.SampleDetailed(absolutePosition, worldSeed);

            return new FactionTerritoryInfo
            {
                ControllingFaction = MapCellToFaction(voronoiResult.CellId, worldSeed),
                ControlStrength = Mathf.InverseLerp(0, _config.controlFadeDistance, voronoiResult.DistanceToEdge),
                IsContestedZone = voronoiResult.DistanceToEdge < _config.contestedBorderWidth,
                DistanceToBorder = voronoiResult.DistanceToEdge,
                EmpireCellId = voronoiResult.CellId
            };
        }

        object IWorldLayerQuery.QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            return QueryAt(absolutePosition, worldSeed);
        }

        private FactionId MapCellToFaction(int cellId, float worldSeed)
        {
            if (_config.majorFactions == null || _config.majorFactions.Count == 0)
                return FactionId.Neutral;

            var rng = new System.Random(cellId ^ (int)(worldSeed * 1000));

            if (_config.enableMinorFactions && rng.NextDouble() < _config.minorFactionChance)
                return FactionId.Minor(cellId);

            float totalWeight = 0f;
            foreach (var faction in _config.majorFactions)
                totalWeight += faction.territoryWeight;

            float roll = (float)rng.NextDouble() * totalWeight;
            float cumulative = 0f;

            foreach (var faction in _config.majorFactions)
            {
                cumulative += faction.territoryWeight;
                if (roll <= cumulative)
                    return new FactionId { Value = faction.factionId };
            }

            return new FactionId { Value = _config.majorFactions[0].factionId };
        }
    }

    /// <summary>
    /// Query for hierarchical two-tier Voronoi territories.
    /// Uses empire cells for faction assignment and territory cells for border detail.
    /// </summary>
    public class HierarchicalFactionTerritoryQuery : IWorldLayerQuery<FactionTerritoryInfo>
    {
        private readonly FactionConfig _config;
        private readonly HierarchicalVoronoiField _voronoi;

        public HierarchicalFactionTerritoryQuery(FactionConfig config, HierarchicalVoronoiField voronoi)
        {
            _config = config;
            _voronoi = voronoi;
        }

        public FactionTerritoryInfo QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            var result = _voronoi.SampleDetailed(absolutePosition, worldSeed);

            return new FactionTerritoryInfo
            {
                ControllingFaction = MapCellToFaction(result.EmpireCellId, worldSeed),
                ControlStrength = Mathf.InverseLerp(0, _config.controlFadeDistance, result.DistanceToEdge),
                IsContestedZone = result.DistanceToEdge < _config.contestedBorderWidth,
                DistanceToBorder = result.DistanceToEdge,
                EmpireCellId = result.EmpireCellId
            };
        }

        object IWorldLayerQuery.QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            return QueryAt(absolutePosition, worldSeed);
        }

        private FactionId MapCellToFaction(int cellId, float worldSeed)
        {
            if (_config.majorFactions == null || _config.majorFactions.Count == 0)
                return FactionId.Neutral;

            var rng = new System.Random(cellId ^ (int)(worldSeed * 1000));

            if (_config.enableMinorFactions && rng.NextDouble() < _config.minorFactionChance)
                return FactionId.Minor(cellId);

            float totalWeight = 0f;
            foreach (var faction in _config.majorFactions)
                totalWeight += faction.territoryWeight;

            float roll = (float)rng.NextDouble() * totalWeight;
            float cumulative = 0f;

            foreach (var faction in _config.majorFactions)
            {
                cumulative += faction.territoryWeight;
                if (roll <= cumulative)
                    return new FactionId { Value = faction.factionId };
            }

            return new FactionId { Value = _config.majorFactions[0].factionId };
        }
    }
}

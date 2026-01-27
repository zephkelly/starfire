using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

namespace StarfireV2
{
    // =========================================================================
    // FactionTerritoryLayer — MetaballField-based faction territories
    //
    // Each major faction gets its own MetaballField with a unique seed offset.
    // Territory ownership = highest density above threshold wins.
    // Unclaimed space = no faction exceeds threshold.
    // =========================================================================

    public class FactionTerritoryLayer : IWorldLayer
    {
        public string LayerId => "FactionTerritory";
        public int Priority => 10;
        public bool IsEnabled => _config != null && _config.enabled;
        public Type DataType => typeof(FactionTerritoryData);
        public IReadOnlyList<Type> Dependencies => _dependencies;

        private static readonly Type[] _dependencies = { typeof(SpaceZoneLayerData), typeof(ResourceLayerData) };

        private readonly FactionConfig _config;

        // One MetaballField per major faction + one for minor factions
        private readonly MetaballField[] _factionFields;
        private readonly MetaballField _minorField;
        private readonly int _majorFactionCount;

        public FactionTerritoryLayer(FactionConfig config)
        {
            _config = config;
            _majorFactionCount = config.majorFactions?.Count ?? 0;

            var fieldConfig = config.GetFactionFieldConfig();

            // Each major faction gets its own field with a unique seed offset
            _factionFields = new MetaballField[_majorFactionCount];
            for (int i = 0; i < _majorFactionCount; i++)
            {
                _factionFields[i] = new MetaballField(fieldConfig, seedOffset: (i + 1) * 10000f);
            }

            // Minor factions share a single field with a different offset
            if (config.enableMinorFactions)
            {
                _minorField = new MetaballField(fieldConfig, seedOffset: 99000f);
            }
        }

        public WorldLayerData Generate(Chunk chunk, WorldFabricContext context)
        {
            var zoneData = context.GetLayerData<SpaceZoneLayerData>();
            var resourceData = context.GetLayerData<ResourceLayerData>();

            Vector2D center = chunk.Coord.ToAbsoluteCenter(context.ChunkSize);
            var result = SampleTerritory(center, context.WorldSeed, zoneData, resourceData);

            return new FactionTerritoryData
            {
                ControllingFaction = result.ControllingFaction,
                ControlStrength = result.ControlStrength,
                DistanceToBorder = result.DistanceToBorder,
                IsContestedZone = result.IsContestedZone,
                WinnerDensity = result.WinnerDensity,
                RunnerUpDensity = result.RunnerUpDensity,
            };
        }

        public IWorldLayerQuery CreateQuery()
        {
            return new MetaballFactionTerritoryQuery(_config, this);
        }

        public void OnChunkUnloading(Chunk chunk) { }

        // =============================================================
        // Core sampling — used by both Generate and Query
        // =============================================================

        internal FactionTerritoryInfo SampleTerritory(
            Vector2D position, float worldSeed,
            SpaceZoneLayerData zoneData, ResourceLayerData resourceData)
        {
            // Zone modifier: weaken faction presence in voids/anomalies
            float zoneMod = 1f;
            if (zoneData != null)
            {
                zoneMod *= (1f - zoneData.FabricSample.VoidFactor * _config.voidNeutralBonus);
                zoneMod *= (1f - zoneData.FabricSample.AnomalyStrength * _config.anomalyNeutralBonus);
            }

            // Resource boost: expand territory toward resources
            float resourceBoost = (resourceData?.FabricSample.OverallResourceValue ?? 0f)
                                  * _config.resourceDensityBoost;

            float threshold = _config.factionActivationThreshold;

            // Sample all major faction fields
            float winnerDensity = 0f;
            float runnerUpDensity = 0f;
            int winnerIndex = -1;

            for (int i = 0; i < _majorFactionCount; i++)
            {
                float raw = _factionFields[i].SampleDetailed(position, worldSeed).NormalizedDensity;
                float density = raw * zoneMod + resourceBoost;

                if (density > winnerDensity)
                {
                    runnerUpDensity = winnerDensity;
                    winnerDensity = density;
                    winnerIndex = i;
                }
                else if (density > runnerUpDensity)
                {
                    runnerUpDensity = density;
                }
            }

            // Check minor faction field
            float minorDensity = 0f;
            int minorBlobHash = 0;
            if (_config.enableMinorFactions && _minorField != null)
            {
                var minorResult = _minorField.SampleDetailed(position, worldSeed);
                minorDensity = minorResult.NormalizedDensity * zoneMod + resourceBoost;

                // Use raw density hash for minor faction ID
                minorBlobHash = (int)(minorResult.RawDensity * 100000f);

                if (minorDensity > winnerDensity)
                {
                    runnerUpDensity = winnerDensity;
                    winnerDensity = minorDensity;
                    winnerIndex = -2; // sentinel for minor faction
                }
                else if (minorDensity > runnerUpDensity)
                {
                    runnerUpDensity = minorDensity;
                }
            }

            // Determine controlling faction
            FactionId controllingFaction;
            if (winnerDensity < threshold)
            {
                controllingFaction = FactionId.Neutral;
            }
            else if (winnerIndex == -2)
            {
                controllingFaction = FactionId.Minor(minorBlobHash);
            }
            else if (winnerIndex >= 0 && winnerIndex < _majorFactionCount)
            {
                controllingFaction = new FactionId { Value = _config.majorFactions[winnerIndex].factionId };
            }
            else
            {
                controllingFaction = FactionId.Neutral;
            }

            float densityGap = winnerDensity - runnerUpDensity;
            bool isContested = winnerDensity >= threshold && densityGap < _config.contestedDensityGap;

            float controlStrength = winnerDensity >= threshold
                ? Mathf.InverseLerp(threshold, threshold + _config.controlFadeDensityRange, winnerDensity)
                : 0f;

            return new FactionTerritoryInfo
            {
                ControllingFaction = controllingFaction,
                ControlStrength = Mathf.Clamp01(controlStrength),
                IsContestedZone = isContested,
                DistanceToBorder = densityGap,
                WinnerDensity = winnerDensity,
                RunnerUpDensity = runnerUpDensity,
            };
        }
    }

    // =========================================================================
    // Layer data stored per chunk
    // =========================================================================

    public class FactionTerritoryData : WorldLayerData
    {
        public override string LayerId => "FactionTerritory";

        public FactionId ControllingFaction;
        public float ControlStrength;
        public float DistanceToBorder;
        public bool IsContestedZone;
        public float WinnerDensity;
        public float RunnerUpDensity;
    }

    // =========================================================================
    // Shared result struct
    // =========================================================================

    public struct FactionTerritoryInfo
    {
        public FactionId ControllingFaction;
        public float ControlStrength;
        public bool IsContestedZone;
        public float DistanceToBorder;
        public float WinnerDensity;
        public float RunnerUpDensity;

        public static FactionTerritoryInfo Neutral => new()
        {
            ControllingFaction = FactionId.Neutral,
            ControlStrength = 0f,
            IsContestedZone = false,
            DistanceToBorder = float.MaxValue,
            WinnerDensity = 0f,
            RunnerUpDensity = 0f,
        };
    }

    // =========================================================================
    // Query class for runtime position lookups
    // =========================================================================

    public class MetaballFactionTerritoryQuery : IWorldLayerQuery<FactionTerritoryInfo>
    {
        private readonly FactionConfig _config;
        private readonly FactionTerritoryLayer _layer;

        public MetaballFactionTerritoryQuery(FactionConfig config, FactionTerritoryLayer layer)
        {
            _config = config;
            _layer = layer;
        }

        public FactionTerritoryInfo QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            // Query path has no zone/resource data — sample with null
            return _layer.SampleTerritory(absolutePosition, worldSeed, null, null);
        }

        object IWorldLayerQuery.QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            return QueryAt(absolutePosition, worldSeed);
        }
    }
}

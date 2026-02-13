using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using Starfire.Demo;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;

namespace Starfire.Network
{
    public struct TrackedEntity
    {
        public int Id;
        public byte EntityType;
        public double2 Position;
        public float2 Velocity;
        public float Health;
        public byte Flags;

        public const byte FlagDestroyed = 1;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ServerEntityTracker : SystemBase
    {
        NativeHashMap<int, TrackedEntity> _tracked;
        bool _initialized;

        public NativeHashMap<int, TrackedEntity> TrackedEntities => _tracked;
        public bool IsInitialized => _initialized;

        protected override void OnCreate()
        {
            RequireForUpdate<SpawnConfig>();
            RequireForUpdate<SimulationConfig>();
            RequireForUpdate<AsteroidConfig>();
        }

        protected override void OnDestroy()
        {
            if (_tracked.IsCreated) _tracked.Dispose();
        }

        public void Initialize(SpawnConfig spawnConfig, SimulationConfig simConfig, AsteroidConfig asteroidConfig)
        {
            if (_initialized) return;
            _initialized = true;

            int estimatedCount = spawnConfig.TargetStarCount + spawnConfig.TargetAsteroidCount +
                                 spawnConfig.Tier0Ships + spawnConfig.Tier1Ships + spawnConfig.Tier2Ships +
                                 spawnConfig.Tier3Fleets * spawnConfig.Tier3MembersPerFleet +
                                 spawnConfig.Tier4Dormant;

            _tracked = new NativeHashMap<int, TrackedEntity>(estimatedCount, Allocator.Persistent);

            int nextId = 1;
            PopulateEntityIds(ref nextId, spawnConfig, simConfig, asteroidConfig);

            Debug.Log($"[ServerEntityTracker] Initialized with {_tracked.Count} entity ID slots for relay routing");
        }

        protected override void OnUpdate()
        {
        }

        public void UpdateFromZoneBatch(int entityId, double2 position, float2 velocity)
        {
            if (_tracked.TryGetValue(entityId, out var tracked))
            {
                tracked.Position = position;
                tracked.Velocity = velocity;
                _tracked[entityId] = tracked;
            }
        }

        public void UpdateHealth(int entityId, float newHealth)
        {
            if (_tracked.TryGetValue(entityId, out var tracked))
            {
                tracked.Health = newHealth;
                _tracked[entityId] = tracked;
            }
        }

        public void MarkDestroyed(int entityId)
        {
            if (_tracked.TryGetValue(entityId, out var tracked))
            {
                tracked.Flags |= TrackedEntity.FlagDestroyed;
                _tracked[entityId] = tracked;
            }
        }

        void PopulateEntityIds(ref int nextId, SpawnConfig spawnConfig, SimulationConfig simConfig, AsteroidConfig asteroidConfig)
        {
            SolarSystemGenerator.Generate(
                spawnConfig.WorldSeed, simConfig.Bounds.Tier3MaxDistance * 2.5f,
                spawnConfig.TargetStarCount, spawnConfig.TargetAsteroidCount,
                out var stars, out var asteroidSpawns);

            for (int i = 0; i < stars.Length; i++)
            {
                int id = nextId++;
                _tracked.Add(id, new TrackedEntity
                {
                    Id = id,
                    EntityType = (byte)Starfire.Entity.EntityType.Star,
                    Position = stars[i].Position,
                    Health = 1f
                });
            }

            float t2Sq = simConfig.Bounds.Tier2MaxDistance * simConfig.Bounds.Tier2MaxDistance;
            float t3Sq = simConfig.Bounds.Tier3MaxDistance * simConfig.Bounds.Tier3MaxDistance;
            var fieldCandidates = new NativeList<AsteroidSpawnData>(1024, Allocator.Temp);
            int currentFieldStarId = -1;

            for (int i = 0; i < asteroidSpawns.Length; i++)
            {
                var a = asteroidSpawns[i];
                double2 delta = a.Position;
                double distSq = delta.x * delta.x + delta.y * delta.y;

                if (distSq < t3Sq)
                {
                    if (distSq >= t2Sq)
                    {
                        if (a.ParentStarId != currentFieldStarId && fieldCandidates.Length > 0)
                        {
                            PopulateFieldMemberIds(ref nextId, fieldCandidates);
                            fieldCandidates.Clear();
                        }
                        currentFieldStarId = a.ParentStarId;
                        fieldCandidates.Add(a);
                    }
                    else
                    {
                        int id = nextId++;
                        _tracked.Add(id, new TrackedEntity
                        {
                            Id = id,
                            EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                            Position = a.Position,
                            Health = 1f
                        });
                    }
                }
                else
                {
                    nextId++;
                }
            }

            if (fieldCandidates.Length > 0)
                PopulateFieldMemberIds(ref nextId, fieldCandidates);

            fieldCandidates.Dispose();
            stars.Dispose();
            asteroidSpawns.Dispose();

            PopulateShipIds(ref nextId, spawnConfig, simConfig);
        }

        void PopulateFieldMemberIds(ref int nextId, NativeList<AsteroidSpawnData> candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                int id = nextId++;
                _tracked.Add(id, new TrackedEntity
                {
                    Id = id,
                    EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                    Position = candidates[i].Position,
                    Health = 1f
                });
            }
        }

        void PopulateShipIds(ref int nextId, SpawnConfig config, SimulationConfig simConfig)
        {
            var rng = new Unity.Mathematics.Random(42);
            for (int i = 0; i < config.Tier0Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(100f, simConfig.Bounds.Tier0MaxDistance * 0.8f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                rng.NextInt(0, 3);
                rng.NextFloat(config.SmallShipSensorRange, config.LargeShipSensorRange);
                int id = nextId++;
                _tracked.Add(id, new TrackedEntity
                {
                    Id = id,
                    EntityType = (byte)Starfire.Entity.EntityType.Ship,
                    Position = pos,
                    Health = config.DefaultMaxHealth
                });
            }

            rng = new Unity.Mathematics.Random(123);
            for (int i = 0; i < config.Tier1Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier0MaxDistance, simConfig.Bounds.Tier1MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                rng.NextInt(0, 3);
                rng.NextFloat(config.SmallShipSensorRange, config.MediumShipSensorRange);
                int id = nextId++;
                _tracked.Add(id, new TrackedEntity
                {
                    Id = id,
                    EntityType = (byte)Starfire.Entity.EntityType.Ship,
                    Position = pos,
                    Health = config.DefaultMaxHealth
                });
            }

            rng = new Unity.Mathematics.Random(456);
            for (int i = 0; i < config.Tier2Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier1MaxDistance, simConfig.Bounds.Tier2MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                rng.NextInt(0, 3);
                rng.NextFloat(config.SmallShipSensorRange, config.MediumShipSensorRange);
                int id = nextId++;
                _tracked.Add(id, new TrackedEntity
                {
                    Id = id,
                    EntityType = (byte)Starfire.Entity.EntityType.Ship,
                    Position = pos,
                    Health = config.DefaultMaxHealth
                });
                rng.NextFloat(0.3f, 1f); rng.NextFloat(0f, 1f);
                rng.NextFloat(0f, config.DefaultMaxSpeed); rng.NextFloat(0f, 360f);
                rng.NextFloat(10f, 100f);
                rng.NextFloat2Direction(); rng.NextFloat(1000f, 5000f);
                rng.NextFloat2Direction(); rng.NextFloat(0f, config.DefaultMaxSpeed * 0.3f);
                rng.NextFloat(0f, 360f);
            }

            rng = new Unity.Mathematics.Random(789);
            for (int i = 0; i < config.Tier3Fleets; i++)
            {
                rng.NextFloat(); rng.NextFloat();
                rng.NextInt(0, 3);
                rng.NextFloat2Direction(); rng.NextFloat(10f, 50f);
                rng.NextFloat(0f, 360f);
                for (int m = 0; m < config.Tier3MembersPerFleet; m++)
                {
                    int id = nextId++;
                    _tracked.Add(id, new TrackedEntity
                    {
                        Id = id,
                        EntityType = (byte)Starfire.Entity.EntityType.Ship,
                        Health = config.DefaultMaxHealth
                    });
                    rng.NextFloat(0f, 360f);
                }
            }

            var dormantRng = new Unity.Mathematics.Random(1011);
            for (int i = 0; i < config.Tier4Dormant; i++)
            {
                dormantRng.NextFloat(); dormantRng.NextFloat();
                nextId++;
                dormantRng.NextInt(0, 3);
                dormantRng.NextInt(5, 20);
                dormantRng.NextUInt();
            }
        }
    }
}

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
        public float2 OrbitalVelocity;
        public int ParentStarId;
        public float Size;
        public byte Composition;
        public byte TypeId;
        public int FactionId;
        public float SensorRange;
        public float Health;
        public byte Flags;

        public const byte FlagDestroyed = 1;
        public const byte FlagPromoted = 2;
    }

    public struct TrackedStar
    {
        public double2 Position;
        public float GravityStrength;
        public float GravityRange;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ServerEntityTracker : SystemBase
    {
        NativeHashMap<int, TrackedEntity> _tracked;
        NativeList<TrackedStar> _stars;
        NativeList<double2> _playerPositions;
        float _lastProximityCheck;
        bool _initialized;

        const float ProximityCheckInterval = 0.1f;
        const int MaxPromotionsPerFrame = 8;
        const int MaxDemotionsPerFrame = 8;

        public NativeHashMap<int, TrackedEntity> TrackedEntities => _tracked;
        public NativeList<TrackedStar> Stars => _stars;

        protected override void OnCreate()
        {
            RequireForUpdate<SpawnConfig>();
            RequireForUpdate<SimulationConfig>();
            RequireForUpdate<WorldOrigin>();
            RequireForUpdate<AsteroidConfig>();
        }

        protected override void OnDestroy()
        {
            if (_tracked.IsCreated) _tracked.Dispose();
            if (_stars.IsCreated) _stars.Dispose();
            if (_playerPositions.IsCreated) _playerPositions.Dispose();
        }

        public void Initialize(SpawnConfig spawnConfig, SimulationConfig simConfig, AsteroidConfig asteroidConfig, WorldOrigin origin)
        {
            if (_initialized) return;
            _initialized = true;

            int estimatedCount = spawnConfig.TargetStarCount + spawnConfig.TargetAsteroidCount +
                                 spawnConfig.Tier0Ships + spawnConfig.Tier1Ships + spawnConfig.Tier2Ships +
                                 spawnConfig.Tier3Fleets * spawnConfig.Tier3MembersPerFleet +
                                 spawnConfig.Tier4Dormant;

            _tracked = new NativeHashMap<int, TrackedEntity>(estimatedCount, Allocator.Persistent);
            _stars = new NativeList<TrackedStar>(spawnConfig.TargetStarCount, Allocator.Persistent);
            _playerPositions = new NativeList<double2>(4, Allocator.Persistent);

            int nextId = 1;
            PopulateCelestialBodies(ref nextId, spawnConfig, simConfig, asteroidConfig);
            PopulateShips(ref nextId, spawnConfig, simConfig);
            PopulateFleets(ref nextId, spawnConfig, simConfig);
            PopulateDormant(ref nextId, spawnConfig, simConfig, origin);

            Debug.Log($"[ServerEntityTracker] Tracking {_tracked.Count} entities analytically, {_stars.Length} stars");
        }

        protected override void OnUpdate()
        {
            if (!_initialized) return;

            float time = (float)SystemAPI.Time.ElapsedTime;
            float dt = SystemAPI.Time.DeltaTime;

            UpdateAsteroidOrbits(dt);

            if (time - _lastProximityCheck >= ProximityCheckInterval)
            {
                _lastProximityCheck = time;
                GatherPlayerPositions();
                CheckProximity();
            }
        }

        void UpdateAsteroidOrbits(float dt)
        {
            var keys = _tracked.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < keys.Length; i++)
            {
                var tracked = _tracked[keys[i]];
                if (tracked.EntityType != (byte)Starfire.Entity.EntityType.Asteroid) continue;
                if ((tracked.Flags & TrackedEntity.FlagDestroyed) != 0) continue;
                if ((tracked.Flags & TrackedEntity.FlagPromoted) != 0) continue;
                if (tracked.ParentStarId < 0 || tracked.ParentStarId >= _stars.Length) continue;

                var star = _stars[tracked.ParentStarId];
                double2 toStar = star.Position - tracked.Position;
                double dist = math.length(toStar);
                if (dist < 1.0) continue;

                double2 radial = toStar / dist;
                double2 tangent = new double2(-radial.y, radial.x);

                float speed = math.length(tracked.OrbitalVelocity);
                float dot = math.dot(math.normalize(tracked.OrbitalVelocity), (float2)tangent);
                if (dot < 0) tangent = -tangent;

                tracked.Position += tangent * speed * dt;
                _tracked[keys[i]] = tracked;
            }
            keys.Dispose();
        }

        void GatherPlayerPositions()
        {
            _playerPositions.Clear();
            foreach (var worldPos in
                SystemAPI.Query<RefRO<WorldPosition>>()
                    .WithAll<PlayerTag>())
            {
                _playerPositions.Add(worldPos.ValueRO.Value);
            }
        }

        void CheckProximity()
        {
            if (_playerPositions.Length == 0) return;

            var simConfig = SystemAPI.GetSingleton<SimulationConfig>();
            float t0Sq = simConfig.Bounds.Tier0MaxDistance * simConfig.Bounds.Tier0MaxDistance;
            float hysteresisSq = simConfig.Bounds.Tier0MaxDistance * 1.15f;
            hysteresisSq *= hysteresisSq;

            var toPromote = new NativeList<int>(MaxPromotionsPerFrame, Allocator.Temp);
            var toDemote = new NativeList<int>(MaxDemotionsPerFrame, Allocator.Temp);

            var keys = _tracked.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < keys.Length; i++)
            {
                var tracked = _tracked[keys[i]];
                if ((tracked.Flags & TrackedEntity.FlagDestroyed) != 0) continue;

                bool isPromoted = (tracked.Flags & TrackedEntity.FlagPromoted) != 0;
                bool nearPlayer = false;

                for (int p = 0; p < _playerPositions.Length; p++)
                {
                    double2 delta = tracked.Position - _playerPositions[p];
                    double distSq = delta.x * delta.x + delta.y * delta.y;

                    float threshold = isPromoted ? hysteresisSq : t0Sq;
                    if (distSq < threshold)
                    {
                        nearPlayer = true;
                        break;
                    }
                }

                if (nearPlayer && !isPromoted && toPromote.Length < MaxPromotionsPerFrame)
                    toPromote.Add(keys[i]);
                else if (!nearPlayer && isPromoted && toDemote.Length < MaxDemotionsPerFrame)
                    toDemote.Add(keys[i]);
            }
            keys.Dispose();

            for (int i = 0; i < toPromote.Length; i++)
            {
                var tracked = _tracked[toPromote[i]];
                tracked.Flags |= TrackedEntity.FlagPromoted;
                _tracked[toPromote[i]] = tracked;
            }

            for (int i = 0; i < toDemote.Length; i++)
            {
                var tracked = _tracked[toDemote[i]];
                tracked.Flags &= unchecked((byte)~TrackedEntity.FlagPromoted);
                _tracked[toDemote[i]] = tracked;
            }

            toPromote.Dispose();
            toDemote.Dispose();
        }

        void PopulateCelestialBodies(ref int nextId, SpawnConfig spawnConfig, SimulationConfig simConfig, AsteroidConfig asteroidConfig)
        {
            SolarSystemGenerator.Generate(
                spawnConfig.WorldSeed, simConfig.Bounds.Tier3MaxDistance * 2.5f,
                spawnConfig.TargetStarCount, spawnConfig.TargetAsteroidCount,
                out var stars, out var asteroidSpawns);

            float t0Sq = simConfig.Bounds.Tier0MaxDistance * simConfig.Bounds.Tier0MaxDistance;

            for (int i = 0; i < stars.Length; i++)
            {
                var s = stars[i];
                int id = nextId++;

                _stars.Add(new TrackedStar
                {
                    Position = s.Position,
                    GravityStrength = s.GravityStrength,
                    GravityRange = s.GravityRange
                });

                double2 delta = s.Position;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                byte flags = distSq < t0Sq ? TrackedEntity.FlagPromoted : (byte)0;

                _tracked.Add(id, new TrackedEntity
                {
                    Id = id,
                    EntityType = (byte)Starfire.Entity.EntityType.Star,
                    Position = s.Position,
                    Flags = flags
                });
            }

            var fieldCandidates = new NativeList<AsteroidSpawnData>(1024, Allocator.Temp);
            int currentFieldStarId = -1;
            float t1Sq = simConfig.Bounds.Tier1MaxDistance * simConfig.Bounds.Tier1MaxDistance;
            float t2Sq = simConfig.Bounds.Tier2MaxDistance * simConfig.Bounds.Tier2MaxDistance;
            float t3Sq = simConfig.Bounds.Tier3MaxDistance * simConfig.Bounds.Tier3MaxDistance;

            for (int i = 0; i < asteroidSpawns.Length; i++)
            {
                var a = asteroidSpawns[i];
                double2 delta = a.Position;
                double distSq = delta.x * delta.x + delta.y * delta.y;

                byte typeId = AsteroidConfig.ComputeTypeId(
                    a.Size, a.Composition, asteroidConfig.Type0MaxSize, asteroidConfig.Type1MaxSize);

                if (distSq < t0Sq)
                {
                    int id = nextId++;
                    _tracked.Add(id, new TrackedEntity
                    {
                        Id = id,
                        EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                        Position = a.Position,
                        OrbitalVelocity = a.OrbitalVelocity,
                        ParentStarId = a.ParentStarId,
                        Size = a.Size,
                        Composition = a.Composition,
                        TypeId = typeId,
                        Health = 1f,
                        Flags = TrackedEntity.FlagPromoted
                    });
                }
                else if (distSq < t1Sq || distSq < t2Sq)
                {
                    int id = nextId++;
                    _tracked.Add(id, new TrackedEntity
                    {
                        Id = id,
                        EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                        Position = a.Position,
                        OrbitalVelocity = a.OrbitalVelocity,
                        ParentStarId = a.ParentStarId,
                        Size = a.Size,
                        Composition = a.Composition,
                        TypeId = typeId,
                        Health = 1f
                    });
                }
                else if (distSq < t3Sq)
                {
                    if (a.ParentStarId != currentFieldStarId && fieldCandidates.Length > 0)
                    {
                        PopulateFieldMembers(ref nextId, fieldCandidates, asteroidConfig);
                        fieldCandidates.Clear();
                    }
                    currentFieldStarId = a.ParentStarId;
                    fieldCandidates.Add(a);
                }
                else
                {
                    nextId++;
                }
            }

            if (fieldCandidates.Length > 0)
                PopulateFieldMembers(ref nextId, fieldCandidates, asteroidConfig);

            fieldCandidates.Dispose();
            stars.Dispose();
            asteroidSpawns.Dispose();
        }

        void PopulateFieldMembers(ref int nextId, NativeList<AsteroidSpawnData> candidates, AsteroidConfig asteroidConfig)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                var a = candidates[i];
                int id = nextId++;
                byte typeId = AsteroidConfig.ComputeTypeId(
                    a.Size, a.Composition, asteroidConfig.Type0MaxSize, asteroidConfig.Type1MaxSize);

                _tracked.Add(id, new TrackedEntity
                {
                    Id = id,
                    EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                    Position = a.Position,
                    OrbitalVelocity = a.OrbitalVelocity,
                    ParentStarId = a.ParentStarId,
                    Size = a.Size,
                    Composition = a.Composition,
                    TypeId = typeId,
                    Health = 1f
                });
            }
        }

        void PopulateShips(ref int nextId, SpawnConfig config, SimulationConfig simConfig)
        {
            var rng = new Unity.Mathematics.Random(42);
            for (int i = 0; i < config.Tier0Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(100f, simConfig.Bounds.Tier0MaxDistance * 0.8f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);
                float range = rng.NextFloat(config.SmallShipSensorRange, config.LargeShipSensorRange);
                int id = nextId++;

                _tracked.Add(id, new TrackedEntity
                {
                    Id = id,
                    EntityType = (byte)Starfire.Entity.EntityType.Ship,
                    Position = pos,
                    FactionId = faction,
                    SensorRange = range,
                    Health = config.DefaultMaxHealth,
                    Flags = TrackedEntity.FlagPromoted
                });
            }

            rng = new Unity.Mathematics.Random(123);
            for (int i = 0; i < config.Tier1Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier0MaxDistance, simConfig.Bounds.Tier1MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);
                float range = rng.NextFloat(config.SmallShipSensorRange, config.MediumShipSensorRange);
                int id = nextId++;

                _tracked.Add(id, new TrackedEntity
                {
                    Id = id,
                    EntityType = (byte)Starfire.Entity.EntityType.Ship,
                    Position = pos,
                    FactionId = faction,
                    SensorRange = range,
                    Health = config.DefaultMaxHealth
                });
            }

            rng = new Unity.Mathematics.Random(456);
            for (int i = 0; i < config.Tier2Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier1MaxDistance, simConfig.Bounds.Tier2MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);
                float range = rng.NextFloat(config.SmallShipSensorRange, config.MediumShipSensorRange);
                int id = nextId++;

                _tracked.Add(id, new TrackedEntity
                {
                    Id = id,
                    EntityType = (byte)Starfire.Entity.EntityType.Ship,
                    Position = pos,
                    FactionId = faction,
                    SensorRange = range,
                    Health = config.DefaultMaxHealth
                });
            }
        }

        void PopulateFleets(ref int nextId, SpawnConfig config, SimulationConfig simConfig)
        {
            var rng = new Unity.Mathematics.Random(789);
            for (int i = 0; i < config.Tier3Fleets; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier2MaxDistance, simConfig.Bounds.Tier3MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);

                for (int m = 0; m < config.Tier3MembersPerFleet; m++)
                {
                    float memberAngle = (float)m / config.Tier3MembersPerFleet * math.PI * 2f;
                    var offset = new float2(math.cos(memberAngle), math.sin(memberAngle)) * 100f;
                    int id = nextId++;

                    _tracked.Add(id, new TrackedEntity
                    {
                        Id = id,
                        EntityType = (byte)Starfire.Entity.EntityType.Ship,
                        Position = pos + (double2)offset,
                        FactionId = faction,
                        Health = config.DefaultMaxHealth
                    });

                    rng.NextFloat(0f, 360f);
                }
            }
        }

        void PopulateDormant(ref int nextId, SpawnConfig config, SimulationConfig simConfig, WorldOrigin origin)
        {
            var rng = new Unity.Mathematics.Random(1011);
            for (int i = 0; i < config.Tier4Dormant; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier3MaxDistance, origin.WorldBoundsRadius * 0.5f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                nextId++;

                rng.NextInt(0, 3);
                rng.NextInt(5, 20);
                rng.NextUInt();
            }
        }

        public void UpdateTrackedPosition(int entityId, double2 newPosition)
        {
            if (_tracked.TryGetValue(entityId, out var tracked))
            {
                tracked.Position = newPosition;
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
    }
}

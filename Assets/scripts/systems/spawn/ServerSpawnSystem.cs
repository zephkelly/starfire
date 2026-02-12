using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Starfire.Core;
using Starfire.Demo;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class ServerSpawnSystem : SystemBase
    {
        bool _hasSpawned;
        int _nextEntityId = 1;

        EntityQuery _shipPrefabQuery;
        EntityQuery _asteroidPrefabQuery;
        EntityQuery _starPrefabQuery;

        protected override void OnCreate()
        {
            RequireForUpdate<SpawnConfig>();
            RequireForUpdate<SimulationConfig>();
            RequireForUpdate<WorldOrigin>();
            RequireForUpdate<AsteroidConfig>();

            _shipPrefabQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ShipTag>(), ComponentType.ReadOnly<Prefab>() },
                Options = EntityQueryOptions.IncludePrefab
            });

            _asteroidPrefabQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<AsteroidTag>(), ComponentType.ReadOnly<Prefab>() },
                Options = EntityQueryOptions.IncludePrefab
            });

            _starPrefabQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<StarTag>(), ComponentType.ReadOnly<Prefab>() },
                Options = EntityQueryOptions.IncludePrefab
            });
        }

        protected override void OnUpdate()
        {
            if (_hasSpawned)
            {
                Enabled = false;
                return;
            }

            if (_shipPrefabQuery.IsEmpty || _asteroidPrefabQuery.IsEmpty || _starPrefabQuery.IsEmpty)
                return;

            _hasSpawned = true;

            var shipPrefab = _shipPrefabQuery.GetSingletonEntity();
            var asteroidPrefab = _asteroidPrefabQuery.GetSingletonEntity();
            var starPrefab = _starPrefabQuery.GetSingletonEntity();

            var spawnConfig = SystemAPI.GetSingleton<SpawnConfig>();
            var simConfig = SystemAPI.GetSingleton<SimulationConfig>();
            var asteroidConfig = SystemAPI.GetSingleton<AsteroidConfig>();

            SpawnCelestialBodies(starPrefab, asteroidPrefab, spawnConfig, simConfig, asteroidConfig);
            SpawnTierShips(shipPrefab, spawnConfig, simConfig);
            SpawnTier3Fleets(spawnConfig, simConfig);
            SpawnTier4Dormant(spawnConfig, simConfig);

            int total = spawnConfig.Tier0Ships + spawnConfig.Tier1Ships + spawnConfig.Tier2Ships +
                        spawnConfig.Tier3Fleets + (spawnConfig.Tier3Fleets * spawnConfig.Tier3MembersPerFleet) +
                        spawnConfig.Tier4Dormant;
            Debug.Log($"[ServerSpawnSystem] Spawned {total} NPC ship entities + celestial bodies (player spawns via GoInGameSystem)");

            Enabled = false;
        }

        void SpawnCelestialBodies(Unity.Entities.Entity starPrefab, Unity.Entities.Entity asteroidPrefab,
            SpawnConfig spawnConfig, SimulationConfig simConfig, AsteroidConfig asteroidConfig)
        {
            SolarSystemGenerator.Generate(
                spawnConfig.WorldSeed, simConfig.Bounds.Tier3MaxDistance * 2.5f,
                spawnConfig.TargetStarCount, spawnConfig.TargetAsteroidCount,
                out var stars, out var asteroidSpawns);

            float t0Sq = simConfig.Bounds.Tier0MaxDistance * simConfig.Bounds.Tier0MaxDistance;
            float t1Sq = simConfig.Bounds.Tier1MaxDistance * simConfig.Bounds.Tier1MaxDistance;
            float t2Sq = simConfig.Bounds.Tier2MaxDistance * simConfig.Bounds.Tier2MaxDistance;
            float t3Sq = simConfig.Bounds.Tier3MaxDistance * simConfig.Bounds.Tier3MaxDistance;

            int starCount = 0;
            int asteroidIndividual = 0;
            int asteroidFieldCount = 0;
            int asteroidDormant = 0;

            for (int i = 0; i < stars.Length; i++)
            {
                var s = stars[i];
                double2 delta = s.Position;
                double distSq = delta.x * delta.x + delta.y * delta.y;

                SimulationTier tier;
                if (distSq < t0Sq)
                    tier = SimulationTier.Loaded;
                else if (distSq < t1Sq)
                    tier = SimulationTier.Active;
                else
                    tier = SimulationTier.Sensor;

                SpawnStar(starPrefab, s, i, tier);
                starCount++;
            }

            var fieldCandidates = new NativeList<AsteroidSpawnData>(1024, Allocator.Temp);
            int currentFieldStarId = -1;

            for (int i = 0; i < asteroidSpawns.Length; i++)
            {
                var a = asteroidSpawns[i];
                double2 delta = a.Position;
                double distSq = delta.x * delta.x + delta.y * delta.y;

                if (distSq < t0Sq)
                {
                    SpawnAsteroid(asteroidPrefab, a, SimulationTier.Loaded, asteroidConfig);
                    asteroidIndividual++;
                }
                else if (distSq < t1Sq)
                {
                    SpawnAsteroid(asteroidPrefab, a, SimulationTier.Active, asteroidConfig);
                    asteroidIndividual++;
                }
                else if (distSq < t2Sq)
                {
                    SpawnAsteroid(asteroidPrefab, a, SimulationTier.Sensor, asteroidConfig);
                    asteroidIndividual++;
                }
                else if (distSq < t3Sq)
                {
                    if (a.ParentStarId != currentFieldStarId && fieldCandidates.Length > 0)
                    {
                        FlushAsteroidField(fieldCandidates, asteroidConfig);
                        asteroidFieldCount++;
                        fieldCandidates.Clear();
                    }
                    currentFieldStarId = a.ParentStarId;
                    fieldCandidates.Add(a);
                }
                else
                {
                    SpawnAsteroidDormant(a);
                    asteroidDormant++;
                }
            }

            if (fieldCandidates.Length > 0)
            {
                FlushAsteroidField(fieldCandidates, asteroidConfig);
                asteroidFieldCount++;
            }

            fieldCandidates.Dispose();
            stars.Dispose();
            asteroidSpawns.Dispose();

            Debug.Log($"[ServerSpawnSystem] Celestial: {starCount} stars, {asteroidIndividual} individual asteroids, " +
                      $"{asteroidFieldCount} asteroid fields, {asteroidDormant} dormant asteroid records");
        }

        void SpawnStar(Unity.Entities.Entity prefab, StarSpawnData data, int starIndex, SimulationTier tier)
        {
            var entity = EntityManager.Instantiate(prefab);
            int id = _nextEntityId++;

            var localPos = (float2)data.Position;
            EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));

            EntityManager.SetComponentData(entity, new WorldPosition { Value = data.Position });

            EntityManager.SetComponentData(entity, new EntityIdentity
            {
                Id = id,
                EntityType = (byte)Starfire.Entity.EntityType.Star,
                Persistence = 2,
                FactionId = 0,
                ConfigId = starIndex
            });

            EntityManager.SetComponentData(entity, new SimulationTierData
            {
                Tier = tier,
                LastUpdatedTime = 0f
            });

            EntityManager.SetComponentData(entity, new StarData
            {
                SpectralType = data.SpectralType,
                Luminosity = data.Luminosity,
                Mass = data.Mass,
                Radius = data.Radius,
                SystemRadius = data.SystemRadius,
                Seed = data.Seed,
                GravityRange = data.GravityRange,
                GravityStrength = data.GravityStrength,
                RadiationRadius = data.RadiationRadius
            });

            EntityManager.SetComponentData(entity, new SensorContact
            {
                HullPercent = 1f,
                SensorRange = data.SystemRadius
            });

            SetTierTags(entity, tier);
        }

        void SpawnAsteroid(Unity.Entities.Entity prefab, AsteroidSpawnData data, SimulationTier tier, AsteroidConfig asteroidConfig)
        {
            var entity = EntityManager.Instantiate(prefab);
            int id = _nextEntityId++;

            byte typeId = AsteroidConfig.ComputeTypeId(
                data.Size, data.Composition, asteroidConfig.Type0MaxSize, asteroidConfig.Type1MaxSize);

            var localPos = (float2)data.Position;
            EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));

            EntityManager.SetComponentData(entity, new WorldPosition { Value = data.Position });

            EntityManager.SetComponentData(entity, new EntityIdentity
            {
                Id = id,
                EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                Persistence = 0,
                FactionId = 0,
                ConfigId = 0
            });

            EntityManager.SetComponentData(entity, new SimulationTierData
            {
                Tier = tier,
                LastUpdatedTime = 0f
            });

            EntityManager.SetComponentData(entity, new AsteroidData
            {
                Size = data.Size,
                Composition = data.Composition,
                TypeId = typeId,
                ParentStarId = data.ParentStarId,
                OrbitalVelocity = data.OrbitalVelocity
            });

            EntityManager.SetComponentData(entity, new SensorContact
            {
                HullPercent = 1f,
                SensorRange = 0f
            });

            if (EntityManager.HasComponent<PhysicsVelocity>(entity))
            {
                EntityManager.SetComponentData(entity, new PhysicsVelocity
                {
                    Linear = new float3(data.OrbitalVelocity.x, data.OrbitalVelocity.y, 0f)
                });
            }

            SetTierTags(entity, tier);
        }

        void FlushAsteroidField(NativeList<AsteroidSpawnData> candidates, AsteroidConfig asteroidConfig)
        {
            if (candidates.Length == 0) return;

            double2 avgPos = double2.zero;
            float totalMass = 0f;
            for (int i = 0; i < candidates.Length; i++)
            {
                avgPos += candidates[i].Position;
                totalMass += candidates[i].Size * candidates[i].Size;
            }
            avgPos /= candidates.Length;

            float maxDist = 0f;
            for (int i = 0; i < candidates.Length; i++)
            {
                double2 d = candidates[i].Position - avgPos;
                float dist = (float)math.length(d);
                if (dist > maxDist) maxDist = dist;
            }

            uint seed = (uint)(candidates[0].ParentStarId * 73856093 + candidates.Length * 19349663);

            var fieldEntity = EntityManager.CreateEntity();
            EntityManager.AddComponent<AsteroidFieldTag>(fieldEntity);
            EntityManager.AddComponentData(fieldEntity, new AsteroidFieldData
            {
                Position = avgPos,
                Radius = maxDist + 500f,
                Count = candidates.Length,
                DominantComposition = candidates[0].Composition,
                TotalMass = totalMass,
                Seed = seed,
                ParentStarId = candidates[0].ParentStarId,
                LastUpdateTime = 0f
            });

            var buffer = EntityManager.AddBuffer<AsteroidFieldMember>(fieldEntity);
            for (int i = 0; i < candidates.Length; i++)
            {
                var a = candidates[i];
                buffer.Add(new AsteroidFieldMember
                {
                    EntityId = _nextEntityId++,
                    Position = a.Position,
                    Size = a.Size,
                    Composition = a.Composition,
                    TypeId = AsteroidConfig.ComputeTypeId(
                        a.Size, a.Composition, asteroidConfig.Type0MaxSize, asteroidConfig.Type1MaxSize),
                    OrbitalVelocity = a.OrbitalVelocity
                });
            }
        }

        void SpawnAsteroidDormant(AsteroidSpawnData data)
        {
            var dormantEntity = EntityManager.CreateEntity();
            EntityManager.AddComponent<DormantTag>(dormantEntity);
            EntityManager.AddComponentData(dormantEntity, new DormantRecord
            {
                ChunkX = (long)(data.Position.x / 1000.0),
                ChunkY = (long)(data.Position.y / 1000.0),
                EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                FactionIndex = 0,
                Count = 1,
                Seed = (uint)(_nextEntityId++ * 73856093),
                Persistence = 0,
                Snapshot = default
            });
        }

        void SpawnTierShips(Unity.Entities.Entity shipPrefab, SpawnConfig config, SimulationConfig simConfig)
        {
            var rng = new Unity.Mathematics.Random(42);
            for (int i = 0; i < config.Tier0Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(100f, simConfig.Bounds.Tier0MaxDistance * 0.8f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);
                float range = rng.NextFloat(config.SmallShipSensorRange, config.LargeShipSensorRange);

                var entity = SpawnShipEntity(shipPrefab, pos, SimulationTier.Loaded, faction, 0, range, config);
                var input = EntityManager.GetComponentData<ControlInput>(entity);
                input.DriverType = 1;
                EntityManager.SetComponentData(entity, input);
            }

            rng = new Unity.Mathematics.Random(123);
            for (int i = 0; i < config.Tier1Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier0MaxDistance, simConfig.Bounds.Tier1MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);
                float range = rng.NextFloat(config.SmallShipSensorRange, config.MediumShipSensorRange);

                var entity = SpawnShipEntity(shipPrefab, pos, SimulationTier.Active, faction, 0, range, config);
                var input = EntityManager.GetComponentData<ControlInput>(entity);
                input.DriverType = 1;
                EntityManager.SetComponentData(entity, input);
            }

            rng = new Unity.Mathematics.Random(456);
            for (int i = 0; i < config.Tier2Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier1MaxDistance, simConfig.Bounds.Tier2MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);
                float range = rng.NextFloat(config.SmallShipSensorRange, config.MediumShipSensorRange);

                var entity = SpawnShipEntity(shipPrefab, pos, SimulationTier.Sensor, faction, 0, range, config);

                EntityManager.SetComponentData(entity, new SensorContact
                {
                    HullPercent = rng.NextFloat(0.3f, 1f),
                    ShieldPercent = rng.NextFloat(0f, 1f),
                    Speed = rng.NextFloat(0f, config.DefaultMaxSpeed),
                    Heading = rng.NextFloat(0f, 360f),
                    MaxSpeed = config.DefaultMaxSpeed,
                    WeaponRange = 500f,
                    SensorRange = range,
                    CombatStrength = rng.NextFloat(10f, 100f),
                    CurrentAIState = 0,
                    PreviousAIState = 0,
                    StateTimer = 0f,
                    TargetEntityId = -1,
                    Waypoint = pos + (double2)(rng.NextFloat2Direction() * rng.NextFloat(1000f, 5000f)),
                    ShieldsActive = 1,
                    WeaponsArmed = 1
                });

                EntityManager.SetComponentData(entity, new ShipSnapshot
                {
                    ConfigId = 0,
                    FactionIndex = faction,
                    Persistence = 0,
                    HullPercent = 1f,
                    ShieldPercent = 1f,
                    PropulsionEfficiency = 1f,
                    RotationEfficiency = 1f,
                    Position = pos,
                    Velocity = (double2)(rng.NextFloat2Direction() * rng.NextFloat(0f, config.DefaultMaxSpeed * 0.3f)),
                    Heading = rng.NextFloat(0f, 360f),
                    AIState = 0,
                    TargetEntityId = -1,
                    Waypoint = double2.zero
                });
            }
        }

        void SpawnTier3Fleets(SpawnConfig config, SimulationConfig simConfig)
        {
            var rng = new Unity.Mathematics.Random(789);

            for (int i = 0; i < config.Tier3Fleets; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier2MaxDistance, simConfig.Bounds.Tier3MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);

                var fleetEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<FleetTag>(fleetEntity);
                EntityManager.AddComponentData(fleetEntity, new FleetData
                {
                    FleetId = i,
                    FactionIndex = faction,
                    Position = pos,
                    Velocity = (double2)(rng.NextFloat2Direction() * rng.NextFloat(10f, 50f)),
                    Rotation = rng.NextFloat(0f, 360f),
                    MemberCount = config.Tier3MembersPerFleet,
                    Formation = 0,
                    TotalStrength = config.Tier3MembersPerFleet * 50f,
                    TotalHP = config.Tier3MembersPerFleet * config.DefaultMaxHealth,
                    CurrentBehavior = 0,
                    TargetFleetId = -1,
                    LastUpdateTime = 0f
                });

                var buffer = EntityManager.AddBuffer<FleetMember>(fleetEntity);
                for (int m = 0; m < config.Tier3MembersPerFleet; m++)
                {
                    float memberAngle = (float)m / config.Tier3MembersPerFleet * math.PI * 2f;
                    var offset = new float2(math.cos(memberAngle), math.sin(memberAngle)) * 100f;

                    buffer.Add(new FleetMember
                    {
                        EntityId = _nextEntityId++,
                        FormationSlot = m,
                        FormationOffset = offset,
                        Strength = 50f,
                        HP = config.DefaultMaxHealth,
                        Persistence = 0,
                        Snapshot = new ShipSnapshot
                        {
                            ConfigId = 0,
                            FactionIndex = faction,
                            Persistence = 0,
                            HullPercent = 1f,
                            ShieldPercent = 1f,
                            PropulsionEfficiency = 1f,
                            RotationEfficiency = 1f,
                            Position = pos + (double2)offset,
                            Velocity = double2.zero,
                            Heading = rng.NextFloat(0f, 360f),
                            AIState = 0,
                            TargetEntityId = -1,
                            Waypoint = double2.zero
                        }
                    });
                }
            }
        }

        void SpawnTier4Dormant(SpawnConfig config, SimulationConfig simConfig)
        {
            var rng = new Unity.Mathematics.Random(1011);
            var origin = SystemAPI.GetSingleton<WorldOrigin>();

            for (int i = 0; i < config.Tier4Dormant; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier3MaxDistance, origin.WorldBoundsRadius * 0.5f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);

                var dormantEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<DormantTag>(dormantEntity);
                EntityManager.AddComponentData(dormantEntity, new DormantRecord
                {
                    ChunkX = (long)(pos.x / 1000.0),
                    ChunkY = (long)(pos.y / 1000.0),
                    EntityType = (byte)Starfire.Entity.EntityType.Ship,
                    FactionIndex = rng.NextInt(0, 3),
                    Count = rng.NextInt(5, 20),
                    Seed = rng.NextUInt(),
                    Persistence = 0,
                    Snapshot = default
                });
            }
        }

        Unity.Entities.Entity SpawnShipEntity(Unity.Entities.Entity prefab, double2 worldPos, SimulationTier tier,
            int factionId, byte persistence, float sensorRange, SpawnConfig config)
        {
            var entity = EntityManager.Instantiate(prefab);
            int id = _nextEntityId++;

            var localPos = (float2)worldPos;
            EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));

            EntityManager.SetComponentData(entity, new WorldPosition { Value = worldPos });

            EntityManager.SetComponentData(entity, new EntityIdentity
            {
                Id = id,
                EntityType = (byte)Starfire.Entity.EntityType.Ship,
                Persistence = persistence,
                FactionId = factionId,
                ConfigId = 0
            });

            EntityManager.SetComponentData(entity, new SimulationTierData
            {
                Tier = tier,
                LastUpdatedTime = 0f
            });

            EntityManager.SetComponentData(entity, new ShipHull
            {
                ConfigId = 0,
                CurrentHealth = config.DefaultMaxHealth,
                MaxHealth = config.DefaultMaxHealth,
                CurrentTemperature = 0f,
                MaxTemperature = 500f,
                EfficiencyCoefficient = 1f,
                HullType = 0,
                IsEnabled = 1
            });

            EntityManager.SetComponentData(entity, new ShipPropulsion
            {
                ConfigId = 0,
                CurrentHealth = config.DefaultMaxHealth,
                MaxHealth = config.DefaultMaxHealth,
                MaxSpeed = config.DefaultMaxSpeed,
                Acceleration = config.DefaultAcceleration,
                DragCoefficient = 0.4f,
                IsEnabled = 1
            });

            EntityManager.SetComponentData(entity, new ShipRotation
            {
                ConfigId = 0,
                CurrentHealth = config.DefaultMaxHealth,
                MaxHealth = config.DefaultMaxHealth,
                TurnRate = config.DefaultTurnRate,
                TargetHeading = 0f,
                CurrentHeading = 0f,
                AngularVelocity = 0f,
                Mode = 0,
                State = 0,
                RotationType = 0,
                IsEnabled = 1
            });

            var sensorContact = EntityManager.GetComponentData<SensorContact>(entity);
            sensorContact.SensorRange = sensorRange;
            sensorContact.WeaponRange = 500f;
            sensorContact.HullPercent = 1f;
            sensorContact.ShieldPercent = 1f;
            sensorContact.MaxSpeed = config.DefaultMaxSpeed;
            EntityManager.SetComponentData(entity, sensorContact);

            SetTierTags(entity, tier);

            return entity;
        }

        void SetTierTags(Unity.Entities.Entity entity, SimulationTier tier)
        {
            if (EntityManager.HasComponent<TierTransition>(entity))
                EntityManager.SetComponentEnabled<TierTransition>(entity, false);

            bool hasRich = EntityManager.HasComponent<RichTierTag>(entity);
            bool hasVisual = EntityManager.HasComponent<VisualTierTag>(entity);
            bool hasSensor = EntityManager.HasComponent<SensorTierTag>(entity);

            switch (tier)
            {
                case SimulationTier.Loaded:
                    if (hasRich) EntityManager.SetComponentEnabled<RichTierTag>(entity, true);
                    if (hasVisual) EntityManager.SetComponentEnabled<VisualTierTag>(entity, true);
                    if (hasSensor) EntityManager.SetComponentEnabled<SensorTierTag>(entity, false);
                    break;

                case SimulationTier.Active:
                    if (hasRich) EntityManager.SetComponentEnabled<RichTierTag>(entity, true);
                    if (hasVisual) EntityManager.SetComponentEnabled<VisualTierTag>(entity, false);
                    if (hasSensor) EntityManager.SetComponentEnabled<SensorTierTag>(entity, false);
                    break;

                case SimulationTier.Sensor:
                    if (hasRich) EntityManager.SetComponentEnabled<RichTierTag>(entity, false);
                    if (hasVisual) EntityManager.SetComponentEnabled<VisualTierTag>(entity, false);
                    if (hasSensor) EntityManager.SetComponentEnabled<SensorTierTag>(entity, true);
                    if (EntityManager.HasComponent<PhysicsWorldIndex>(entity))
                        EntityManager.RemoveComponent<PhysicsWorldIndex>(entity);
                    break;
            }
        }
    }
}

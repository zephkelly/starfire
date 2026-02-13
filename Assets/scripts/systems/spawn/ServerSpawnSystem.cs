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
using Starfire.Network;
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
            var origin = SystemAPI.GetSingleton<WorldOrigin>();

            var tracker = World.GetExistingSystemManaged<ServerEntityTracker>();
            tracker.Initialize(spawnConfig, simConfig, asteroidConfig, origin);

            SpawnT0CelestialBodies(starPrefab, asteroidPrefab, spawnConfig, simConfig, asteroidConfig);
            SpawnT0Ships(shipPrefab, spawnConfig, simConfig);

            Debug.Log($"[ServerSpawnSystem] Spawned T0 ghost entities only. T1+ tracked analytically by ServerEntityTracker.");

            Enabled = false;
        }

        void SpawnT0CelestialBodies(Unity.Entities.Entity starPrefab, Unity.Entities.Entity asteroidPrefab,
            SpawnConfig spawnConfig, SimulationConfig simConfig, AsteroidConfig asteroidConfig)
        {
            SolarSystemGenerator.Generate(
                spawnConfig.WorldSeed, simConfig.Bounds.Tier3MaxDistance * 2.5f,
                spawnConfig.TargetStarCount, spawnConfig.TargetAsteroidCount,
                out var stars, out var asteroidSpawns);

            float t0Sq = simConfig.Bounds.Tier0MaxDistance * simConfig.Bounds.Tier0MaxDistance;

            int starCount = 0;
            int asteroidCount = 0;

            for (int i = 0; i < stars.Length; i++)
            {
                var s = stars[i];
                double2 delta = s.Position;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                _nextEntityId++;

                if (distSq < t0Sq)
                {
                    SpawnStar(starPrefab, s, i, SimulationTier.Loaded);
                    starCount++;
                }
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

                if (distSq < t0Sq)
                {
                    _nextEntityId++;
                    SpawnAsteroid(asteroidPrefab, a, SimulationTier.Loaded, asteroidConfig);
                    asteroidCount++;
                }
                else if (distSq < t1Sq || distSq < t2Sq)
                {
                    _nextEntityId++;
                }
                else if (distSq < t3Sq)
                {
                    if (a.ParentStarId != currentFieldStarId && fieldCandidates.Length > 0)
                    {
                        SkipFieldMemberIds(fieldCandidates);
                        fieldCandidates.Clear();
                    }
                    currentFieldStarId = a.ParentStarId;
                    fieldCandidates.Add(a);
                }
                else
                {
                    _nextEntityId++;
                }
            }

            if (fieldCandidates.Length > 0)
                SkipFieldMemberIds(fieldCandidates);

            fieldCandidates.Dispose();
            stars.Dispose();
            asteroidSpawns.Dispose();

            Debug.Log($"[ServerSpawnSystem] T0 celestial: {starCount} stars, {asteroidCount} asteroids");
        }

        void SkipFieldMemberIds(NativeList<AsteroidSpawnData> candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
                _nextEntityId++;
        }

        void SpawnStar(Unity.Entities.Entity prefab, StarSpawnData data, int starIndex, SimulationTier tier)
        {
            var entity = EntityManager.Instantiate(prefab);

            var localPos = (float2)data.Position;
            EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));

            EntityManager.SetComponentData(entity, new WorldPosition { Value = data.Position });

            EntityManager.SetComponentData(entity, new EntityIdentity
            {
                Id = _nextEntityId - 1,
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

            byte typeId = AsteroidConfig.ComputeTypeId(
                data.Size, data.Composition, asteroidConfig.Type0MaxSize, asteroidConfig.Type1MaxSize);

            var localPos = (float2)data.Position;
            EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));

            EntityManager.SetComponentData(entity, new WorldPosition { Value = data.Position });

            EntityManager.SetComponentData(entity, new EntityIdentity
            {
                Id = _nextEntityId - 1,
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

        void SpawnT0Ships(Unity.Entities.Entity shipPrefab, SpawnConfig config, SimulationConfig simConfig)
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

            SkipT1PlusShipIds(config, simConfig);
        }

        void SkipT1PlusShipIds(SpawnConfig config, SimulationConfig simConfig)
        {
            var rng = new Unity.Mathematics.Random(123);
            for (int i = 0; i < config.Tier1Ships; i++)
            {
                rng.NextFloat(); rng.NextFloat();
                rng.NextInt(0, 3);
                rng.NextFloat(config.SmallShipSensorRange, config.MediumShipSensorRange);
                _nextEntityId++;
            }

            rng = new Unity.Mathematics.Random(456);
            for (int i = 0; i < config.Tier2Ships; i++)
            {
                rng.NextFloat(); rng.NextFloat();
                rng.NextInt(0, 3);
                rng.NextFloat(config.SmallShipSensorRange, config.MediumShipSensorRange);
                _nextEntityId++;
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
                    _nextEntityId++;
                    rng.NextFloat(0f, 360f);
                }
            }

            var dormantRng = new Unity.Mathematics.Random(1011);
            for (int i = 0; i < config.Tier4Dormant; i++)
            {
                dormantRng.NextFloat(); dormantRng.NextFloat();
                dormantRng.NextInt(0, 3);
                dormantRng.NextInt(5, 20);
                dormantRng.NextUInt();
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

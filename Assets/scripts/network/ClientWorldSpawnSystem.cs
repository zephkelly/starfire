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

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class ClientWorldSpawnSystem : SystemBase
    {
        bool _hasSpawned;
        int _nextEntityId = 1;

        EntityArchetype _shipArchetype;
        EntityArchetype _asteroidArchetype;
        EntityArchetype _starArchetype;
        BlobAssetReference<Unity.Physics.Collider> _shipCollider;
        BlobAssetReference<Unity.Physics.Collider> _defaultAsteroidCollider;
        BlobAssetReference<Unity.Physics.Collider> _starCollider;

        NativeHashMap<int, Unity.Entities.Entity> _entityIdToLocal;

        protected override void OnCreate()
        {
            RequireForUpdate<SpawnConfig>();
            RequireForUpdate<SimulationConfig>();
            RequireForUpdate<AsteroidConfig>();
            RequireForUpdate<NetworkStreamInGame>();
        }

        protected override void OnDestroy()
        {
            if (_shipCollider.IsCreated) _shipCollider.Dispose();
            if (_defaultAsteroidCollider.IsCreated) _defaultAsteroidCollider.Dispose();
            if (_starCollider.IsCreated) _starCollider.Dispose();
            if (_entityIdToLocal.IsCreated) _entityIdToLocal.Dispose();
        }

        public NativeHashMap<int, Unity.Entities.Entity> EntityIdToLocal => _entityIdToLocal;

        protected override void OnUpdate()
        {
            if (_hasSpawned)
            {
                Enabled = false;
                return;
            }

            _hasSpawned = true;

            var spawnConfig = SystemAPI.GetSingleton<SpawnConfig>();
            var simConfig = SystemAPI.GetSingleton<SimulationConfig>();
            var asteroidConfig = SystemAPI.GetSingleton<AsteroidConfig>();

            int estimatedEntities = spawnConfig.TargetStarCount + spawnConfig.TargetAsteroidCount +
                                    spawnConfig.Tier0Ships + spawnConfig.Tier1Ships + spawnConfig.Tier2Ships +
                                    spawnConfig.Tier3Fleets * spawnConfig.Tier3MembersPerFleet +
                                    spawnConfig.Tier4Dormant;
            _entityIdToLocal = new NativeHashMap<int, Unity.Entities.Entity>(estimatedEntities, Allocator.Persistent);

            CreateArchetypes();
            CreateColliders(spawnConfig);

            SpawnCelestialBodies(spawnConfig, simConfig, asteroidConfig);
            SpawnTierShips(spawnConfig, simConfig);
            SpawnTier3Fleets(spawnConfig, simConfig);
            SpawnTier4Dormant(spawnConfig, simConfig);

            Debug.Log($"[ClientWorldSpawnSystem] Spawned {_entityIdToLocal.Count} local entities from seed {spawnConfig.WorldSeed}");

            Enabled = false;
        }

        void CreateArchetypes()
        {
            _shipArchetype = EntityManager.CreateArchetype(
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(WorldPosition),
                typeof(EntityIdentity),
                typeof(SimulationTierData),
                typeof(TierTransition),
                typeof(ControlInput),
                typeof(ShipTag),
                typeof(ShipHull),
                typeof(ShipRotation),
                typeof(ShipPropulsion),
                typeof(ShipSnapshot),
                typeof(SensorContact),
                typeof(RichTierTag),
                typeof(VisualTierTag),
                typeof(SensorTierTag),
                typeof(LocalEntityTag),
                typeof(PhysicsCollider),
                typeof(PhysicsMass),
                typeof(PhysicsVelocity),
                typeof(PhysicsDamping),
                typeof(PhysicsGravityFactor)
            );

            _asteroidArchetype = EntityManager.CreateArchetype(
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(WorldPosition),
                typeof(EntityIdentity),
                typeof(SimulationTierData),
                typeof(TierTransition),
                typeof(AsteroidTag),
                typeof(AsteroidData),
                typeof(SensorContact),
                typeof(RichTierTag),
                typeof(VisualTierTag),
                typeof(SensorTierTag),
                typeof(LocalEntityTag),
                typeof(PhysicsCollider),
                typeof(PhysicsMass),
                typeof(PhysicsVelocity),
                typeof(PhysicsDamping),
                typeof(PhysicsGravityFactor)
            );

            _starArchetype = EntityManager.CreateArchetype(
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(WorldPosition),
                typeof(EntityIdentity),
                typeof(SimulationTierData),
                typeof(TierTransition),
                typeof(StarTag),
                typeof(StarData),
                typeof(SensorContact),
                typeof(RichTierTag),
                typeof(VisualTierTag),
                typeof(SensorTierTag),
                typeof(LocalEntityTag),
                typeof(PhysicsCollider),
                typeof(PhysicsMass),
                typeof(PhysicsVelocity),
                typeof(PhysicsDamping),
                typeof(PhysicsGravityFactor)
            );
        }

        void CreateColliders(SpawnConfig spawnConfig)
        {
            _shipCollider = SphereCollider.Create(
                new SphereGeometry { Center = float3.zero, Radius = spawnConfig.ShipColliderRadius },
                CollisionFilter.Default);

            _defaultAsteroidCollider = SphereCollider.Create(
                new SphereGeometry { Center = float3.zero, Radius = 1f },
                CollisionFilter.Default);

            _starCollider = SphereCollider.Create(
                new SphereGeometry { Center = float3.zero, Radius = 50f },
                CollisionFilter.Default);
        }

        void SpawnCelestialBodies(SpawnConfig spawnConfig, SimulationConfig simConfig, AsteroidConfig asteroidConfig)
        {
            SolarSystemGenerator.Generate(
                spawnConfig.WorldSeed, simConfig.Bounds.Tier3MaxDistance * 2.5f,
                spawnConfig.TargetStarCount, spawnConfig.TargetAsteroidCount,
                out var stars, out var asteroidSpawns);

            float t0Sq = simConfig.Bounds.Tier0MaxDistance * simConfig.Bounds.Tier0MaxDistance;
            float t1Sq = simConfig.Bounds.Tier1MaxDistance * simConfig.Bounds.Tier1MaxDistance;
            float t2Sq = simConfig.Bounds.Tier2MaxDistance * simConfig.Bounds.Tier2MaxDistance;
            float t3Sq = simConfig.Bounds.Tier3MaxDistance * simConfig.Bounds.Tier3MaxDistance;

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

                SpawnStar(s, i, tier);
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
                    SpawnAsteroid(a, SimulationTier.Loaded, asteroidConfig);
                }
                else if (distSq < t1Sq)
                {
                    SpawnAsteroid(a, SimulationTier.Active, asteroidConfig);
                }
                else if (distSq < t2Sq)
                {
                    SpawnAsteroid(a, SimulationTier.Sensor, asteroidConfig);
                }
                else if (distSq < t3Sq)
                {
                    if (a.ParentStarId != currentFieldStarId && fieldCandidates.Length > 0)
                    {
                        FlushAsteroidField(fieldCandidates, asteroidConfig);
                        fieldCandidates.Clear();
                    }
                    currentFieldStarId = a.ParentStarId;
                    fieldCandidates.Add(a);
                }
                else
                {
                    SpawnAsteroidDormant(a);
                }
            }

            if (fieldCandidates.Length > 0)
                FlushAsteroidField(fieldCandidates, asteroidConfig);

            fieldCandidates.Dispose();
            stars.Dispose();
            asteroidSpawns.Dispose();
        }

        void SpawnStar(StarSpawnData data, int starIndex, SimulationTier tier)
        {
            var entity = EntityManager.CreateEntity(_starArchetype);
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

            EntityManager.SetComponentData(entity, new PhysicsCollider { Value = _starCollider });
            var mass = PhysicsMass.CreateKinematic(MassProperties.UnitSphere);
            EntityManager.SetComponentData(entity, mass);
            EntityManager.SetComponentData(entity, new PhysicsDamping { Linear = 1f, Angular = 1f });
            EntityManager.SetComponentData(entity, new PhysicsGravityFactor { Value = 0f });

            SetTierTags(entity, tier);
            _entityIdToLocal.Add(id, entity);
        }

        void SpawnAsteroid(AsteroidSpawnData data, SimulationTier tier, AsteroidConfig asteroidConfig)
        {
            var entity = EntityManager.CreateEntity(_asteroidArchetype);
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

            EntityManager.SetComponentData(entity, new PhysicsCollider { Value = _defaultAsteroidCollider });
            var mass = PhysicsMass.CreateDynamic(MassProperties.UnitSphere, data.Size * 10f);
            mass.InverseInertia = new float3(0f, 0f, mass.InverseInertia.z);
            EntityManager.SetComponentData(entity, mass);
            EntityManager.SetComponentData(entity, new PhysicsVelocity
            {
                Linear = new float3(data.OrbitalVelocity.x, data.OrbitalVelocity.y, 0f)
            });
            EntityManager.SetComponentData(entity, new PhysicsDamping { Linear = 0f, Angular = 0f });
            EntityManager.SetComponentData(entity, new PhysicsGravityFactor { Value = 0f });

            SetTierTags(entity, tier);
            _entityIdToLocal.Add(id, entity);
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
            EntityManager.AddComponent<LocalEntityTag>(fieldEntity);
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
                int memberId = _nextEntityId++;
                buffer.Add(new AsteroidFieldMember
                {
                    EntityId = memberId,
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
            EntityManager.AddComponent<LocalEntityTag>(dormantEntity);
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

        void SpawnTierShips(SpawnConfig config, SimulationConfig simConfig)
        {
            var rng = new Unity.Mathematics.Random(42);
            for (int i = 0; i < config.Tier0Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(100f, simConfig.Bounds.Tier0MaxDistance * 0.8f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);
                float range = rng.NextFloat(config.SmallShipSensorRange, config.LargeShipSensorRange);

                var entity = SpawnShipEntity(pos, SimulationTier.Loaded, faction, 0, range, config);
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

                var entity = SpawnShipEntity(pos, SimulationTier.Active, faction, 0, range, config);
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

                var entity = SpawnShipEntity(pos, SimulationTier.Sensor, faction, 0, range, config);

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
                EntityManager.AddComponent<LocalEntityTag>(fleetEntity);
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

            for (int i = 0; i < config.Tier4Dormant; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(simConfig.Bounds.Tier3MaxDistance, simConfig.Bounds.Tier3MaxDistance * 2.5f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);

                var dormantEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<DormantTag>(dormantEntity);
                EntityManager.AddComponent<LocalEntityTag>(dormantEntity);
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

        Unity.Entities.Entity SpawnShipEntity(double2 worldPos, SimulationTier tier,
            int factionId, byte persistence, float sensorRange, SpawnConfig config)
        {
            var entity = EntityManager.CreateEntity(_shipArchetype);
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

            EntityManager.SetComponentData(entity, new PhysicsCollider { Value = _shipCollider });
            var mass = PhysicsMass.CreateDynamic(MassProperties.UnitSphere, config.ShipMass);
            mass.InverseInertia = new float3(0f, 0f, mass.InverseInertia.z);
            EntityManager.SetComponentData(entity, mass);
            EntityManager.SetComponentData(entity, new PhysicsVelocity());
            EntityManager.SetComponentData(entity, new PhysicsDamping { Linear = 0.05f, Angular = 5f });
            EntityManager.SetComponentData(entity, new PhysicsGravityFactor { Value = 0f });

            var sensorContact = EntityManager.GetComponentData<SensorContact>(entity);
            sensorContact.SensorRange = sensorRange;
            sensorContact.WeaponRange = 500f;
            sensorContact.HullPercent = 1f;
            sensorContact.ShieldPercent = 1f;
            sensorContact.MaxSpeed = config.DefaultMaxSpeed;
            EntityManager.SetComponentData(entity, sensorContact);

            SetTierTags(entity, tier);
            _entityIdToLocal.Add(id, entity);

            return entity;
        }

        void SetTierTags(Unity.Entities.Entity entity, SimulationTier tier)
        {
            EntityManager.SetComponentEnabled<TierTransition>(entity, false);

            switch (tier)
            {
                case SimulationTier.Loaded:
                    EntityManager.SetComponentEnabled<RichTierTag>(entity, true);
                    EntityManager.SetComponentEnabled<VisualTierTag>(entity, true);
                    EntityManager.SetComponentEnabled<SensorTierTag>(entity, false);
                    break;

                case SimulationTier.Active:
                    EntityManager.SetComponentEnabled<RichTierTag>(entity, true);
                    EntityManager.SetComponentEnabled<VisualTierTag>(entity, false);
                    EntityManager.SetComponentEnabled<SensorTierTag>(entity, false);
                    EntityManager.AddComponent<Unity.Rendering.DisableRendering>(entity);
                    break;

                case SimulationTier.Sensor:
                    EntityManager.SetComponentEnabled<RichTierTag>(entity, false);
                    EntityManager.SetComponentEnabled<VisualTierTag>(entity, false);
                    EntityManager.SetComponentEnabled<SensorTierTag>(entity, true);
                    EntityManager.AddComponent<Unity.Rendering.DisableRendering>(entity);
                    break;
            }
        }
    }
}

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Starfire.Core;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;
using Starfire.Systems;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Starfire.Demo
{
    public class DemoSpawner : MonoBehaviour
    {
        [Header("Entity Counts")]
        [SerializeField] int tier0Ships = 10;
        [SerializeField] int tier1Ships = 200;
        [SerializeField] int tier2Ships = 500;
        [SerializeField] int tier3Fleets = 20;
        [SerializeField] int tier3MembersPerFleet = 10;
        [SerializeField] int tier4Dormant = 50;

        [Header("Celestial Generation")]
        [SerializeField] uint worldSeed = 12345;
        [SerializeField] int targetStarCount = 1000;
        [SerializeField] int targetAsteroidCount = 100000;

        [Header("World")]
        [SerializeField] float rebaseThreshold = 10000f;
        [SerializeField] float worldBoundsRadius = 500000f;

        [Header("Tier Distances")]
        [SerializeField] float tier0MaxDistance = 5000f;
        [SerializeField] float tier1MaxDistance = 20000f;
        [SerializeField] float tier2MaxDistance = 100000f;
        [SerializeField] float tier3MaxDistance = 200000f;

        [Header("Ship Defaults")]
        [SerializeField] float defaultMaxSpeed = 200f;
        [SerializeField] float defaultAcceleration = 60f;
        [SerializeField] float defaultTurnRate = 180f;
        [SerializeField] float defaultMaxHealth = 100f;
        [SerializeField] float shipColliderRadius = 0.5f;
        [SerializeField] float shipMass = 50f;
        [SerializeField] float playerSensorRange = 8000f;
        [SerializeField] float smallShipSensorRange = 1000f;
        [SerializeField] float mediumShipSensorRange = 3000f;
        [SerializeField] float largeShipSensorRange = 8000f;

        [Header("Rendering")]
        [SerializeField] Mesh shipMesh;
        [SerializeField] UnityEngine.Material shipMaterial;
        [SerializeField] Mesh asteroidMesh;
        [SerializeField] UnityEngine.Material asteroidMaterial;
        [SerializeField] Mesh starMesh;
        [SerializeField] UnityEngine.Material starMaterial;

        EntityManager _em;
        EntityArchetype _shipArchetype;
        EntityArchetype _asteroidArchetype;
        EntityArchetype _starArchetype;
        BlobAssetReference<Unity.Physics.Collider> _shipCollider;
        BlobAssetReference<Unity.Physics.Collider> _asteroidCollider;
        BlobAssetReference<Unity.Physics.Collider> _starCollider;
        int _nextEntityId = 1;

        void Start()
        {
            if (shipMesh == null || shipMaterial == null)
            {
                Debug.LogError("[DemoSpawner] Assign shipMesh and shipMaterial in the inspector.");
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            _em = world.EntityManager;

            CreateSingletons();
            CreateArchetypes();
            CreateColliders();
            ConfigurePhysics(world);

            SpawnCelestialBodies();
            SpawnPlayer();
            SpawnTier0Ships();
            SpawnTier1Ships();
            SpawnTier2Ships();
            SpawnTier3Fleets();
            SpawnTier4Dormant();

            int total = 1 + tier0Ships + tier1Ships + tier2Ships +
                        tier3Fleets + (tier3Fleets * tier3MembersPerFleet) + tier4Dormant;
            Debug.Log($"[DemoSpawner] Spawned {total} ship entities + celestial bodies across all tiers");
        }

        void CreateSingletons()
        {
            var originEntity = _em.CreateEntity();
            _em.AddComponentData(originEntity, new WorldOrigin
            {
                Value = double2.zero,
                RebaseThreshold = rebaseThreshold,
                WorldBoundsRadius = worldBoundsRadius
            });

            var configEntity = _em.CreateEntity();
            _em.AddComponentData(configEntity, new SimulationConfig
            {
                Bounds = new TierBounds
                {
                    Tier0MaxDistance = tier0MaxDistance,
                    Tier1MaxDistance = tier1MaxDistance,
                    Tier2MaxDistance = tier2MaxDistance,
                    Tier3MaxDistance = tier3MaxDistance,
                    Tier0Hysteresis = tier0MaxDistance * 0.15f,
                    Tier1Hysteresis = tier1MaxDistance * 0.15f,
                    Tier2Hysteresis = tier2MaxDistance * 0.15f,
                    Tier3Hysteresis = tier3MaxDistance * 0.15f,
                    TierChangeCooldown = 2f,
                    HysteresisPercent = 0.15f
                },
                Capacity = new TierCapacity
                {
                    Tier0MaxEntities = 20,
                    RichLayerMaxEntities = 500,
                    SensorLayerMaxEntities = 2000,
                    MaxFleets = 100,
                    MaxCriticalEntities = 50
                },
                Fleet = new FleetSettings
                {
                    MinFleetSize = 3,
                    GroupingRadius = 5000f
                },
                Sensor = new SensorSettings
                {
                    DefaultRange = 2000f,
                    EngagementBuffer = 2000f,
                    UpdateBatchSize = 30
                }
            });
        }

        void ConfigurePhysics(World world)
        {
            var physicsStepEntity = _em.CreateEntity();
            _em.AddComponentData(physicsStepEntity, new PhysicsStep
            {
                SimulationType = SimulationType.UnityPhysics,
                Gravity = new float3(0f, 0f, 0f),
                SolverIterationCount = 2,
                MultiThreaded = 1
            });

            world.MaximumDeltaTime = 1f / 30f;
        }

        void CreateArchetypes()
        {
            _shipArchetype = _em.CreateArchetype(
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
                typeof(PhysicsCollider),
                typeof(PhysicsMass),
                typeof(PhysicsVelocity),
                typeof(PhysicsDamping),
                typeof(PhysicsGravityFactor),
                typeof(PhysicsWorldIndex)
            );

            _asteroidArchetype = _em.CreateArchetype(
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
                typeof(PhysicsCollider),
                typeof(PhysicsMass),
                typeof(PhysicsVelocity),
                typeof(PhysicsDamping),
                typeof(PhysicsGravityFactor),
                typeof(PhysicsWorldIndex)
            );

            _starArchetype = _em.CreateArchetype(
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
                typeof(PhysicsCollider),
                typeof(PhysicsMass),
                typeof(PhysicsVelocity),
                typeof(PhysicsDamping),
                typeof(PhysicsGravityFactor),
                typeof(PhysicsWorldIndex)
            );
        }

        void CreateColliders()
        {
            _shipCollider = SphereCollider.Create(
                new SphereGeometry { Center = float3.zero, Radius = shipColliderRadius },
                CollisionFilter.Default);

            _asteroidCollider = SphereCollider.Create(
                new SphereGeometry { Center = float3.zero, Radius = 1f },
                CollisionFilter.Default);

            _starCollider = SphereCollider.Create(
                new SphereGeometry { Center = float3.zero, Radius = 50f },
                CollisionFilter.Default);
        }

        void SpawnCelestialBodies()
        {
            SolarSystemGenerator.Generate(
                worldSeed, worldBoundsRadius, targetStarCount, targetAsteroidCount,
                out var stars, out var asteroidSpawns);

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
                if (distSq < (double)tier0MaxDistance * tier0MaxDistance)
                    tier = SimulationTier.Loaded;
                else if (distSq < (double)tier1MaxDistance * tier1MaxDistance)
                    tier = SimulationTier.Active;
                else
                    tier = SimulationTier.Sensor;

                SpawnStar(s, i, tier);
                starCount++;
            }

            var fieldCandidates = new NativeList<AsteroidSpawnData>(1024, Allocator.Temp);
            int currentFieldStarId = -1;

            for (int i = 0; i < asteroidSpawns.Length; i++)
            {
                var a = asteroidSpawns[i];
                double2 delta = a.Position;
                double distSq = delta.x * delta.x + delta.y * delta.y;

                if (distSq < (double)tier0MaxDistance * tier0MaxDistance)
                {
                    SpawnAsteroid(a, SimulationTier.Loaded);
                    asteroidIndividual++;
                }
                else if (distSq < (double)tier1MaxDistance * tier1MaxDistance)
                {
                    SpawnAsteroid(a, SimulationTier.Active);
                    asteroidIndividual++;
                }
                else if (distSq < (double)tier2MaxDistance * tier2MaxDistance)
                {
                    SpawnAsteroid(a, SimulationTier.Sensor);
                    asteroidIndividual++;
                }
                else if (distSq < (double)tier3MaxDistance * tier3MaxDistance)
                {
                    if (a.ParentStarId != currentFieldStarId && fieldCandidates.Length > 0)
                    {
                        FlushAsteroidField(fieldCandidates);
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
                FlushAsteroidField(fieldCandidates);
                asteroidFieldCount++;
            }

            fieldCandidates.Dispose();
            stars.Dispose();
            asteroidSpawns.Dispose();

            Debug.Log($"[DemoSpawner] Celestial: {starCount} stars, {asteroidIndividual} individual asteroids, " +
                      $"{asteroidFieldCount} asteroid fields, {asteroidDormant} dormant asteroid records");
        }

        void SpawnStar(StarSpawnData data, int starIndex, SimulationTier tier)
        {
            var entity = _em.CreateEntity(_starArchetype);
            int id = _nextEntityId++;

            var localPos = (float2)data.Position;
            _em.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));

            _em.SetComponentData(entity, new WorldPosition { Value = data.Position });

            _em.SetComponentData(entity, new EntityIdentity
            {
                Id = id,
                EntityType = (byte)Starfire.Entity.EntityType.Star,
                Persistence = 2,
                FactionId = 0,
                ConfigId = starIndex
            });

            _em.SetComponentData(entity, new SimulationTierData
            {
                Tier = tier,
                LastUpdatedTime = 0f
            });

            _em.SetComponentData(entity, new StarData
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

            _em.SetComponentData(entity, new SensorContact
            {
                HullPercent = 1f,
                SensorRange = data.SystemRadius
            });

            _em.SetComponentData(entity, new PhysicsCollider { Value = _starCollider });
            var mass = PhysicsMass.CreateKinematic(MassProperties.UnitSphere);
            _em.SetComponentData(entity, mass);
            _em.SetComponentData(entity, new PhysicsDamping { Linear = 1f, Angular = 1f });
            _em.SetComponentData(entity, new PhysicsGravityFactor { Value = 0f });

            SetTierTags(entity, tier);

            if (tier == SimulationTier.Loaded && starMesh != null && starMaterial != null)
                AddRendering(entity, starMesh, starMaterial);
        }

        void SpawnAsteroid(AsteroidSpawnData data, SimulationTier tier)
        {
            var entity = _em.CreateEntity(_asteroidArchetype);
            int id = _nextEntityId++;

            var localPos = (float2)data.Position;
            _em.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));

            _em.SetComponentData(entity, new WorldPosition { Value = data.Position });

            _em.SetComponentData(entity, new EntityIdentity
            {
                Id = id,
                EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                Persistence = 0,
                FactionId = 0,
                ConfigId = 0
            });

            _em.SetComponentData(entity, new SimulationTierData
            {
                Tier = tier,
                LastUpdatedTime = 0f
            });

            _em.SetComponentData(entity, new AsteroidData
            {
                Size = data.Size,
                Composition = data.Composition,
                ParentStarId = data.ParentStarId,
                OrbitalVelocity = data.OrbitalVelocity
            });

            _em.SetComponentData(entity, new SensorContact
            {
                HullPercent = 1f,
                SensorRange = 0f
            });

            _em.SetComponentData(entity, new PhysicsCollider { Value = _asteroidCollider });
            var mass = PhysicsMass.CreateDynamic(MassProperties.UnitSphere, data.Size * 10f);
            mass.InverseInertia = new float3(0f, 0f, mass.InverseInertia.z);
            _em.SetComponentData(entity, mass);
            _em.SetComponentData(entity, new PhysicsVelocity
            {
                Linear = new float3(data.OrbitalVelocity.x, data.OrbitalVelocity.y, 0f)
            });
            _em.SetComponentData(entity, new PhysicsDamping { Linear = 0f, Angular = 0f });
            _em.SetComponentData(entity, new PhysicsGravityFactor { Value = 0f });

            SetTierTags(entity, tier);

            if (tier == SimulationTier.Loaded && asteroidMesh != null && asteroidMaterial != null)
                AddRendering(entity, asteroidMesh, asteroidMaterial);
        }

        void FlushAsteroidField(NativeList<AsteroidSpawnData> candidates)
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

            var fieldEntity = _em.CreateEntity();
            _em.AddComponent<AsteroidFieldTag>(fieldEntity);
            _em.AddComponentData(fieldEntity, new AsteroidFieldData
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

            var buffer = _em.AddBuffer<AsteroidFieldMember>(fieldEntity);
            for (int i = 0; i < candidates.Length; i++)
            {
                var a = candidates[i];
                buffer.Add(new AsteroidFieldMember
                {
                    EntityId = _nextEntityId++,
                    Position = a.Position,
                    Size = a.Size,
                    Composition = a.Composition,
                    OrbitalVelocity = a.OrbitalVelocity
                });
            }
        }

        void SpawnAsteroidDormant(AsteroidSpawnData data)
        {
            var dormantEntity = _em.CreateEntity();
            _em.AddComponent<DormantTag>(dormantEntity);
            _em.AddComponentData(dormantEntity, new DormantRecord
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

        Unity.Entities.Entity SpawnShipEntity(double2 worldPos, SimulationTier tier, int factionId, byte persistence, float sensorRange = 0f)
        {
            var entity = _em.CreateEntity(_shipArchetype);
            int id = _nextEntityId++;

            var localPos = (float2)(worldPos);
            _em.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));

            _em.SetComponentData(entity, new WorldPosition { Value = worldPos });

            _em.SetComponentData(entity, new EntityIdentity
            {
                Id = id,
                EntityType = (byte)Starfire.Entity.EntityType.Ship,
                Persistence = persistence,
                FactionId = factionId,
                ConfigId = 0
            });

            _em.SetComponentData(entity, new SimulationTierData
            {
                Tier = tier,
                LastUpdatedTime = 0f
            });

            _em.SetComponentData(entity, new ShipHull
            {
                ConfigId = 0,
                CurrentHealth = defaultMaxHealth,
                MaxHealth = defaultMaxHealth,
                CurrentTemperature = 0f,
                MaxTemperature = 500f,
                EfficiencyCoefficient = 1f,
                HullType = 0,
                IsEnabled = 1
            });

            _em.SetComponentData(entity, new ShipPropulsion
            {
                ConfigId = 0,
                CurrentHealth = defaultMaxHealth,
                MaxHealth = defaultMaxHealth,
                MaxSpeed = defaultMaxSpeed,
                Acceleration = defaultAcceleration,
                DragCoefficient = 0.4f,
                IsEnabled = 1
            });

            _em.SetComponentData(entity, new ShipRotation
            {
                ConfigId = 0,
                CurrentHealth = defaultMaxHealth,
                MaxHealth = defaultMaxHealth,
                TurnRate = defaultTurnRate,
                TargetHeading = 0f,
                CurrentHeading = 0f,
                AngularVelocity = 0f,
                Mode = 0,
                State = 0,
                RotationType = 0,
                IsEnabled = 1
            });

            _em.SetComponentData(entity, new PhysicsCollider { Value = _shipCollider });

            var mass = PhysicsMass.CreateDynamic(
                MassProperties.UnitSphere,
                shipMass);
            mass.InverseInertia = new float3(0f, 0f, mass.InverseInertia.z);
            _em.SetComponentData(entity, mass);

            _em.SetComponentData(entity, new PhysicsVelocity());
            _em.SetComponentData(entity, new PhysicsDamping { Linear = 0.05f, Angular = 5f });
            _em.SetComponentData(entity, new PhysicsGravityFactor { Value = 0f });

            var sensorContact = _em.GetComponentData<SensorContact>(entity);
            sensorContact.SensorRange = sensorRange;
            sensorContact.WeaponRange = 500f;
            sensorContact.HullPercent = 1f;
            sensorContact.ShieldPercent = 1f;
            sensorContact.MaxSpeed = defaultMaxSpeed;
            _em.SetComponentData(entity, sensorContact);

            SetTierTags(entity, tier);

            return entity;
        }

        void SetTierTags(Unity.Entities.Entity entity, SimulationTier tier)
        {
            _em.SetComponentEnabled<TierTransition>(entity, false);

            switch (tier)
            {
                case SimulationTier.Loaded:
                    _em.SetComponentEnabled<RichTierTag>(entity, true);
                    _em.SetComponentEnabled<VisualTierTag>(entity, true);
                    _em.SetComponentEnabled<SensorTierTag>(entity, false);
                    break;

                case SimulationTier.Active:
                    _em.SetComponentEnabled<RichTierTag>(entity, true);
                    _em.SetComponentEnabled<VisualTierTag>(entity, false);
                    _em.SetComponentEnabled<SensorTierTag>(entity, false);
                    _em.AddComponent<DisableRendering>(entity);
                    break;

                case SimulationTier.Sensor:
                    _em.SetComponentEnabled<RichTierTag>(entity, false);
                    _em.SetComponentEnabled<VisualTierTag>(entity, false);
                    _em.SetComponentEnabled<SensorTierTag>(entity, true);
                    _em.AddComponent<DisableRendering>(entity);
                    _em.RemoveComponent<PhysicsWorldIndex>(entity);
                    break;
            }
        }

        void AddRendering(Unity.Entities.Entity entity, Mesh mesh, UnityEngine.Material material)
        {
            var desc = new RenderMeshDescription(ShadowCastingMode.Off);
            var meshArray = new RenderMeshArray(
                new UnityEngine.Material[] { material },
                new Mesh[] { mesh });

            RenderMeshUtility.AddComponents(entity, _em, desc, meshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        }

        void AddRendering(Unity.Entities.Entity entity)
        {
            AddRendering(entity, shipMesh, shipMaterial);
        }

        void SpawnPlayer()
        {
            var entity = SpawnShipEntity(double2.zero, SimulationTier.Loaded, 0, 2, playerSensorRange);
            _em.AddComponent<PlayerTag>(entity);

            var input = _em.GetComponentData<ControlInput>(entity);
            input.DriverType = 0;
            _em.SetComponentData(entity, input);

            AddRendering(entity);
        }

        void SpawnTier0Ships()
        {
            var rng = new Unity.Mathematics.Random(42);

            for (int i = 0; i < tier0Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(100f, tier0MaxDistance * 0.8f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);

                float range = rng.NextFloat(smallShipSensorRange, largeShipSensorRange);
                var entity = SpawnShipEntity(pos, SimulationTier.Loaded, faction, 0, range);

                var input = _em.GetComponentData<ControlInput>(entity);
                input.DriverType = 1;
                _em.SetComponentData(entity, input);

                AddRendering(entity);
            }
        }

        void SpawnTier1Ships()
        {
            var rng = new Unity.Mathematics.Random(123);

            for (int i = 0; i < tier1Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(tier0MaxDistance, tier1MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);

                float range = rng.NextFloat(smallShipSensorRange, mediumShipSensorRange);
                var entity = SpawnShipEntity(pos, SimulationTier.Active, faction, 0, range);

                var input = _em.GetComponentData<ControlInput>(entity);
                input.DriverType = 1;
                _em.SetComponentData(entity, input);

                AddRendering(entity);
            }
        }

        void SpawnTier2Ships()
        {
            var rng = new Unity.Mathematics.Random(456);

            for (int i = 0; i < tier2Ships; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(tier1MaxDistance, tier2MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);

                float range = rng.NextFloat(smallShipSensorRange, mediumShipSensorRange);
                var entity = SpawnShipEntity(pos, SimulationTier.Sensor, faction, 0, range);

                _em.SetComponentData(entity, new SensorContact
                {
                    HullPercent = rng.NextFloat(0.3f, 1f),
                    ShieldPercent = rng.NextFloat(0f, 1f),
                    Speed = rng.NextFloat(0f, defaultMaxSpeed),
                    Heading = rng.NextFloat(0f, 360f),
                    MaxSpeed = defaultMaxSpeed,
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

                _em.SetComponentData(entity, new ShipSnapshot
                {
                    ConfigId = 0,
                    FactionIndex = faction,
                    Persistence = 0,
                    HullPercent = 1f,
                    ShieldPercent = 1f,
                    PropulsionEfficiency = 1f,
                    RotationEfficiency = 1f,
                    Position = pos,
                    Velocity = (double2)(rng.NextFloat2Direction() * rng.NextFloat(0f, defaultMaxSpeed * 0.3f)),
                    Heading = rng.NextFloat(0f, 360f),
                    AIState = 0,
                    TargetEntityId = -1,
                    Waypoint = double2.zero
                });
            }
        }

        void SpawnTier3Fleets()
        {
            var rng = new Unity.Mathematics.Random(789);

            for (int i = 0; i < tier3Fleets; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(tier2MaxDistance, tier3MaxDistance * 0.9f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);
                int faction = rng.NextInt(0, 3);

                var fleetEntity = _em.CreateEntity();
                _em.AddComponent<FleetTag>(fleetEntity);
                _em.AddComponentData(fleetEntity, new FleetData
                {
                    FleetId = i,
                    FactionIndex = faction,
                    Position = pos,
                    Velocity = (double2)(rng.NextFloat2Direction() * rng.NextFloat(10f, 50f)),
                    Rotation = rng.NextFloat(0f, 360f),
                    MemberCount = tier3MembersPerFleet,
                    Formation = 0,
                    TotalStrength = tier3MembersPerFleet * 50f,
                    TotalHP = tier3MembersPerFleet * defaultMaxHealth,
                    CurrentBehavior = 0,
                    TargetFleetId = -1,
                    LastUpdateTime = 0f
                });

                var buffer = _em.AddBuffer<FleetMember>(fleetEntity);
                for (int m = 0; m < tier3MembersPerFleet; m++)
                {
                    float memberAngle = (float)m / tier3MembersPerFleet * math.PI * 2f;
                    var offset = new float2(math.cos(memberAngle), math.sin(memberAngle)) * 100f;

                    buffer.Add(new FleetMember
                    {
                        EntityId = _nextEntityId++,
                        FormationSlot = m,
                        FormationOffset = offset,
                        Strength = 50f,
                        HP = defaultMaxHealth,
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

        void SpawnTier4Dormant()
        {
            var rng = new Unity.Mathematics.Random(1011);

            for (int i = 0; i < tier4Dormant; i++)
            {
                float angle = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(tier3MaxDistance, worldBoundsRadius * 0.5f);
                var pos = new double2(math.cos(angle) * dist, math.sin(angle) * dist);

                var dormantEntity = _em.CreateEntity();
                _em.AddComponent<DormantTag>(dormantEntity);
                _em.AddComponentData(dormantEntity, new DormantRecord
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

        void OnDestroy()
        {
            if (_shipCollider.IsCreated)
                _shipCollider.Dispose();
            if (_asteroidCollider.IsCreated)
                _asteroidCollider.Dispose();
            if (_starCollider.IsCreated)
                _starCollider.Dispose();
        }
    }
}

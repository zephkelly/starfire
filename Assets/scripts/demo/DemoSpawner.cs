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

        EntityManager _em;
        EntityArchetype _shipArchetype;
        BlobAssetReference<Unity.Physics.Collider> _shipCollider;
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
            CreateShipArchetype();
            CreateCollider();
            ConfigurePhysics(world);

            SpawnPlayer();
            SpawnTier0Ships();
            SpawnTier1Ships();
            SpawnTier2Ships();
            SpawnTier3Fleets();
            SpawnTier4Dormant();

            int total = 1 + tier0Ships + tier1Ships + tier2Ships +
                        tier3Fleets + (tier3Fleets * tier3MembersPerFleet) + tier4Dormant;
            Debug.Log($"[DemoSpawner] Spawned {total} entities across all tiers");
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
                Tier0MaxDistance = tier0MaxDistance,
                Tier1MaxDistance = tier1MaxDistance,
                Tier2MaxDistance = tier2MaxDistance,
                Tier3MaxDistance = tier3MaxDistance,
                Tier0Hysteresis = tier0MaxDistance * 0.15f,
                Tier1Hysteresis = tier1MaxDistance * 0.15f,
                Tier2Hysteresis = tier2MaxDistance * 0.15f,
                Tier3Hysteresis = tier3MaxDistance * 0.15f,
                TierChangeCooldown = 2f,
                Tier0MaxEntities = 20,
                RichLayerMaxEntities = 500,
                SensorLayerMaxEntities = 2000,
                MaxFleets = 100,
                MaxCriticalEntities = 50,
                SensorUpdateBatchSize = 30,
                MinFleetSize = 3,
                FleetGroupingRadius = 5000f,
                DefaultSensorRange = 2000f,
                EngagementBuffer = 2000f,
                HysteresisPercent = 0.15f
            });
        }

        void ConfigurePhysics(World world)
        {
            var physicsStepEntity = _em.CreateEntity();
            _em.AddComponentData(physicsStepEntity, new PhysicsStep
            {
                SimulationType = SimulationType.UnityPhysics,
                Gravity = new float3(0f, 0f, 0f),
                SolverIterationCount = 4,
                MultiThreaded = 1
            });

            world.MaximumDeltaTime = 1f / 30f;
        }

        void CreateShipArchetype()
        {
            _shipArchetype = _em.CreateArchetype(
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(WorldPosition),
                typeof(EntityIdentity),
                typeof(SimulationTierData),
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
        }

        void CreateCollider()
        {
            _shipCollider = SphereCollider.Create(
                new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = shipColliderRadius
                },
                CollisionFilter.Default);
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

        void AddRendering(Unity.Entities.Entity entity)
        {
            var desc = new RenderMeshDescription(ShadowCastingMode.Off);
            var meshArray = new RenderMeshArray(
                new UnityEngine.Material[] { shipMaterial },
                new Mesh[] { shipMesh });

            RenderMeshUtility.AddComponents(entity, _em, desc, meshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
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
        }
    }
}

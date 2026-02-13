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

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServerEntityTracker))]
    public partial class GhostPromotionSystem : SystemBase
    {
        NativeHashMap<int, Unity.Entities.Entity> _promotedGhosts;
        EntityQuery _shipPrefabQuery;
        EntityQuery _asteroidPrefabQuery;
        EntityQuery _starPrefabQuery;
        EntityQuery _connectionQuery;

        const int MaxPromotionsPerFrame = 8;
        const int MaxDemotionsPerFrame = 8;

        protected override void OnCreate()
        {
            RequireForUpdate<SpawnConfig>();
            RequireForUpdate<SimulationConfig>();

            _promotedGhosts = new NativeHashMap<int, Unity.Entities.Entity>(256, Allocator.Persistent);

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

            _connectionQuery = GetEntityQuery(ComponentType.ReadOnly<NetworkId>(), ComponentType.ReadOnly<NetworkStreamInGame>());
        }

        protected override void OnDestroy()
        {
            if (_promotedGhosts.IsCreated) _promotedGhosts.Dispose();
        }

        protected override void OnUpdate()
        {
            var tracker = World.GetExistingSystemManaged<ServerEntityTracker>();
            if (!tracker.TrackedEntities.IsCreated) return;

            if (_shipPrefabQuery.IsEmpty || _asteroidPrefabQuery.IsEmpty || _starPrefabQuery.IsEmpty)
                return;

            var shipPrefab = _shipPrefabQuery.GetSingletonEntity();
            var asteroidPrefab = _asteroidPrefabQuery.GetSingletonEntity();
            var starPrefab = _starPrefabQuery.GetSingletonEntity();

            var spawnConfig = SystemAPI.GetSingleton<SpawnConfig>();
            var asteroidConfig = SystemAPI.GetSingleton<AsteroidConfig>();
            var tracked = tracker.TrackedEntities;

            var toPromote = new NativeList<int>(MaxPromotionsPerFrame, Allocator.Temp);
            var toDemote = new NativeList<int>(MaxDemotionsPerFrame, Allocator.Temp);

            var keys = tracked.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < keys.Length; i++)
            {
                var record = tracked[keys[i]];
                if ((record.Flags & TrackedEntity.FlagDestroyed) != 0) continue;

                bool isPromotedInTracker = (record.Flags & TrackedEntity.FlagPromoted) != 0;
                bool hasGhost = _promotedGhosts.ContainsKey(keys[i]);

                if (isPromotedInTracker && !hasGhost && toPromote.Length < MaxPromotionsPerFrame)
                    toPromote.Add(keys[i]);
                else if (!isPromotedInTracker && hasGhost && toDemote.Length < MaxDemotionsPerFrame)
                    toDemote.Add(keys[i]);
            }
            keys.Dispose();

            for (int i = 0; i < toPromote.Length; i++)
                PromoteEntity(toPromote[i], tracked[toPromote[i]], shipPrefab, asteroidPrefab, starPrefab, spawnConfig, asteroidConfig);

            for (int i = 0; i < toDemote.Length; i++)
                DemoteEntity(toDemote[i], tracked[toDemote[i]]);

            toPromote.Dispose();
            toDemote.Dispose();

            CleanupStaleGhosts();
        }

        void PromoteEntity(int entityId, TrackedEntity record,
            Unity.Entities.Entity shipPrefab, Unity.Entities.Entity asteroidPrefab, Unity.Entities.Entity starPrefab,
            SpawnConfig spawnConfig, AsteroidConfig asteroidConfig)
        {
            Unity.Entities.Entity ghost;

            switch ((Starfire.Entity.EntityType)record.EntityType)
            {
                case Starfire.Entity.EntityType.Star:
                    ghost = SpawnStarGhost(starPrefab, record, entityId);
                    break;
                case Starfire.Entity.EntityType.Asteroid:
                    ghost = SpawnAsteroidGhost(asteroidPrefab, record, entityId, asteroidConfig);
                    break;
                case Starfire.Entity.EntityType.Ship:
                    ghost = SpawnShipGhost(shipPrefab, record, entityId, spawnConfig);
                    break;
                default:
                    return;
            }

            _promotedGhosts.Add(entityId, ghost);
            SendPromotionRpc(entityId);
        }

        void DemoteEntity(int entityId, TrackedEntity record)
        {
            if (!_promotedGhosts.TryGetValue(entityId, out var ghost)) return;

            if (EntityManager.Exists(ghost))
            {
                var worldPos = EntityManager.GetComponentData<WorldPosition>(ghost);
                var tracker = World.GetExistingSystemManaged<ServerEntityTracker>();
                tracker.UpdateTrackedPosition(entityId, worldPos.Value);

                float2 velocity = float2.zero;
                if (EntityManager.HasComponent<PhysicsVelocity>(ghost))
                {
                    var pv = EntityManager.GetComponentData<PhysicsVelocity>(ghost);
                    velocity = new float2(pv.Linear.x, pv.Linear.y);
                }

                SendDemotionRpc(entityId, record, worldPos.Value, velocity);
                EntityManager.DestroyEntity(ghost);
            }

            _promotedGhosts.Remove(entityId);
        }

        Unity.Entities.Entity SpawnStarGhost(Unity.Entities.Entity prefab, TrackedEntity record, int entityId)
        {
            var entity = EntityManager.Instantiate(prefab);
            var localPos = (float2)record.Position;

            EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));
            EntityManager.SetComponentData(entity, new WorldPosition { Value = record.Position });
            EntityManager.SetComponentData(entity, new EntityIdentity
            {
                Id = entityId,
                EntityType = (byte)Starfire.Entity.EntityType.Star,
                Persistence = 2,
                FactionId = 0,
                ConfigId = 0
            });
            EntityManager.SetComponentData(entity, new SimulationTierData
            {
                Tier = SimulationTier.Loaded,
                LastUpdatedTime = 0f
            });

            SetLoadedTierTags(entity);
            return entity;
        }

        Unity.Entities.Entity SpawnAsteroidGhost(Unity.Entities.Entity prefab, TrackedEntity record, int entityId, AsteroidConfig asteroidConfig)
        {
            var entity = EntityManager.Instantiate(prefab);
            var localPos = (float2)record.Position;

            EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));
            EntityManager.SetComponentData(entity, new WorldPosition { Value = record.Position });
            EntityManager.SetComponentData(entity, new EntityIdentity
            {
                Id = entityId,
                EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                Persistence = 0,
                FactionId = 0,
                ConfigId = 0
            });
            EntityManager.SetComponentData(entity, new SimulationTierData
            {
                Tier = SimulationTier.Loaded,
                LastUpdatedTime = 0f
            });
            EntityManager.SetComponentData(entity, new AsteroidData
            {
                Size = record.Size,
                Composition = record.Composition,
                TypeId = record.TypeId,
                ParentStarId = record.ParentStarId,
                OrbitalVelocity = record.OrbitalVelocity
            });

            if (EntityManager.HasComponent<PhysicsVelocity>(entity))
            {
                EntityManager.SetComponentData(entity, new PhysicsVelocity
                {
                    Linear = new float3(record.OrbitalVelocity.x, record.OrbitalVelocity.y, 0f)
                });
            }

            SetLoadedTierTags(entity);
            return entity;
        }

        Unity.Entities.Entity SpawnShipGhost(Unity.Entities.Entity prefab, TrackedEntity record, int entityId, SpawnConfig config)
        {
            var entity = EntityManager.Instantiate(prefab);
            var localPos = (float2)record.Position;

            EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(
                new float3(localPos.x, localPos.y, 0f), quaternion.identity));
            EntityManager.SetComponentData(entity, new WorldPosition { Value = record.Position });
            EntityManager.SetComponentData(entity, new EntityIdentity
            {
                Id = entityId,
                EntityType = (byte)Starfire.Entity.EntityType.Ship,
                Persistence = 0,
                FactionId = record.FactionId,
                ConfigId = 0
            });
            EntityManager.SetComponentData(entity, new SimulationTierData
            {
                Tier = SimulationTier.Loaded,
                LastUpdatedTime = 0f
            });
            EntityManager.SetComponentData(entity, new ShipHull
            {
                ConfigId = 0,
                CurrentHealth = record.Health,
                MaxHealth = config.DefaultMaxHealth,
                MaxTemperature = 500f,
                EfficiencyCoefficient = 1f,
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
                IsEnabled = 1
            });

            var input = EntityManager.GetComponentData<ControlInput>(entity);
            input.DriverType = 1;
            EntityManager.SetComponentData(entity, input);

            SetLoadedTierTags(entity);
            return entity;
        }

        void SetLoadedTierTags(Unity.Entities.Entity entity)
        {
            if (EntityManager.HasComponent<TierTransition>(entity))
                EntityManager.SetComponentEnabled<TierTransition>(entity, false);
            if (EntityManager.HasComponent<RichTierTag>(entity))
                EntityManager.SetComponentEnabled<RichTierTag>(entity, true);
            if (EntityManager.HasComponent<VisualTierTag>(entity))
                EntityManager.SetComponentEnabled<VisualTierTag>(entity, true);
            if (EntityManager.HasComponent<SensorTierTag>(entity))
                EntityManager.SetComponentEnabled<SensorTierTag>(entity, false);
        }

        void SendPromotionRpc(int entityId)
        {
            var connections = _connectionQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < connections.Length; i++)
            {
                var rpcEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(rpcEntity, new EntityPromotedRpc { EntityId = entityId });
                EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest { TargetConnection = connections[i] });
            }
            connections.Dispose();
        }

        void SendDemotionRpc(int entityId, TrackedEntity record, double2 position, float2 velocity)
        {
            var connections = _connectionQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < connections.Length; i++)
            {
                var rpcEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(rpcEntity, new EntityDemotedRpc
                {
                    EntityId = entityId,
                    EntityType = record.EntityType,
                    Position = position,
                    Velocity = velocity,
                    OrbitalVelocity = record.OrbitalVelocity,
                    Health = record.Health,
                    Tier = (byte)SimulationTier.Sensor
                });
                EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest { TargetConnection = connections[i] });
            }
            connections.Dispose();
        }

        void CleanupStaleGhosts()
        {
            var stale = new NativeList<int>(8, Allocator.Temp);
            var keys = _promotedGhosts.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < keys.Length; i++)
            {
                if (!EntityManager.Exists(_promotedGhosts[keys[i]]))
                    stale.Add(keys[i]);
            }
            keys.Dispose();

            for (int i = 0; i < stale.Length; i++)
                _promotedGhosts.Remove(stale[i]);
            stale.Dispose();
        }
    }
}

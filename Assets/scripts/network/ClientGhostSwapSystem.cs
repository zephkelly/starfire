using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ClientGhostSwapSystem : SystemBase
    {
        NativeHashSet<int> _pendingPromotions;
        NativeHashMap<int, double> _promotionTimestamps;

        const double PromotionTimeout = 2.0;

        protected override void OnCreate()
        {
            _pendingPromotions = new NativeHashSet<int>(64, Allocator.Persistent);
            _promotionTimestamps = new NativeHashMap<int, double>(64, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (_pendingPromotions.IsCreated) _pendingPromotions.Dispose();
            if (_promotionTimestamps.IsCreated) _promotionTimestamps.Dispose();
        }

        protected override void OnUpdate()
        {
            var clientSpawner = World.GetExistingSystemManaged<ClientWorldSpawnSystem>();
            if (clientSpawner == null || !clientSpawner.EntityIdToLocal.IsCreated) return;

            var entityIdToLocal = clientSpawner.EntityIdToLocal;
            double time = SystemAPI.Time.ElapsedTime;

            ProcessPromotionRpcs(entityIdToLocal, time);
            ProcessDemotionRpcs(entityIdToLocal);
            CheckForArrivedGhosts(entityIdToLocal);
            TimeoutStalePromotions(entityIdToLocal, time);
        }

        struct PromotionEntry
        {
            public int EntityId;
            public Unity.Entities.Entity LocalEntity;
            public Unity.Entities.Entity RpcEntity;
            public bool NeedsDisableRendering;
        }

        void ProcessPromotionRpcs(NativeHashMap<int, Unity.Entities.Entity> entityIdToLocal, double time)
        {
            var entries = new NativeList<PromotionEntry>(8, Allocator.Temp);

            foreach (var (rpc, entity) in
                SystemAPI.Query<RefRO<EntityPromotedRpc>>()
                    .WithAll<ReceiveRpcCommandRequest>()
                    .WithEntityAccess())
            {
                int entityId = rpc.ValueRO.EntityId;
                var entry = new PromotionEntry
                {
                    EntityId = entityId,
                    RpcEntity = entity,
                    LocalEntity = default,
                    NeedsDisableRendering = false
                };

                if (entityIdToLocal.TryGetValue(entityId, out var localEntity) && EntityManager.Exists(localEntity))
                {
                    entry.LocalEntity = localEntity;
                    entry.NeedsDisableRendering = !EntityManager.HasComponent<Unity.Rendering.DisableRendering>(localEntity);
                }

                entries.Add(entry);
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                if (entry.LocalEntity != Unity.Entities.Entity.Null && EntityManager.Exists(entry.LocalEntity))
                {
                    if (entry.NeedsDisableRendering)
                        EntityManager.AddComponent<Unity.Rendering.DisableRendering>(entry.LocalEntity);

                    _pendingPromotions.Add(entry.EntityId);
                    _promotionTimestamps[entry.EntityId] = time;
                }

                EntityManager.DestroyEntity(entry.RpcEntity);
            }

            entries.Dispose();
        }

        struct DemotionEntry
        {
            public int EntityId;
            public Unity.Entities.Entity LocalEntity;
            public Unity.Entities.Entity RpcEntity;
            public EntityDemotedRpc RpcData;
        }

        void ProcessDemotionRpcs(NativeHashMap<int, Unity.Entities.Entity> entityIdToLocal)
        {
            var entries = new NativeList<DemotionEntry>(8, Allocator.Temp);

            foreach (var (rpc, entity) in
                SystemAPI.Query<RefRO<EntityDemotedRpc>>()
                    .WithAll<ReceiveRpcCommandRequest>()
                    .WithEntityAccess())
            {
                var entry = new DemotionEntry
                {
                    EntityId = rpc.ValueRO.EntityId,
                    RpcData = rpc.ValueRO,
                    RpcEntity = entity,
                    LocalEntity = default
                };

                if (entityIdToLocal.TryGetValue(entry.EntityId, out var localEntity) && EntityManager.Exists(localEntity))
                    entry.LocalEntity = localEntity;

                entries.Add(entry);
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                if (entry.LocalEntity != Unity.Entities.Entity.Null && EntityManager.Exists(entry.LocalEntity))
                {
                    EntityManager.SetComponentData(entry.LocalEntity, new WorldPosition { Value = entry.RpcData.Position });
                    var origin = SystemAPI.GetSingleton<WorldOrigin>();
                    float2 relPos = (float2)(entry.RpcData.Position - origin.Value);
                    EntityManager.SetComponentData(entry.LocalEntity, LocalTransform.FromPositionRotation(
                        new float3(relPos.x, relPos.y, 0f), quaternion.identity));

                    if (EntityManager.HasComponent<PhysicsVelocity>(entry.LocalEntity))
                    {
                        EntityManager.SetComponentData(entry.LocalEntity, new PhysicsVelocity
                        {
                            Linear = new float3(entry.RpcData.Velocity.x, entry.RpcData.Velocity.y, 0f)
                        });
                    }

                    if (EntityManager.HasComponent<Unity.Rendering.DisableRendering>(entry.LocalEntity))
                    {
                        var tier = EntityManager.GetComponentData<SimulationTierData>(entry.LocalEntity);
                        if (tier.Tier == SimulationTier.Loaded)
                            EntityManager.RemoveComponent<Unity.Rendering.DisableRendering>(entry.LocalEntity);
                    }
                }

                _pendingPromotions.Remove(entry.EntityId);
                _promotionTimestamps.Remove(entry.EntityId);

                EntityManager.DestroyEntity(entry.RpcEntity);
            }

            entries.Dispose();
        }

        void CheckForArrivedGhosts(NativeHashMap<int, Unity.Entities.Entity> entityIdToLocal)
        {
            if (_pendingPromotions.Count == 0) return;

            var arrivedIds = new NativeList<int>(8, Allocator.Temp);

            foreach (var (identity, entity) in
                SystemAPI.Query<RefRO<EntityIdentity>>()
                    .WithAll<GhostInstance>()
                    .WithNone<LocalEntityTag>()
                    .WithEntityAccess())
            {
                int ghostId = identity.ValueRO.Id;
                if (_pendingPromotions.Contains(ghostId))
                    arrivedIds.Add(ghostId);
            }

            for (int i = 0; i < arrivedIds.Length; i++)
            {
                int entityId = arrivedIds[i];
                if (entityIdToLocal.TryGetValue(entityId, out var localEntity) && EntityManager.Exists(localEntity))
                    EntityManager.DestroyEntity(localEntity);

                entityIdToLocal.Remove(entityId);
                _pendingPromotions.Remove(entityId);
                _promotionTimestamps.Remove(entityId);
            }

            arrivedIds.Dispose();
        }

        void TimeoutStalePromotions(NativeHashMap<int, Unity.Entities.Entity> entityIdToLocal, double time)
        {
            if (_pendingPromotions.Count == 0) return;

            var timedOut = new NativeList<int>(8, Allocator.Temp);
            var keys = _promotionTimestamps.GetKeyArray(Allocator.Temp);

            for (int i = 0; i < keys.Length; i++)
            {
                if (time - _promotionTimestamps[keys[i]] > PromotionTimeout)
                    timedOut.Add(keys[i]);
            }
            keys.Dispose();

            for (int i = 0; i < timedOut.Length; i++)
            {
                int entityId = timedOut[i];

                if (entityIdToLocal.TryGetValue(entityId, out var localEntity) && EntityManager.Exists(localEntity))
                {
                    if (EntityManager.HasComponent<Unity.Rendering.DisableRendering>(localEntity))
                    {
                        var tier = EntityManager.GetComponentData<SimulationTierData>(localEntity);
                        if (tier.Tier == SimulationTier.Loaded)
                            EntityManager.RemoveComponent<Unity.Rendering.DisableRendering>(localEntity);
                    }
                }

                _pendingPromotions.Remove(entityId);
                _promotionTimestamps.Remove(entityId);
            }

            timedOut.Dispose();
        }
    }
}

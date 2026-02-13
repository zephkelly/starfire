using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Starfire.Entity;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClientZoneCorrectionSystem))]
    public partial class ClientWorldEventSystem : SystemBase
    {
        NativeHashMap<int, float> _previousHealth;

        protected override void OnCreate()
        {
            _previousHealth = new NativeHashMap<int, float>(256, Allocator.Persistent);
            RequireForUpdate<NetworkId>();
        }

        protected override void OnDestroy()
        {
            if (_previousHealth.IsCreated) _previousHealth.Dispose();
        }

        struct DestroyEntry
        {
            public int EntityId;
            public Unity.Entities.Entity RpcEntity;
        }

        struct DamageEntry
        {
            public int EntityId;
            public float NewHealth;
            public Unity.Entities.Entity RpcEntity;
        }

        protected override void OnUpdate()
        {
            var clientSpawner = World.GetExistingSystemManaged<ClientWorldSpawnSystem>();
            if (clientSpawner == null || !clientSpawner.EntityIdToLocal.IsCreated) return;

            int localNetId = SystemAPI.GetSingleton<NetworkId>().Value;
            var entityIdToLocal = clientSpawner.EntityIdToLocal;

            ProcessInboundDestroyEvents(entityIdToLocal);
            ProcessInboundDamageEvents(entityIdToLocal);
            DetectOutboundDamageEvents(localNetId);
        }

        void ProcessInboundDestroyEvents(NativeHashMap<int, Unity.Entities.Entity> entityIdToLocal)
        {
            var entries = new NativeList<DestroyEntry>(8, Allocator.Temp);

            foreach (var (rpc, entity) in
                SystemAPI.Query<RefRO<ZoneDestroyEventRpc>>()
                    .WithAll<ReceiveRpcCommandRequest>()
                    .WithEntityAccess())
            {
                entries.Add(new DestroyEntry
                {
                    EntityId = rpc.ValueRO.EntityId,
                    RpcEntity = entity
                });
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entityIdToLocal.TryGetValue(entry.EntityId, out var localEntity) && EntityManager.Exists(localEntity))
                {
                    EntityManager.DestroyEntity(localEntity);
                    entityIdToLocal.Remove(entry.EntityId);
                }
                EntityManager.DestroyEntity(entry.RpcEntity);
            }

            entries.Dispose();
        }

        void ProcessInboundDamageEvents(NativeHashMap<int, Unity.Entities.Entity> entityIdToLocal)
        {
            var entries = new NativeList<DamageEntry>(8, Allocator.Temp);

            foreach (var (rpc, entity) in
                SystemAPI.Query<RefRO<ZoneDamageEventRpc>>()
                    .WithAll<ReceiveRpcCommandRequest>()
                    .WithEntityAccess())
            {
                entries.Add(new DamageEntry
                {
                    EntityId = rpc.ValueRO.EntityId,
                    NewHealth = rpc.ValueRO.NewHealth,
                    RpcEntity = entity
                });
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entityIdToLocal.TryGetValue(entry.EntityId, out var localEntity) && EntityManager.Exists(localEntity))
                {
                    if (EntityManager.HasComponent<ShipHull>(localEntity))
                    {
                        var hull = EntityManager.GetComponentData<ShipHull>(localEntity);
                        hull.CurrentHealth = entry.NewHealth;
                        EntityManager.SetComponentData(localEntity, hull);
                    }
                }
                EntityManager.DestroyEntity(entry.RpcEntity);
            }

            entries.Dispose();
        }

        void DetectOutboundDamageEvents(int localNetId)
        {
            var outboundDamage = new NativeList<OutboundDamage>(8, Allocator.Temp);
            var outboundDestroy = new NativeList<int>(4, Allocator.Temp);

            foreach (var (identity, hull) in
                SystemAPI.Query<RefRO<EntityIdentity>, RefRO<ShipHull>>()
                    .WithAll<LocalEntityTag>()
                    .WithNone<PlayerTag>())
            {
                int id = identity.ValueRO.Id;
                float currentHealth = hull.ValueRO.CurrentHealth;

                if (_previousHealth.TryGetValue(id, out float prevHealth))
                {
                    if (currentHealth < prevHealth)
                    {
                        outboundDamage.Add(new OutboundDamage
                        {
                            EntityId = id,
                            NewHealth = currentHealth
                        });

                        if (currentHealth <= 0f)
                            outboundDestroy.Add(id);
                    }
                    _previousHealth[id] = currentHealth;
                }
                else
                {
                    _previousHealth.Add(id, currentHealth);
                }
            }

            for (int i = 0; i < outboundDamage.Length; i++)
            {
                var rpcEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(rpcEntity, new ZoneDamageEventRpc
                {
                    SourceNetworkId = localNetId,
                    EntityId = outboundDamage[i].EntityId,
                    NewHealth = outboundDamage[i].NewHealth
                });
                EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest());
            }

            for (int i = 0; i < outboundDestroy.Length; i++)
            {
                var rpcEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(rpcEntity, new ZoneDestroyEventRpc
                {
                    SourceNetworkId = localNetId,
                    EntityId = outboundDestroy[i]
                });
                EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest());
            }

            outboundDamage.Dispose();
            outboundDestroy.Dispose();
        }

        struct OutboundDamage
        {
            public int EntityId;
            public float NewHealth;
        }
    }
}

using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Starfire.Entity;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ClientWorldEventSystem : SystemBase
    {
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

            var entityIdToLocal = clientSpawner.EntityIdToLocal;

            var destroyEntries = new NativeList<DestroyEntry>(8, Allocator.Temp);
            foreach (var (rpc, entity) in
                SystemAPI.Query<RefRO<EntityDestroyedRpc>>()
                    .WithAll<ReceiveRpcCommandRequest>()
                    .WithEntityAccess())
            {
                destroyEntries.Add(new DestroyEntry
                {
                    EntityId = rpc.ValueRO.EntityId,
                    RpcEntity = entity
                });
            }

            for (int i = 0; i < destroyEntries.Length; i++)
            {
                var entry = destroyEntries[i];
                if (entityIdToLocal.TryGetValue(entry.EntityId, out var localEntity) && EntityManager.Exists(localEntity))
                {
                    EntityManager.DestroyEntity(localEntity);
                    entityIdToLocal.Remove(entry.EntityId);
                }
                EntityManager.DestroyEntity(entry.RpcEntity);
            }
            destroyEntries.Dispose();

            var damageEntries = new NativeList<DamageEntry>(8, Allocator.Temp);
            foreach (var (rpc, entity) in
                SystemAPI.Query<RefRO<EntityDamagedRpc>>()
                    .WithAll<ReceiveRpcCommandRequest>()
                    .WithEntityAccess())
            {
                damageEntries.Add(new DamageEntry
                {
                    EntityId = rpc.ValueRO.EntityId,
                    NewHealth = rpc.ValueRO.NewHealth,
                    RpcEntity = entity
                });
            }

            for (int i = 0; i < damageEntries.Length; i++)
            {
                var entry = damageEntries[i];
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
            damageEntries.Dispose();
        }
    }
}

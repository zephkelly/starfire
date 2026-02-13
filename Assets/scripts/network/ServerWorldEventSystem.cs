using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Starfire.Entity;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ServerWorldEventSystem : SystemBase
    {
        NativeHashMap<int, float> _previousHealth;
        EntityQuery _connectionQuery;

        protected override void OnCreate()
        {
            _previousHealth = new NativeHashMap<int, float>(256, Allocator.Persistent);
            _connectionQuery = GetEntityQuery(
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamInGame>());
        }

        protected override void OnDestroy()
        {
            if (_previousHealth.IsCreated) _previousHealth.Dispose();
        }

        struct HealthEvent
        {
            public int EntityId;
            public float NewHealth;
            public bool IsDestroyed;
        }

        protected override void OnUpdate()
        {
            var tracker = World.GetExistingSystemManaged<ServerEntityTracker>();
            if (tracker == null || !tracker.TrackedEntities.IsCreated) return;

            var events = new NativeList<HealthEvent>(16, Allocator.Temp);

            foreach (var (identity, hull, entity) in
                SystemAPI.Query<RefRO<EntityIdentity>, RefRO<ShipHull>>()
                    .WithNone<PlayerTag>()
                    .WithEntityAccess())
            {
                int id = identity.ValueRO.Id;
                float currentHealth = hull.ValueRO.CurrentHealth;

                if (_previousHealth.TryGetValue(id, out float prevHealth))
                {
                    if (currentHealth < prevHealth)
                    {
                        events.Add(new HealthEvent
                        {
                            EntityId = id,
                            NewHealth = currentHealth,
                            IsDestroyed = currentHealth <= 0f
                        });
                    }
                    _previousHealth[id] = currentHealth;
                }
                else
                {
                    _previousHealth.Add(id, currentHealth);
                }
            }

            for (int i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                BroadcastDamageRpc(evt.EntityId, evt.NewHealth);
                tracker.UpdateTrackedPosition(evt.EntityId, default);

                if (evt.IsDestroyed)
                {
                    BroadcastDestroyRpc(evt.EntityId);
                    tracker.MarkDestroyed(evt.EntityId);
                }
            }

            events.Dispose();
        }

        void BroadcastDamageRpc(int entityId, float newHealth)
        {
            var connections = _connectionQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < connections.Length; i++)
            {
                var rpcEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(rpcEntity, new EntityDamagedRpc
                {
                    EntityId = entityId,
                    NewHealth = newHealth
                });
                EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest
                {
                    TargetConnection = connections[i]
                });
            }
            connections.Dispose();
        }

        void BroadcastDestroyRpc(int entityId)
        {
            var connections = _connectionQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < connections.Length; i++)
            {
                var rpcEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(rpcEntity, new EntityDestroyedRpc
                {
                    EntityId = entityId
                });
                EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest
                {
                    TargetConnection = connections[i]
                });
            }
            connections.Dispose();
        }
    }
}

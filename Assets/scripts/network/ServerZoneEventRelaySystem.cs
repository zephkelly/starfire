using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServerZoneRelaySystem))]
    public partial class ServerZoneEventRelaySystem : SystemBase
    {
        EntityQuery _connectionQuery;

        protected override void OnCreate()
        {
            _connectionQuery = GetEntityQuery(
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamInGame>());
        }

        struct DamageEntry
        {
            public ZoneDamageEventRpc Rpc;
            public Unity.Entities.Entity SourceConnection;
            public Unity.Entities.Entity RpcEntity;
        }

        struct DestroyEntry
        {
            public ZoneDestroyEventRpc Rpc;
            public Unity.Entities.Entity SourceConnection;
            public Unity.Entities.Entity RpcEntity;
        }

        protected override void OnUpdate()
        {
            var tracker = World.GetExistingSystemManaged<ServerEntityTracker>();

            var damageEntries = new NativeList<DamageEntry>(8, Allocator.Temp);
            foreach (var (rpc, reqSrc, entity) in
                SystemAPI.Query<RefRO<ZoneDamageEventRpc>, RefRO<ReceiveRpcCommandRequest>>()
                    .WithEntityAccess())
            {
                damageEntries.Add(new DamageEntry
                {
                    Rpc = rpc.ValueRO,
                    SourceConnection = reqSrc.ValueRO.SourceConnection,
                    RpcEntity = entity
                });
            }

            var destroyEntries = new NativeList<DestroyEntry>(8, Allocator.Temp);
            foreach (var (rpc, reqSrc, entity) in
                SystemAPI.Query<RefRO<ZoneDestroyEventRpc>, RefRO<ReceiveRpcCommandRequest>>()
                    .WithEntityAccess())
            {
                destroyEntries.Add(new DestroyEntry
                {
                    Rpc = rpc.ValueRO,
                    SourceConnection = reqSrc.ValueRO.SourceConnection,
                    RpcEntity = entity
                });
            }

            var connections = _connectionQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < damageEntries.Length; i++)
            {
                var entry = damageEntries[i];

                if (tracker != null && tracker.IsInitialized)
                    tracker.UpdateHealth(entry.Rpc.EntityId, entry.Rpc.NewHealth);

                RelayToOthers(entry.Rpc, entry.SourceConnection, connections);
                EntityManager.DestroyEntity(entry.RpcEntity);
            }

            for (int i = 0; i < destroyEntries.Length; i++)
            {
                var entry = destroyEntries[i];

                if (tracker != null && tracker.IsInitialized)
                    tracker.MarkDestroyed(entry.Rpc.EntityId);

                RelayToOthers(entry.Rpc, entry.SourceConnection, connections);
                EntityManager.DestroyEntity(entry.RpcEntity);
            }

            connections.Dispose();
            damageEntries.Dispose();
            destroyEntries.Dispose();
        }

        void RelayToOthers(ZoneDamageEventRpc rpc, Unity.Entities.Entity sourceConn, NativeArray<Unity.Entities.Entity> connections)
        {
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] == sourceConn) continue;
                var rpcEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(rpcEntity, rpc);
                EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest
                {
                    TargetConnection = connections[i]
                });
            }
        }

        void RelayToOthers(ZoneDestroyEventRpc rpc, Unity.Entities.Entity sourceConn, NativeArray<Unity.Entities.Entity> connections)
        {
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] == sourceConn) continue;
                var rpcEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(rpcEntity, rpc);
                EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest
                {
                    TargetConnection = connections[i]
                });
            }
        }
    }
}

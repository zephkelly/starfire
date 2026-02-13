using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Starfire.Entity;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServerPlayerBroadcastSystem))]
    public partial class ServerZoneRelaySystem : SystemBase
    {
        EntityQuery _connectionQuery;

        const float ZoneMargin = 1.2f;

        protected override void OnCreate()
        {
            _connectionQuery = GetEntityQuery(
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamInGame>());
        }

        struct ClientInfo
        {
            public Unity.Entities.Entity ConnectionEntity;
            public int NetworkId;
            public double2 PlayerPosition;
            public float SensorRange;
        }

        protected override void OnUpdate()
        {
            var tracker = World.GetExistingSystemManaged<ServerEntityTracker>();
            if (tracker == null || !tracker.IsInitialized) return;

            var clients = GatherClients();
            if (clients.Length == 0)
            {
                clients.Dispose();
                return;
            }

            var batches = new NativeList<BatchEntry>(64, Allocator.Temp);

            foreach (var (rpc, reqSrc, entity) in
                SystemAPI.Query<RefRO<ZoneEntityBatchRpc>, RefRO<ReceiveRpcCommandRequest>>()
                    .WithEntityAccess())
            {
                batches.Add(new BatchEntry
                {
                    Rpc = rpc.ValueRO,
                    SourceConnection = reqSrc.ValueRO.SourceConnection,
                    RpcEntity = entity
                });
            }

            for (int b = 0; b < batches.Length; b++)
            {
                var batch = batches[b];
                int sourceNetId = batch.Rpc.SourceNetworkId;

                for (int slot = 0; slot < batch.Rpc.Count; slot++)
                {
                    batch.Rpc.Get(slot, out int entityId, out double2 pos, out float2 vel);
                    tracker.UpdateFromZoneBatch(entityId, pos, vel);
                }

                for (int c = 0; c < clients.Length; c++)
                {
                    if (clients[c].NetworkId == sourceNetId) continue;

                    bool anyRelevant = false;
                    double zoneRadius = clients[c].SensorRange * ZoneMargin;
                    double zoneRadiusSq = zoneRadius * zoneRadius;

                    for (int slot = 0; slot < batch.Rpc.Count; slot++)
                    {
                        batch.Rpc.Get(slot, out _, out double2 pos, out _);
                        double2 delta = pos - clients[c].PlayerPosition;
                        double distSq = delta.x * delta.x + delta.y * delta.y;
                        if (distSq < zoneRadiusSq)
                        {
                            anyRelevant = true;
                            break;
                        }
                    }

                    if (anyRelevant)
                    {
                        var rpcEntity = EntityManager.CreateEntity();
                        EntityManager.AddComponentData(rpcEntity, batch.Rpc);
                        EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest
                        {
                            TargetConnection = clients[c].ConnectionEntity
                        });
                    }
                }

                EntityManager.DestroyEntity(batch.RpcEntity);
            }

            batches.Dispose();
            clients.Dispose();
        }

        struct BatchEntry
        {
            public ZoneEntityBatchRpc Rpc;
            public Unity.Entities.Entity SourceConnection;
            public Unity.Entities.Entity RpcEntity;
        }

        NativeList<ClientInfo> GatherClients()
        {
            var result = new NativeList<ClientInfo>(4, Allocator.Temp);

            var playersByNetId = new NativeHashMap<int, int>(4, Allocator.Temp);
            var playerPositions = new NativeList<double2>(4, Allocator.Temp);
            var playerRanges = new NativeList<float>(4, Allocator.Temp);

            foreach (var (worldPos, sensor, ghostOwner) in
                SystemAPI.Query<RefRO<WorldPosition>, RefRO<SensorContact>, RefRO<GhostOwner>>()
                    .WithAll<PlayerTag>())
            {
                int idx = playerPositions.Length;
                playersByNetId.Add(ghostOwner.ValueRO.NetworkId, idx);
                playerPositions.Add(worldPos.ValueRO.Value);
                playerRanges.Add(sensor.ValueRO.SensorRange > 0f
                    ? sensor.ValueRO.SensorRange : 20000f);
            }

            var connections = _connectionQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < connections.Length; i++)
            {
                int netId = EntityManager.GetComponentData<NetworkId>(connections[i]).Value;

                double2 pos = double2.zero;
                float range = 20000f;
                if (playersByNetId.TryGetValue(netId, out int idx))
                {
                    pos = playerPositions[idx];
                    range = playerRanges[idx];
                }

                result.Add(new ClientInfo
                {
                    ConnectionEntity = connections[i],
                    NetworkId = netId,
                    PlayerPosition = pos,
                    SensorRange = range
                });
            }

            connections.Dispose();
            playersByNetId.Dispose();
            playerPositions.Dispose();
            playerRanges.Dispose();
            return result;
        }
    }
}

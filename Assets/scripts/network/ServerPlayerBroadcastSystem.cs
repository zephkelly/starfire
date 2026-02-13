using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Starfire.Entity;
using Starfire.Sim;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ServerEntityTracker))]
    public partial class ServerPlayerBroadcastSystem : SystemBase
    {
        EntityQuery _connectionQuery;
        Unity.Entities.Entity _bufferEntity;
        float _lastBroadcastTime;
        bool _bufferCreated;

        const float BroadcastInterval = 0.1f;

        protected override void OnCreate()
        {
            RequireForUpdate<SimulationConfig>();
            _connectionQuery = GetEntityQuery(
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamInGame>());
        }

        protected override void OnUpdate()
        {
            if (!_bufferCreated)
            {
                _bufferEntity = EntityManager.CreateEntity();
                EntityManager.AddBuffer<PlayerPositionElement>(_bufferEntity);
                _bufferCreated = true;
            }

            var buffer = EntityManager.GetBuffer<PlayerPositionElement>(_bufferEntity);
            buffer.Clear();

            var config = SystemAPI.GetSingleton<SimulationConfig>();

            foreach (var (worldPos, sensor, ghostOwner) in
                SystemAPI.Query<RefRO<WorldPosition>, RefRO<SensorContact>, RefRO<GhostOwner>>()
                    .WithAll<PlayerTag>())
            {
                buffer.Add(new PlayerPositionElement
                {
                    NetworkId = ghostOwner.ValueRO.NetworkId,
                    Position = worldPos.ValueRO.Value,
                    SensorRange = sensor.ValueRO.SensorRange > 0f
                        ? sensor.ValueRO.SensorRange
                        : config.Sensor.DefaultRange
                });
            }

            float time = (float)SystemAPI.Time.ElapsedTime;
            if (time - _lastBroadcastTime < BroadcastInterval) return;
            if (buffer.Length == 0) return;

            _lastBroadcastTime = time;

            var rpc = new PlayerPositionBroadcastRpc { PlayerCount = math.min(buffer.Length, 4) };

            if (buffer.Length > 0) { rpc.NetId0 = buffer[0].NetworkId; rpc.Pos0 = buffer[0].Position; rpc.Range0 = buffer[0].SensorRange; }
            if (buffer.Length > 1) { rpc.NetId1 = buffer[1].NetworkId; rpc.Pos1 = buffer[1].Position; rpc.Range1 = buffer[1].SensorRange; }
            if (buffer.Length > 2) { rpc.NetId2 = buffer[2].NetworkId; rpc.Pos2 = buffer[2].Position; rpc.Range2 = buffer[2].SensorRange; }
            if (buffer.Length > 3) { rpc.NetId3 = buffer[3].NetworkId; rpc.Pos3 = buffer[3].Position; rpc.Range3 = buffer[3].SensorRange; }

            var connections = _connectionQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < connections.Length; i++)
            {
                var rpcEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(rpcEntity, rpc);
                EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest
                {
                    TargetConnection = connections[i]
                });
            }
            connections.Dispose();
        }

        protected override void OnDestroy()
        {
            if (_bufferCreated && EntityManager.Exists(_bufferEntity))
                EntityManager.DestroyEntity(_bufferEntity);
        }
    }
}

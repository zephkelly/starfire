using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Systems;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TierEvaluationSystem))]
    public partial class ClientPlayerPositionSystem : SystemBase
    {
        Unity.Entities.Entity _bufferEntity;
        bool _bufferCreated;

        int _lastRemoteNetId0;
        int _lastRemoteNetId1;
        int _lastRemoteNetId2;
        int _lastRemoteNetId3;
        double2 _lastRemotePos0;
        double2 _lastRemotePos1;
        double2 _lastRemotePos2;
        double2 _lastRemotePos3;
        float _lastRemoteRange0;
        float _lastRemoteRange1;
        float _lastRemoteRange2;
        float _lastRemoteRange3;
        int _lastRemoteCount;

        protected override void OnCreate()
        {
            RequireForUpdate<SimulationConfig>();
            RequireForUpdate<NetworkId>();
        }

        protected override void OnUpdate()
        {
            if (!_bufferCreated)
            {
                _bufferEntity = EntityManager.CreateEntity();
                EntityManager.AddBuffer<PlayerPositionElement>(_bufferEntity);
                _bufferCreated = true;
            }

            ProcessBroadcastRpcs();

            var buffer = EntityManager.GetBuffer<PlayerPositionElement>(_bufferEntity);
            buffer.Clear();

            var config = SystemAPI.GetSingleton<SimulationConfig>();
            int localNetId = SystemAPI.GetSingleton<NetworkId>().Value;

            foreach (var (worldPos, sensor, ghostOwner) in
                SystemAPI.Query<RefRO<WorldPosition>, RefRO<SensorContact>, RefRO<GhostOwner>>()
                    .WithAll<PlayerTag>())
            {
                if (ghostOwner.ValueRO.NetworkId == localNetId)
                {
                    buffer.Add(new PlayerPositionElement
                    {
                        NetworkId = localNetId,
                        Position = worldPos.ValueRO.Value,
                        SensorRange = sensor.ValueRO.SensorRange > 0f
                            ? sensor.ValueRO.SensorRange
                            : config.Sensor.DefaultRange
                    });
                    break;
                }
            }

            for (int i = 0; i < _lastRemoteCount; i++)
            {
                int netId;
                double2 pos;
                float range;
                switch (i)
                {
                    case 0: netId = _lastRemoteNetId0; pos = _lastRemotePos0; range = _lastRemoteRange0; break;
                    case 1: netId = _lastRemoteNetId1; pos = _lastRemotePos1; range = _lastRemoteRange1; break;
                    case 2: netId = _lastRemoteNetId2; pos = _lastRemotePos2; range = _lastRemoteRange2; break;
                    case 3: netId = _lastRemoteNetId3; pos = _lastRemotePos3; range = _lastRemoteRange3; break;
                    default: continue;
                }

                if (netId == localNetId)
                    continue;

                bool isDuplicate = false;
                for (int b = 0; b < buffer.Length; b++)
                {
                    if (buffer[b].NetworkId == netId)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    buffer.Add(new PlayerPositionElement
                    {
                        NetworkId = netId,
                        Position = pos,
                        SensorRange = range
                    });
                }
            }
        }

        void ProcessBroadcastRpcs()
        {
            var toDestroy = new NativeList<Unity.Entities.Entity>(4, Allocator.Temp);

            foreach (var (rpc, entity) in
                SystemAPI.Query<RefRO<PlayerPositionBroadcastRpc>>()
                    .WithAll<ReceiveRpcCommandRequest>()
                    .WithEntityAccess())
            {
                var data = rpc.ValueRO;
                _lastRemoteCount = data.PlayerCount;

                if (data.PlayerCount > 0) { _lastRemoteNetId0 = data.NetId0; _lastRemotePos0 = data.Pos0; _lastRemoteRange0 = data.Range0; }
                if (data.PlayerCount > 1) { _lastRemoteNetId1 = data.NetId1; _lastRemotePos1 = data.Pos1; _lastRemoteRange1 = data.Range1; }
                if (data.PlayerCount > 2) { _lastRemoteNetId2 = data.NetId2; _lastRemotePos2 = data.Pos2; _lastRemoteRange2 = data.Range2; }
                if (data.PlayerCount > 3) { _lastRemoteNetId3 = data.NetId3; _lastRemotePos3 = data.Pos3; _lastRemoteRange3 = data.Range3; }

                toDestroy.Add(entity);
            }

            for (int i = 0; i < toDestroy.Length; i++)
                EntityManager.DestroyEntity(toDestroy[i]);

            toDestroy.Dispose();
        }

        protected override void OnDestroy()
        {
            if (_bufferCreated && EntityManager.Exists(_bufferEntity))
                EntityManager.DestroyEntity(_bufferEntity);
        }
    }
}

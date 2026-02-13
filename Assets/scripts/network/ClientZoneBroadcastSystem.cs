using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;
using Starfire.Systems;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClientWorldPositionSyncSystem))]
    public partial class ClientZoneBroadcastSystem : SystemBase
    {
        NativeHashMap<int, double2> _lastBroadcastPositions;
        float _lastBroadcastTime;
        int _batchSequence;

        const float BroadcastInterval = 0.1f;
        const float MinMoveDist = 5f;
        const float MinMoveDistSq = MinMoveDist * MinMoveDist;
        const int MaxEntitiesPerTick = 200;
        const int EntitiesPerBatch = 8;
        const float AuthorityHysteresis = 0.9f;

        protected override void OnCreate()
        {
            _lastBroadcastPositions = new NativeHashMap<int, double2>(4096, Allocator.Persistent);
            RequireForUpdate<NetworkId>();
            RequireForUpdate<SimulationConfig>();
        }

        protected override void OnDestroy()
        {
            if (_lastBroadcastPositions.IsCreated) _lastBroadcastPositions.Dispose();
        }

        struct BroadcastCandidate
        {
            public int EntityId;
            public double2 Position;
            public float2 Velocity;
            public double DistSq;
        }

        protected override void OnUpdate()
        {
            float time = (float)SystemAPI.Time.ElapsedTime;
            if (time - _lastBroadcastTime < BroadcastInterval) return;
            _lastBroadcastTime = time;

            int localNetId = SystemAPI.GetSingleton<NetworkId>().Value;

            double2 localPlayerPos = double2.zero;
            float localSensorRange = 20000f;
            bool foundPlayer = false;

            foreach (var (worldPos, sensor, ghostOwner) in
                SystemAPI.Query<RefRO<WorldPosition>, RefRO<SensorContact>, RefRO<GhostOwner>>()
                    .WithAll<PlayerTag>())
            {
                if (ghostOwner.ValueRO.NetworkId == localNetId)
                {
                    localPlayerPos = worldPos.ValueRO.Value;
                    localSensorRange = sensor.ValueRO.SensorRange > 0f
                        ? sensor.ValueRO.SensorRange
                        : 20000f;
                    foundPlayer = true;
                    break;
                }
            }

            if (!foundPlayer) return;

            var otherPlayers = new NativeList<PlayerInfo>(4, Allocator.Temp);
            GatherOtherPlayers(localNetId, ref otherPlayers);

            if (otherPlayers.Length == 0)
            {
                otherPlayers.Dispose();
                return;
            }

            double zoneRadiusSq = (double)localSensorRange * localSensorRange;

            var candidates = new NativeList<BroadcastCandidate>(256, Allocator.Temp);

            Dependency.Complete();

            foreach (var (worldPos, identity, tierData, entity) in
                SystemAPI.Query<RefRO<WorldPosition>, RefRO<EntityIdentity>, RefRO<SimulationTierData>>()
                    .WithAll<LocalEntityTag>()
                    .WithNone<PlayerTag>()
                    .WithEntityAccess())
            {
                var tier = tierData.ValueRO.Tier;
                if (tier == SimulationTier.Strategic || tier == SimulationTier.Dormant)
                    continue;

                double2 entityPos = worldPos.ValueRO.Value;
                double2 delta = entityPos - localPlayerPos;
                double distSqToLocal = delta.x * delta.x + delta.y * delta.y;

                if (distSqToLocal > zoneRadiusSq) continue;

                bool isLocalAuthority = true;
                for (int p = 0; p < otherPlayers.Length; p++)
                {
                    double2 deltaOther = entityPos - otherPlayers[p].Position;
                    double distSqOther = deltaOther.x * deltaOther.x + deltaOther.y * deltaOther.y;

                    double threshold = distSqToLocal * AuthorityHysteresis;
                    if (distSqOther < threshold)
                    {
                        if (distSqOther < distSqToLocal || (distSqOther == distSqToLocal && otherPlayers[p].NetworkId < localNetId))
                        {
                            isLocalAuthority = false;
                            break;
                        }
                    }
                }

                if (!isLocalAuthority) continue;

                int entityId = identity.ValueRO.Id;
                if (_lastBroadcastPositions.TryGetValue(entityId, out double2 lastPos))
                {
                    double2 moveDelta = entityPos - lastPos;
                    if (moveDelta.x * moveDelta.x + moveDelta.y * moveDelta.y < MinMoveDistSq)
                        continue;
                }

                float2 vel = float2.zero;
                if (EntityManager.HasComponent<PhysicsVelocity>(entity))
                {
                    var pv = EntityManager.GetComponentData<PhysicsVelocity>(entity);
                    vel = new float2(pv.Linear.x, pv.Linear.y);
                }

                candidates.Add(new BroadcastCandidate
                {
                    EntityId = entityId,
                    Position = entityPos,
                    Velocity = vel,
                    DistSq = distSqToLocal
                });
            }

            SortByDistance(ref candidates);

            int count = math.min(candidates.Length, MaxEntitiesPerTick);

            int batchCount = (count + EntitiesPerBatch - 1) / EntitiesPerBatch;
            for (int b = 0; b < batchCount; b++)
            {
                var rpc = new ZoneEntityBatchRpc
                {
                    SourceNetworkId = localNetId,
                    BatchSequence = _batchSequence++
                };

                int start = b * EntitiesPerBatch;
                int end = math.min(start + EntitiesPerBatch, count);
                rpc.Count = end - start;

                for (int i = start; i < end; i++)
                {
                    var c = candidates[i];
                    rpc.Set(i - start, c.EntityId, c.Position, c.Velocity);

                    if (_lastBroadcastPositions.ContainsKey(c.EntityId))
                        _lastBroadcastPositions[c.EntityId] = c.Position;
                    else
                        _lastBroadcastPositions.Add(c.EntityId, c.Position);
                }

                var rpcEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(rpcEntity, rpc);
                EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest());
            }

            candidates.Dispose();
            otherPlayers.Dispose();
        }

        struct PlayerInfo
        {
            public int NetworkId;
            public double2 Position;
        }

        void GatherOtherPlayers(int localNetId, ref NativeList<PlayerInfo> result)
        {
            if (!SystemAPI.HasSingleton<PlayerPositionElement>()) return;

            var bufferEntity = SystemAPI.GetSingletonEntity<PlayerPositionElement>();
            var buffer = EntityManager.GetBuffer<PlayerPositionElement>(bufferEntity, true);

            foreach (var (worldPos, ghostOwner) in
                SystemAPI.Query<RefRO<WorldPosition>, RefRO<GhostOwner>>()
                    .WithAll<PlayerTag>())
            {
                if (ghostOwner.ValueRO.NetworkId != localNetId)
                {
                    result.Add(new PlayerInfo
                    {
                        NetworkId = ghostOwner.ValueRO.NetworkId,
                        Position = worldPos.ValueRO.Value
                    });
                }
            }

            if (result.Length == 0 && buffer.Length > 1)
            {
                for (int i = 1; i < buffer.Length; i++)
                {
                    result.Add(new PlayerInfo
                    {
                        NetworkId = buffer[i].NetworkId,
                        Position = buffer[i].Position
                    });
                }
            }
        }

        static void SortByDistance(ref NativeList<BroadcastCandidate> list)
        {
            for (int i = 0; i < list.Length - 1; i++)
            {
                for (int j = i + 1; j < list.Length; j++)
                {
                    if (list[j].DistSq < list[i].DistSq)
                    {
                        var tmp = list[i];
                        list[i] = list[j];
                        list[j] = tmp;
                    }
                }
            }
        }
    }
}

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TierTransitionCleanupSystem))]
    public partial struct ShipFleetFormationSystem : ISystem
    {
        const int MaxFleetsPerTick = 5;

        float _lastUpdateTime;

        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTime = -1.0f;
            state.RequireForUpdate<SimulationConfig>();
            state.RequireForUpdate<PlayerTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            if (elapsedTime - _lastUpdateTime < 1.0f)
                return;
            _lastUpdateTime = elapsedTime;

            var config = SystemAPI.GetSingleton<SimulationConfig>();

            double2 playerWorldPos = double2.zero;
            foreach (var (worldPos, _) in SystemAPI.Query<RefRO<WorldPosition>, RefRO<PlayerTag>>())
            {
                playerWorldPos = worldPos.ValueRO.Value;
                break;
            }

            double tier2MaxDistSq = (double)config.Bounds.Tier2MaxDistance * config.Bounds.Tier2MaxDistance;
            double fleetGroupRadiusSq = (double)config.Fleet.GroupingRadius * config.Fleet.GroupingRadius;
            double cellSize = config.Fleet.GroupingRadius;

            var candidates = new NativeList<CandidateInfo>(Allocator.Temp);

            foreach (var (tierData, worldPos, identity, snapshot, entity) in
                SystemAPI.Query<RefRO<SimulationTierData>, RefRO<WorldPosition>, RefRO<EntityIdentity>, RefRO<ShipSnapshot>>()
                    .WithAll<ShipTag, SensorTierTag>()
                    .WithNone<PlayerTag>()
                    .WithEntityAccess())
            {
                if (identity.ValueRO.Persistence == 2) continue;
                if (identity.ValueRO.Persistence == 1) continue;

                double2 delta = worldPos.ValueRO.Value - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq <= tier2MaxDistSq) continue;

                candidates.Add(new CandidateInfo
                {
                    Entity = entity,
                    Position = worldPos.ValueRO.Value,
                    FactionId = identity.ValueRO.FactionId,
                    EntityId = identity.ValueRO.Id,
                    Snapshot = snapshot.ValueRO
                });
            }

            if (candidates.Length < config.Fleet.MinFleetSize)
            {
                candidates.Dispose();
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var cellMap = new NativeParallelMultiHashMap<long, int>(candidates.Length, Allocator.Temp);
            for (int i = 0; i < candidates.Length; i++)
            {
                long cellX = (long)math.floor(candidates[i].Position.x / cellSize);
                long cellY = (long)math.floor(candidates[i].Position.y / cellSize);
                long key = CellKey(candidates[i].FactionId, cellX, cellY);
                cellMap.Add(key, i);
            }

            var grouped = new NativeArray<bool>(candidates.Length, Allocator.Temp);
            int fleetsCreated = 0;

            for (int i = 0; i < candidates.Length; i++)
            {
                if (fleetsCreated >= MaxFleetsPerTick) break;
                if (grouped[i]) continue;

                var group = new NativeList<int>(Allocator.Temp);
                group.Add(i);
                grouped[i] = true;

                long cellX = (long)math.floor(candidates[i].Position.x / cellSize);
                long cellY = (long)math.floor(candidates[i].Position.y / cellSize);
                int factionId = candidates[i].FactionId;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        long neighborKey = CellKey(factionId, cellX + dx, cellY + dy);
                        if (!cellMap.TryGetFirstValue(neighborKey, out int j, out var it))
                            continue;
                        do
                        {
                            if (j <= i || grouped[j]) continue;
                            double2 d = candidates[j].Position - candidates[i].Position;
                            double dSq = d.x * d.x + d.y * d.y;
                            if (dSq <= fleetGroupRadiusSq)
                            {
                                group.Add(j);
                                grouped[j] = true;
                            }
                        } while (cellMap.TryGetNextValue(out j, ref it));
                    }
                }

                if (group.Length >= config.Fleet.MinFleetSize)
                {
                    CreateFleet(candidates, group, ecb, elapsedTime);
                    fleetsCreated++;
                }

                group.Dispose();
            }

            grouped.Dispose();
            cellMap.Dispose();
            candidates.Dispose();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        static long CellKey(int factionId, long cellX, long cellY)
        {
            return (long)factionId * 73856093L + cellX * 19349663L + cellY * 83492791L;
        }

        static void CreateFleet(NativeList<CandidateInfo> candidates, NativeList<int> group,
            EntityCommandBuffer ecb, float elapsedTime)
        {
            double2 avgPos = double2.zero;
            float totalStrength = 0f;
            float totalHP = 0f;

            for (int k = 0; k < group.Length; k++)
            {
                avgPos += candidates[group[k]].Position;
                totalStrength += 50f;
                totalHP += candidates[group[k]].Snapshot.HullPercent * 100f;
            }
            avgPos /= group.Length;

            var fleetEntity = ecb.CreateEntity();
            ecb.AddComponent(fleetEntity, new FleetTag());
            ecb.AddComponent(fleetEntity, new FleetData
            {
                FleetId = candidates[group[0]].EntityId,
                FactionIndex = candidates[group[0]].FactionId,
                Position = avgPos,
                Velocity = double2.zero,
                Rotation = 0f,
                MemberCount = group.Length,
                Formation = 0,
                TotalStrength = totalStrength,
                TotalHP = totalHP,
                CurrentBehavior = 0,
                TargetFleetId = -1,
                LastUpdateTime = elapsedTime
            });

            var buffer = ecb.AddBuffer<FleetMember>(fleetEntity);
            for (int k = 0; k < group.Length; k++)
            {
                var c = candidates[group[k]];
                float memberAngle = (float)k / group.Length * math.PI * 2f;
                var offset = new float2(math.cos(memberAngle), math.sin(memberAngle)) * 100f;

                buffer.Add(new FleetMember
                {
                    EntityId = c.EntityId,
                    FormationSlot = k,
                    FormationOffset = offset,
                    Strength = 50f,
                    HP = c.Snapshot.HullPercent * 100f,
                    Persistence = 0,
                    Snapshot = c.Snapshot
                });

                ecb.DestroyEntity(c.Entity);
            }
        }

        struct CandidateInfo
        {
            public Unity.Entities.Entity Entity;
            public double2 Position;
            public int FactionId;
            public int EntityId;
            public ShipSnapshot Snapshot;
        }
    }
}

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierTransitionCleanupSystem))]
    public partial struct ShipDormantRevivalSystem : ISystem
    {
        float _lastUpdateTime;

        public void OnCreate(ref SystemState state)
        {
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

            double threshold = config.Bounds.Tier3MaxDistance - config.Bounds.Tier3Hysteresis;
            double thresholdSq = threshold * threshold;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (dormantRecord, entity) in
                SystemAPI.Query<RefRO<DormantRecord>>()
                    .WithAll<DormantTag>()
                    .WithEntityAccess())
            {
                if (dormantRecord.ValueRO.EntityType != (byte)Starfire.Entity.EntityType.Ship)
                    continue;

                var chunkCenter = new double2(
                    dormantRecord.ValueRO.ChunkX * 1000.0 + 500.0,
                    dormantRecord.ValueRO.ChunkY * 1000.0 + 500.0);

                double2 delta = chunkCenter - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq >= thresholdSq)
                    continue;

                int memberCount = math.max(dormantRecord.ValueRO.Count, config.Fleet.MinFleetSize);
                var rng = new Random(dormantRecord.ValueRO.Seed);

                var fleetEntity = ecb.CreateEntity();
                ecb.AddComponent(fleetEntity, new FleetTag());
                ecb.AddComponent(fleetEntity, new FleetData
                {
                    FleetId = (int)dormantRecord.ValueRO.Seed,
                    FactionIndex = dormantRecord.ValueRO.FactionIndex,
                    Position = chunkCenter,
                    Velocity = (double2)(rng.NextFloat2Direction() * rng.NextFloat(10f, 50f)),
                    Rotation = rng.NextFloat(0f, 360f),
                    MemberCount = memberCount,
                    Formation = 0,
                    TotalStrength = memberCount * 50f,
                    TotalHP = memberCount * 100f,
                    CurrentBehavior = 0,
                    TargetFleetId = -1,
                    LastUpdateTime = elapsedTime
                });

                var buffer = ecb.AddBuffer<FleetMember>(fleetEntity);
                for (int i = 0; i < memberCount; i++)
                {
                    float angle = (float)i / memberCount * math.PI * 2f;
                    var offset = new float2(math.cos(angle), math.sin(angle)) * 100f;

                    buffer.Add(new FleetMember
                    {
                        EntityId = rng.NextInt(100000, 999999),
                        FormationSlot = i,
                        FormationOffset = offset,
                        Strength = 50f,
                        HP = 100f,
                        Persistence = 0,
                        Snapshot = new ShipSnapshot
                        {
                            ConfigId = 0,
                            FactionIndex = dormantRecord.ValueRO.FactionIndex,
                            Persistence = 0,
                            HullPercent = 1f,
                            ShieldPercent = 1f,
                            PropulsionEfficiency = 1f,
                            RotationEfficiency = 1f,
                            Position = chunkCenter + (double2)offset,
                            Velocity = double2.zero,
                            Heading = rng.NextFloat(0f, 360f),
                            AIState = 0,
                            TargetEntityId = -1,
                            Waypoint = double2.zero
                        }
                    });
                }

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

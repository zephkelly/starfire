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
    public partial struct AsteroidDormantRevivalSystem : ISystem
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
                if (dormantRecord.ValueRO.EntityType != (byte)Starfire.Entity.EntityType.Asteroid)
                    continue;

                var chunkCenter = new double2(
                    dormantRecord.ValueRO.ChunkX * 1000.0 + 500.0,
                    dormantRecord.ValueRO.ChunkY * 1000.0 + 500.0);

                double2 delta = chunkCenter - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq >= thresholdSq)
                    continue;

                int count = math.max(dormantRecord.ValueRO.Count, 3);
                var rng = new Random(dormantRecord.ValueRO.Seed);

                var fieldEntity = ecb.CreateEntity();
                ecb.AddComponent(fieldEntity, new AsteroidFieldTag());
                ecb.AddComponent(fieldEntity, new AsteroidFieldData
                {
                    Position = chunkCenter,
                    Radius = 2000f,
                    Count = count,
                    DominantComposition = 0,
                    TotalMass = count * 4f,
                    Seed = dormantRecord.ValueRO.Seed,
                    ParentStarId = -1,
                    LastUpdateTime = elapsedTime
                });

                var buffer = ecb.AddBuffer<AsteroidFieldMember>(fieldEntity);
                for (int i = 0; i < count; i++)
                {
                    float angle = rng.NextFloat(0f, math.PI * 2f);
                    float dist = rng.NextFloat(100f, 2000f);
                    var memberPos = chunkCenter + new double2(math.cos(angle) * dist, math.sin(angle) * dist);

                    buffer.Add(new AsteroidFieldMember
                    {
                        EntityId = rng.NextInt(100000, 999999),
                        Position = memberPos,
                        Size = rng.NextFloat(0.3f, 3.0f),
                        Composition = (byte)rng.NextInt(0, 4),
                        AngularSpeed = rng.NextFloat() < 0.15f ? rng.NextFloat(5f, 45f) : 0f,
                        DriftSpeed = rng.NextFloat() < 0.05f ? rng.NextFloat(1f, 10f) : 0f,
                        DriftDirection = rng.NextFloat2Direction()
                    });
                }

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

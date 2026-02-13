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
    public partial struct AsteroidDormantConversionSystem : ISystem
    {
        float _lastUpdateTime;

        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTime = -0.167f;
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

            double tier3MaxDistSq = (double)config.Bounds.Tier3MaxDistance * config.Bounds.Tier3MaxDistance;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (fieldData, entity) in
                SystemAPI.Query<RefRO<AsteroidFieldData>>()
                    .WithAll<AsteroidFieldTag>()
                    .WithEntityAccess())
            {
                double2 delta = fieldData.ValueRO.Position - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq <= tier3MaxDistSq)
                    continue;

                var dormantEntity = ecb.CreateEntity();
                ecb.AddComponent(dormantEntity, new DormantTag());
                ecb.AddComponent(dormantEntity, new DormantRecord
                {
                    ChunkX = (long)(fieldData.ValueRO.Position.x / 1000.0),
                    ChunkY = (long)(fieldData.ValueRO.Position.y / 1000.0),
                    EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                    FactionIndex = 0,
                    Count = fieldData.ValueRO.Count,
                    Seed = fieldData.ValueRO.Seed,
                    Persistence = 0,
                    Snapshot = default
                });

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

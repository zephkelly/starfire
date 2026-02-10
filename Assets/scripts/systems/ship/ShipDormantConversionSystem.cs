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
    public partial struct ShipDormantConversionSystem : ISystem
    {
        float _lastUpdateTime;

        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTime = -0.333f;
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

            foreach (var (fleetData, entity) in
                SystemAPI.Query<RefRO<FleetData>>()
                    .WithAll<FleetTag>()
                    .WithEntityAccess())
            {
                double2 delta = fleetData.ValueRO.Position - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq <= tier3MaxDistSq)
                    continue;

                var dormantEntity = ecb.CreateEntity();
                ecb.AddComponent(dormantEntity, new DormantTag());
                ecb.AddComponent(dormantEntity, new DormantRecord
                {
                    ChunkX = (long)(fleetData.ValueRO.Position.x / 1000.0),
                    ChunkY = (long)(fleetData.ValueRO.Position.y / 1000.0),
                    EntityType = (byte)Starfire.Entity.EntityType.Ship,
                    FactionIndex = fleetData.ValueRO.FactionIndex,
                    Count = fleetData.ValueRO.MemberCount,
                    Seed = (uint)(fleetData.ValueRO.FleetId * 31 + 17),
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

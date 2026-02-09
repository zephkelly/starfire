using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(Constrain2DSystem))]
    [BurstCompile]
    public partial struct WorldPositionSyncSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldOrigin>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var origin = SystemAPI.GetSingleton<WorldOrigin>();

            new SyncJob
            {
                OriginValue = origin.Value
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct SyncJob : IJobEntity
        {
            public double2 OriginValue;

            void Execute(ref WorldPosition worldPos, in LocalTransform transform, in SimulationTierData tier)
            {
                if (tier.Tier <= SimulationTier.Active)
                {
                    worldPos.Value = OriginValue + new double2(transform.Position.x, transform.Position.y);
                }
            }
        }
    }
}

using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Starfire.Entity;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(WorldPositionSyncSystem))]
    [BurstCompile]
    public partial struct WorldBoundsWrapSystem : ISystem
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

            new WrapJob
            {
                OriginValue = origin.Value,
                BoundsRadius = origin.WorldBoundsRadius
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct WrapJob : IJobEntity
        {
            public double2 OriginValue;
            public float BoundsRadius;

            void Execute(ref WorldPosition worldPos, ref LocalTransform transform)
            {
                double radius = BoundsRadius;
                double diameter = radius * 2.0;
                var pos = worldPos.Value;
                bool wrapped = false;

                if (pos.x > radius) { pos.x -= diameter; wrapped = true; }
                else if (pos.x < -radius) { pos.x += diameter; wrapped = true; }

                if (pos.y > radius) { pos.y -= diameter; wrapped = true; }
                else if (pos.y < -radius) { pos.y += diameter; wrapped = true; }

                if (wrapped)
                {
                    worldPos.Value = pos;
                    var local = (float2)(pos - OriginValue);
                    transform.Position = new float3(local.x, local.y, 0f);
                }
            }
        }
    }
}

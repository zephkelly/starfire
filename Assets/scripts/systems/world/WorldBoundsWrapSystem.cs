using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Starfire.Entity;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
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
                BoundsRadius = origin.WorldBoundsRadius
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct WrapJob : IJobEntity
        {
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
                    var local = (float2)pos;
                    transform.Position = new float3(local.x, local.y, 0f);
                }
            }
        }
    }
}

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct StarGravitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StarTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int starCount = 0;
            foreach (var _ in SystemAPI.Query<RefRO<StarData>>().WithAll<StarTag>())
                starCount++;

            if (starCount == 0) return;

            var starData = new NativeArray<StarGravityData>(starCount, Allocator.TempJob);

            int index = 0;
            foreach (var (star, transform) in
                SystemAPI.Query<RefRO<StarData>, RefRO<LocalTransform>>()
                    .WithAll<StarTag>())
            {
                var pos = transform.ValueRO.Position;
                starData[index++] = new StarGravityData
                {
                    Position = new float2(pos.x, pos.y),
                    GravityStrength = star.ValueRO.GravityStrength,
                    GravityRangeSq = star.ValueRO.GravityRange * star.ValueRO.GravityRange,
                    RadiusSq = star.ValueRO.Radius * star.ValueRO.Radius
                };
            }

            new GravityJob
            {
                Stars = starData,
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }

    struct StarGravityData
    {
        public float2 Position;
        public float GravityStrength;
        public float GravityRangeSq;
        public float RadiusSq;
    }

    [BurstCompile]
    [WithAll(typeof(RichTierTag), typeof(Simulate))]
    partial struct GravityJob : IJobEntity
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<StarGravityData> Stars;
        public float DeltaTime;

        void Execute(in LocalTransform transform, ref PhysicsVelocity velocity)
        {
            float2 entityPos = transform.Position.xy;

            for (int i = 0; i < Stars.Length; i++)
            {
                var star = Stars[i];
                float2 delta = star.Position - entityPos;
                float distSq = delta.x * delta.x + delta.y * delta.y;

                if (distSq > star.GravityRangeSq || distSq < math.max(star.RadiusSq, 1f))
                    continue;

                float invDist = math.rsqrt(distSq);
                float acceleration = star.GravityStrength * invDist * invDist;
                float2 direction = delta * invDist;

                velocity.Linear.x += direction.x * acceleration * DeltaTime;
                velocity.Linear.y += direction.y * acceleration * DeltaTime;
            }
        }
    }
}

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Starfire.Entity;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct AsteroidSensorOrbitSystem : ISystem
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

            var starData = new NativeArray<StarOrbitInfo>(starCount, Allocator.TempJob);

            int index = 0;
            foreach (var (star, identity, worldPos) in
                SystemAPI.Query<RefRO<StarData>, RefRO<EntityIdentity>, RefRO<WorldPosition>>()
                    .WithAll<StarTag>())
            {
                starData[index++] = new StarOrbitInfo
                {
                    ConfigId = identity.ValueRO.ConfigId,
                    Position = worldPos.ValueRO.Value,
                    GravityStrength = star.ValueRO.GravityStrength
                };
            }

            new SensorOrbitJob
            {
                Stars = starData,
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }

    struct StarOrbitInfo
    {
        public int ConfigId;
        public double2 Position;
        public float GravityStrength;
    }

    [BurstCompile]
    [WithAll(typeof(AsteroidTag), typeof(SensorTierTag))]
    partial struct SensorOrbitJob : IJobEntity
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<StarOrbitInfo> Stars;
        public float DeltaTime;

        void Execute(ref WorldPosition worldPos, ref LocalTransform transform, ref AsteroidData asteroid)
        {
            int parentId = asteroid.ParentStarId;
            StarOrbitInfo star = default;
            bool found = false;
            for (int i = 0; i < Stars.Length; i++)
            {
                if (Stars[i].ConfigId == parentId)
                {
                    star = Stars[i];
                    found = true;
                    break;
                }
            }
            if (!found) return;

            double2 radial = worldPos.Value - star.Position;
            double distSq = radial.x * radial.x + radial.y * radial.y;
            if (distSq < 1.0) return;

            float2 vel = asteroid.OrbitalVelocity;
            double cross = radial.x * vel.y - radial.y * vel.x;
            double omega = cross / distSq;
            double angle = omega * DeltaTime;

            double cosA = math.cos(angle);
            double sinA = math.sin(angle);

            double2 rotatedRadial = new double2(
                radial.x * cosA - radial.y * sinA,
                radial.x * sinA + radial.y * cosA
            );
            worldPos.Value = star.Position + rotatedRadial;

            float cosAf = (float)cosA;
            float sinAf = (float)sinA;
            asteroid.OrbitalVelocity = new float2(
                vel.x * cosAf - vel.y * sinAf,
                vel.x * sinAf + vel.y * cosAf
            );

            float2 localPos = (float2)worldPos.Value;
            transform.Position = new float3(localPos.x, localPos.y, 0f);
        }
    }
}

using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct AsteroidDriftSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new AsteroidDriftJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(RichTierTag), typeof(AsteroidTag))]
        partial struct AsteroidDriftJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(in AsteroidData asteroid, ref PhysicsVelocity velocity)
            {
                if (asteroid.DriftSpeed <= 0f && asteroid.AngularSpeed <= 0f)
                    return;

                if (asteroid.DriftSpeed > 0f)
                {
                    velocity.Linear = new float3(
                        asteroid.DriftDirection.x * asteroid.DriftSpeed,
                        asteroid.DriftDirection.y * asteroid.DriftSpeed,
                        0f);
                }

                if (asteroid.AngularSpeed > 0f)
                {
                    velocity.Angular = new float3(0f, 0f, math.radians(asteroid.AngularSpeed));
                }
            }
        }
    }
}

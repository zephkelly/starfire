using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Starfire.Core;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct ShipThrustSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            new ThrustJob
            {
                DeltaTime = dt
            }.ScheduleParallel();
        }

        [BurstCompile]
    [WithAll(typeof(RichTierTag), typeof(Simulate))]
        partial struct ThrustJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(in ControlInput input, in ShipPropulsion propulsion, ref PhysicsVelocity velocity,
                in ShipTag tag)
            {
                if (input.Throttle <= 0f || math.lengthsq(input.MovementDirection) < 0.001f)
                    return;

                if (propulsion.IsEnabled == 0)
                    return;

                var dir = math.normalizesafe(new float3(input.MovementDirection, 0f));
                velocity.Linear += dir * propulsion.Acceleration * input.Throttle * DeltaTime;
            }
        }
    }
}

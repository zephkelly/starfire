using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(Constrain2DSystem))]
    [BurstCompile]
    public partial struct SpeedLimitSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SpeedLimitJob().ScheduleParallel();
        }

        [BurstCompile]
    [WithAll(typeof(RichTierTag), typeof(Simulate))]
        partial struct SpeedLimitJob : IJobEntity
        {
            void Execute(in ShipPropulsion propulsion, ref PhysicsVelocity velocity, in ShipTag tag)
            {
                float speed = math.length(velocity.Linear.xy);

                if (speed > propulsion.MaxSpeed && speed > 0f)
                {
                    float scale = propulsion.MaxSpeed / speed;
                    velocity.Linear = new float3(velocity.Linear.x * scale, velocity.Linear.y * scale, 0f);
                }
            }
        }
    }
}

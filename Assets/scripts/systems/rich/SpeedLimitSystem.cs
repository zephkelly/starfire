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
    [UpdateAfter(typeof(PhysicsSystemGroup))]
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
        [WithAll(typeof(RichTierTag))]
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

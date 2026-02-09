using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct Constrain2DSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Constrain2DJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(RichTierTag))]
        partial struct Constrain2DJob : IJobEntity
        {
            void Execute(ref LocalTransform transform, ref PhysicsVelocity velocity)
            {
                var pos = transform.Position;
                pos.z = 0f;
                transform.Position = pos;

                var linear = velocity.Linear;
                linear.z = 0f;
                velocity.Linear = linear;

                var angular = velocity.Angular;
                angular.x = 0f;
                angular.y = 0f;
                velocity.Angular = angular;
            }
        }
    }
}

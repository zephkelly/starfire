using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct Constrain2DSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Constrain2DJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(RichTierTag), typeof(Simulate))]
        partial struct Constrain2DJob : IJobEntity
        {
            void Execute(ref LocalTransform transform, ref PhysicsVelocity velocity)
            {
                var pos = transform.Position;
                pos.z = 0f;
                transform.Position = pos;

                var q = transform.Rotation;
                float zwLenSq = q.value.z * q.value.z + q.value.w * q.value.w;
                transform.Rotation = zwLenSq > math.FLT_MIN_NORMAL
                    ? math.normalize(new quaternion(0f, 0f, q.value.z, q.value.w))
                    : quaternion.identity;

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

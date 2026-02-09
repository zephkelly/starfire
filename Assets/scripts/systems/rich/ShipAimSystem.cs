using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Starfire.Core;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(ShipThrustSystem))]
    [BurstCompile]
    public partial struct ShipAimSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            new AimJob
            {
                DeltaTime = dt
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct AimJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(in ControlInput input, ref ShipRotation rotation, ref PhysicsVelocity velocity,
                in LocalTransform transform, in ShipTag tag, EnabledRefRO<RichTierTag> richEnabled)
            {
                if (math.lengthsq(input.AimDirection) < 0.001f)
                    return;

                if (rotation.IsEnabled == 0)
                    return;

                float targetAngle = math.atan2(input.AimDirection.y, input.AimDirection.x);

                var q = transform.Rotation;
                float currentAngle = math.atan2(
                    2f * (q.value.w * q.value.z + q.value.x * q.value.y),
                    1f - 2f * (q.value.y * q.value.y + q.value.z * q.value.z));

                float delta = targetAngle - currentAngle;

                if (delta > math.PI) delta -= 2f * math.PI;
                else if (delta < -math.PI) delta += 2f * math.PI;

                float maxTurn = math.radians(rotation.TurnRate) * DeltaTime;
                float clampedDelta = math.clamp(delta, -maxTurn, maxTurn);

                velocity.Angular = new float3(0f, 0f, clampedDelta / DeltaTime);

                rotation.CurrentHeading = math.degrees(currentAngle);
                rotation.TargetHeading = math.degrees(targetAngle);
                rotation.AngularVelocity = math.degrees(clampedDelta / DeltaTime);
            }
        }
    }
}

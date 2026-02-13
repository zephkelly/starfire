using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Starfire.Core;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct ClientLocalPhysicsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LocalEntityTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            state.Dependency.Complete();

            int starCount = 0;
            foreach (var _ in SystemAPI.Query<RefRO<StarData>>().WithAll<StarTag, LocalEntityTag>())
                starCount++;

            var starData = new NativeArray<LocalStarGravityData>(starCount, Allocator.TempJob);
            if (starCount > 0)
            {
                int index = 0;
                foreach (var (star, transform) in
                    SystemAPI.Query<RefRO<StarData>, RefRO<LocalTransform>>()
                        .WithAll<StarTag, LocalEntityTag>())
                {
                    var pos = transform.ValueRO.Position;
                    starData[index++] = new LocalStarGravityData
                    {
                        Position = new float2(pos.x, pos.y),
                        GravityStrength = star.ValueRO.GravityStrength,
                        GravityRangeSq = star.ValueRO.GravityRange * star.ValueRO.GravityRange,
                        RadiusSq = star.ValueRO.Radius * star.ValueRO.Radius
                    };
                }
            }

            new LocalThrustJob { DeltaTime = dt }.ScheduleParallel();
            new LocalAimJob { DeltaTime = dt }.ScheduleParallel();

            if (starCount > 0)
                new LocalGravityJob { Stars = starData, DeltaTime = dt }.ScheduleParallel();
            else
                starData.Dispose();

            new LocalSpeedLimitJob().ScheduleParallel();
            new LocalVelocityIntegrationJob { DeltaTime = dt }.ScheduleParallel();
            new LocalConstrain2DJob().ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithAll(typeof(RichTierTag), typeof(LocalEntityTag))]
    partial struct LocalThrustJob : IJobEntity
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

    [BurstCompile]
    [WithAll(typeof(RichTierTag), typeof(LocalEntityTag))]
    partial struct LocalAimJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(in ControlInput input, ref ShipRotation rotation, ref PhysicsVelocity velocity,
            in LocalTransform transform, in ShipTag tag)
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

    [BurstCompile]
    [WithAll(typeof(RichTierTag), typeof(LocalEntityTag))]
    partial struct LocalVelocityIntegrationJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(ref LocalTransform transform, in PhysicsVelocity velocity, in PhysicsDamping damping)
        {
            float3 linear = velocity.Linear;
            float dampFactor = math.max(0f, 1f - damping.Linear * DeltaTime);
            linear *= dampFactor;

            transform.Position += linear * DeltaTime;

            float angularZ = velocity.Angular.z;
            if (math.abs(angularZ) > 0.001f)
            {
                float rotAngle = angularZ * DeltaTime;
                transform.Rotation = math.mul(transform.Rotation, quaternion.RotateZ(rotAngle));
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(RichTierTag), typeof(LocalEntityTag))]
    partial struct LocalConstrain2DJob : IJobEntity
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

    [BurstCompile]
    [WithAll(typeof(RichTierTag), typeof(LocalEntityTag))]
    partial struct LocalSpeedLimitJob : IJobEntity
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

    struct LocalStarGravityData
    {
        public float2 Position;
        public float GravityStrength;
        public float GravityRangeSq;
        public float RadiusSq;
    }

    [BurstCompile]
    [WithAll(typeof(RichTierTag), typeof(LocalEntityTag))]
    partial struct LocalGravityJob : IJobEntity
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<LocalStarGravityData> Stars;
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

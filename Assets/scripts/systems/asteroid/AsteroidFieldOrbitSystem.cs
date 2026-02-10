using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WorldPositionSyncSystem))]
    [UpdateAfter(typeof(FloatingOriginSystem))]
    [BurstCompile]
    public partial struct AsteroidFieldOrbitSystem : ISystem
    {
        float _timeSinceLastUpdate;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StarTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            _timeSinceLastUpdate += dt;
            if (_timeSinceLastUpdate < 0.25f) return;

            float accumulatedDt = _timeSinceLastUpdate;
            _timeSinceLastUpdate = 0f;

            int starCount = 0;
            foreach (var _ in SystemAPI.Query<RefRO<StarData>>().WithAll<StarTag>())
                starCount++;

            if (starCount == 0) return;

            var stars = new NativeArray<StarOrbitInfo>(starCount, Allocator.Temp);

            int index = 0;
            foreach (var (star, identity, worldPos) in
                SystemAPI.Query<RefRO<StarData>, RefRO<EntityIdentity>, RefRO<WorldPosition>>()
                    .WithAll<StarTag>())
            {
                stars[index++] = new StarOrbitInfo
                {
                    ConfigId = identity.ValueRO.ConfigId,
                    Position = worldPos.ValueRO.Value,
                    GravityStrength = star.ValueRO.GravityStrength
                };
            }

            foreach (var (fieldData, entity) in
                SystemAPI.Query<RefRW<AsteroidFieldData>>()
                    .WithAll<AsteroidFieldTag>()
                    .WithEntityAccess())
            {
                int parentId = fieldData.ValueRO.ParentStarId;
                if (parentId < 0) continue;

                StarOrbitInfo star = default;
                bool found = false;
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i].ConfigId == parentId)
                    {
                        star = stars[i];
                        found = true;
                        break;
                    }
                }
                if (!found) continue;

                double2 fieldRadial = fieldData.ValueRO.Position - star.Position;
                double fieldDistSq = fieldRadial.x * fieldRadial.x + fieldRadial.y * fieldRadial.y;
                if (fieldDistSq < 1.0) continue;

                var buffer = SystemAPI.GetBuffer<AsteroidFieldMember>(entity);

                double orbitSign = 1.0;
                if (buffer.Length > 0)
                {
                    var first = buffer[0];
                    double2 firstRadial = first.Position - star.Position;
                    float2 firstVel = first.OrbitalVelocity;
                    double cross = firstRadial.x * firstVel.y - firstRadial.y * firstVel.x;
                    orbitSign = cross >= 0 ? 1.0 : -1.0;
                }

                double fieldDist = math.sqrt(fieldDistSq);
                double fieldOmega = orbitSign * math.sqrt(star.GravityStrength / (fieldDist * fieldDistSq));
                double angle = fieldOmega * accumulatedDt;

                double cosA = math.cos(angle);
                double sinA = math.sin(angle);

                double2 rotatedFieldRadial = new double2(
                    fieldRadial.x * cosA - fieldRadial.y * sinA,
                    fieldRadial.x * sinA + fieldRadial.y * cosA
                );
                fieldData.ValueRW.Position = star.Position + rotatedFieldRadial;

                float cosAf = (float)cosA;
                float sinAf = (float)sinA;

                for (int m = 0; m < buffer.Length; m++)
                {
                    var member = buffer[m];

                    double2 radial = member.Position - star.Position;
                    member.Position = star.Position + new double2(
                        radial.x * cosA - radial.y * sinA,
                        radial.x * sinA + radial.y * cosA
                    );

                    float2 vel = member.OrbitalVelocity;
                    member.OrbitalVelocity = new float2(
                        vel.x * cosAf - vel.y * sinAf,
                        vel.x * sinAf + vel.y * cosAf
                    );

                    buffer[m] = member;
                }
            }

            stars.Dispose();
        }
    }
}

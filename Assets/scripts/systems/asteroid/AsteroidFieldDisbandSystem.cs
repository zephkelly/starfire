using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierTransitionCleanupSystem))]
    public partial struct AsteroidFieldDisbandSystem : ISystem
    {
        const int MaxFieldsPerTick = 3;

        EntityArchetype _asteroidArchetype;
        bool _archetypeCreated;
        float _lastUpdateTime;

        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTime = -0.5f;
            state.RequireForUpdate<SimulationConfig>();
            state.RequireForUpdate<PlayerTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            if (elapsedTime - _lastUpdateTime < 1.0f)
                return;
            _lastUpdateTime = elapsedTime;

            if (!_archetypeCreated)
            {
                _asteroidArchetype = state.EntityManager.CreateArchetype(
                    typeof(LocalTransform),
                    typeof(LocalToWorld),
                    typeof(WorldPosition),
                    typeof(EntityIdentity),
                    typeof(SimulationTierData),
                    typeof(TierTransition),
                    typeof(AsteroidTag),
                    typeof(AsteroidData),
                    typeof(SensorContact),
                    typeof(RichTierTag),
                    typeof(VisualTierTag),
                    typeof(SensorTierTag),
                    typeof(PhysicsCollider),
                    typeof(PhysicsMass),
                    typeof(PhysicsVelocity),
                    typeof(PhysicsDamping),
                    typeof(PhysicsGravityFactor),
                    typeof(PhysicsWorldIndex),
                    typeof(DisableRendering));
                _archetypeCreated = true;
            }

            var config = SystemAPI.GetSingleton<SimulationConfig>();
            var origin = SystemAPI.GetSingleton<WorldOrigin>();

            double2 playerWorldPos = double2.zero;
            foreach (var (worldPos, _) in SystemAPI.Query<RefRO<WorldPosition>, RefRO<PlayerTag>>())
            {
                playerWorldPos = worldPos.ValueRO.Value;
                break;
            }

            double threshold = config.Bounds.Tier2MaxDistance - config.Bounds.Tier2Hysteresis;
            double thresholdSq = threshold * threshold;
            int fieldsProcessed = 0;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (fieldData, entity) in
                SystemAPI.Query<RefRO<AsteroidFieldData>>()
                    .WithAll<AsteroidFieldTag>()
                    .WithEntityAccess())
            {
                if (fieldsProcessed >= MaxFieldsPerTick)
                    break;

                double2 delta = fieldData.ValueRO.Position - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq >= thresholdSq)
                    continue;

                var buffer = state.EntityManager.GetBuffer<AsteroidFieldMember>(entity);

                for (int i = 0; i < buffer.Length; i++)
                {
                    var member = buffer[i];
                    var localPos = (float2)(member.Position - origin.Value);

                    var asteroidEntity = ecb.CreateEntity(_asteroidArchetype);

                    ecb.SetComponent(asteroidEntity, LocalTransform.FromPositionRotation(
                        new float3(localPos.x, localPos.y, 0f), quaternion.identity));

                    ecb.SetComponent(asteroidEntity, new WorldPosition { Value = member.Position });

                    ecb.SetComponent(asteroidEntity, new EntityIdentity
                    {
                        Id = member.EntityId,
                        EntityType = (byte)Starfire.Entity.EntityType.Asteroid,
                        Persistence = 0,
                        FactionId = 0,
                        ConfigId = 0
                    });

                    ecb.SetComponent(asteroidEntity, new SimulationTierData
                    {
                        Tier = SimulationTier.Sensor,
                        LastUpdatedTime = elapsedTime
                    });

                    ecb.SetComponent(asteroidEntity, new AsteroidData
                    {
                        Size = member.Size,
                        Composition = member.Composition,
                        ParentStarId = fieldData.ValueRO.ParentStarId,
                        OrbitalVelocity = member.OrbitalVelocity
                    });

                    ecb.SetComponent(asteroidEntity, new SensorContact
                    {
                        HullPercent = 1f,
                        SensorRange = 0f
                    });

                    ecb.SetComponent(asteroidEntity, new PhysicsDamping { Linear = 0f, Angular = 0f });
                    ecb.SetComponent(asteroidEntity, new PhysicsGravityFactor { Value = 0f });

                    ecb.SetComponentEnabled<RichTierTag>(asteroidEntity, false);
                    ecb.SetComponentEnabled<VisualTierTag>(asteroidEntity, false);
                    ecb.SetComponentEnabled<SensorTierTag>(asteroidEntity, true);
                    ecb.SetComponentEnabled<TierTransition>(asteroidEntity, false);
                    ecb.RemoveComponent<PhysicsWorldIndex>(asteroidEntity);
                }

                ecb.DestroyEntity(entity);
                fieldsProcessed++;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

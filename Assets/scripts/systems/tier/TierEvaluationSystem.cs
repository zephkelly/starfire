using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct TierEvaluationSystem : ISystem
    {
        const int MaxChangesPerFrame = 8;

        EntityQuery _evalQuery;
        NativeQueue<TierChange> _pendingChanges;
        bool _queueCreated;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationConfig>();
            state.RequireForUpdate<WorldOrigin>();
            state.RequireForUpdate<PlayerTag>();

            _evalQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<SimulationTierData>(),
                    ComponentType.ReadOnly<WorldPosition>(),
                    ComponentType.ReadOnly<EntityIdentity>(),
                    ComponentType.ReadOnly<SensorContact>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<PlayerTag>()
                }
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_queueCreated)
            {
                _pendingChanges = new NativeQueue<TierChange>(Allocator.Persistent);
                _queueCreated = true;
            }

            var config = SystemAPI.GetSingleton<SimulationConfig>();
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;

            double2 playerWorldPos = double2.zero;
            float playerSensorRange = config.Sensor.DefaultRange;
            foreach (var (worldPos, sensor, _) in
                SystemAPI.Query<RefRO<WorldPosition>, RefRO<SensorContact>, RefRO<PlayerTag>>())
            {
                playerWorldPos = worldPos.ValueRO.Value;
                playerSensorRange = sensor.ValueRO.SensorRange > 0f
                    ? sensor.ValueRO.SensorRange
                    : config.Sensor.DefaultRange;
                break;
            }

            var entities = _evalQuery.ToEntityArray(Allocator.TempJob);

            float hysteresisPercent = config.Bounds.HysteresisPercent > 0f ? config.Bounds.HysteresisPercent : 0.15f;
            float t0Hysteresis = config.Bounds.Tier0MaxDistance * hysteresisPercent;
            double tier0Inner = config.Bounds.Tier0MaxDistance - t0Hysteresis;

            float asteroidRefSize = 2f;
            float asteroidMinFactor = 0.3f;
            if (SystemAPI.TryGetSingleton<AsteroidConfig>(out var asteroidConfig))
            {
                asteroidRefSize = asteroidConfig.ReferenceSize > 0f ? asteroidConfig.ReferenceSize : 2f;
                asteroidMinFactor = asteroidConfig.MinSizeFactor;
            }

            state.Dependency = new EvaluateTierJob
            {
                Entities = entities,
                PlayerWorldPos = playerWorldPos,
                PlayerSensorRange = playerSensorRange,
                DefaultSensorRange = config.Sensor.DefaultRange,
                EngagementBuffer = config.Sensor.EngagementBuffer,
                Tier0MaxDistanceSq = (double)config.Bounds.Tier0MaxDistance * config.Bounds.Tier0MaxDistance,
                Tier0InnerThresholdSq = tier0Inner * tier0Inner,
                Tier1MaxDistance = config.Bounds.Tier1MaxDistance,
                HysteresisPercent = hysteresisPercent,
                TierChangeCooldown = config.Bounds.TierChangeCooldown,
                ElapsedTime = elapsedTime,
                AsteroidDataLookup = SystemAPI.GetComponentLookup<AsteroidData>(true),
                AsteroidReferenceSize = asteroidRefSize,
                AsteroidMinSizeFactor = asteroidMinFactor,
                ChangedEntities = _pendingChanges.AsParallelWriter()
            }.ScheduleParallel(_evalQuery, state.Dependency);

            state.Dependency.Complete();
            entities.Dispose();

            var tierDataLookup = SystemAPI.GetComponentLookup<SimulationTierData>(false);
            var transitionLookup = SystemAPI.GetComponentLookup<TierTransition>(false);

            int applied = 0;
            while (applied < MaxChangesPerFrame && _pendingChanges.TryDequeue(out var change))
            {
                if (!state.EntityManager.Exists(change.Entity))
                    continue;

                var tierData = tierDataLookup[change.Entity];
                var previousTier = tierData.Tier;

                transitionLookup[change.Entity] = new TierTransition
                {
                    PreviousTier = previousTier,
                    NewTier = change.NewTier
                };
                state.EntityManager.SetComponentEnabled<TierTransition>(change.Entity, true);

                tierData.Tier = change.NewTier;
                tierData.LastUpdatedTime = elapsedTime;
                tierDataLookup[change.Entity] = tierData;

                applied++;
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_queueCreated)
                _pendingChanges.Dispose();
        }

        struct TierChange
        {
            public Unity.Entities.Entity Entity;
            public SimulationTier NewTier;
        }

        [BurstCompile]
        partial struct EvaluateTierJob : IJobEntity
        {
            [ReadOnly] public NativeArray<Unity.Entities.Entity> Entities;

            public double2 PlayerWorldPos;
            public float PlayerSensorRange;
            public float DefaultSensorRange;
            public float EngagementBuffer;
            public double Tier0MaxDistanceSq;
            public double Tier0InnerThresholdSq;
            public float Tier1MaxDistance;
            public float HysteresisPercent;
            public float TierChangeCooldown;
            public float ElapsedTime;

            [ReadOnly] public ComponentLookup<AsteroidData> AsteroidDataLookup;
            public float AsteroidReferenceSize;
            public float AsteroidMinSizeFactor;

            public NativeQueue<TierChange>.ParallelWriter ChangedEntities;

            void Execute(
                [EntityIndexInQuery] int entityIndex,
                ref SimulationTierData tierData,
                in WorldPosition worldPos,
                in EntityIdentity identity,
                in SensorContact sensor)
            {
                if (ElapsedTime - tierData.LastUpdatedTime < TierChangeCooldown)
                    return;

                double2 delta = worldPos.Value - PlayerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;

                var entityRef = Entities[entityIndex];
                if (AsteroidDataLookup.HasComponent(entityRef))
                {
                    float size = AsteroidDataLookup[entityRef].Size;
                    float sizeFactor = math.clamp(size / AsteroidReferenceSize, AsteroidMinSizeFactor, 1f);
                    distSq = distSq / ((double)sizeFactor * sizeFactor);
                }

                var currentTier = tierData.Tier;
                var targetTier = currentTier;

                float entityRange = sensor.SensorRange > 0f ? sensor.SensorRange : DefaultSensorRange;
                float maxDetectionRange = math.max(PlayerSensorRange, entityRange);
                float effectiveT1Boundary = math.min(maxDetectionRange + EngagementBuffer, Tier1MaxDistance);
                float t1Hysteresis = effectiveT1Boundary * HysteresisPercent;
                double t1BoundarySq = (double)effectiveT1Boundary * effectiveT1Boundary;
                double t1Inner = effectiveT1Boundary - t1Hysteresis;
                double t1InnerSq = t1Inner * t1Inner;

                if (currentTier == SimulationTier.Loaded && distSq > Tier0MaxDistanceSq)
                    targetTier = SimulationTier.Active;
                else if (currentTier == SimulationTier.Active && distSq < Tier0InnerThresholdSq)
                    targetTier = SimulationTier.Loaded;
                else if (currentTier == SimulationTier.Active && distSq > t1BoundarySq)
                    targetTier = SimulationTier.Sensor;
                else if (currentTier == SimulationTier.Sensor && distSq < t1InnerSq)
                    targetTier = SimulationTier.Active;

                byte persistence = identity.Persistence;
                if (persistence == 2 && targetTier > SimulationTier.Sensor)
                    targetTier = SimulationTier.Sensor;

                if (targetTier == currentTier)
                    return;

                tierData.LastUpdatedTime = ElapsedTime;

                ChangedEntities.Enqueue(new TierChange
                {
                    Entity = Entities[entityIndex],
                    NewTier = targetTier
                });
            }
        }
    }
}

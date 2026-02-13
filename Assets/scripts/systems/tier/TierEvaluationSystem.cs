using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Starfire.Entity;
using Starfire.Network;
using Starfire.Sim;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct TierEvaluationSystem : ISystem
    {
        const int MaxChangesPerFrame = 8;
        const float PlayerProximityThreshold = 20000f;

        EntityQuery _evalQuery;
        EntityQuery _bufferQuery;
        NativeQueue<TierChange> _pendingChanges;
        bool _queueCreated;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationConfig>();
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

            _bufferQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerPositionElement>());
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

            NativeArray<double2> playerPositions;
            NativeArray<float> playerSensorRanges;

            if (!_bufferQuery.IsEmpty)
            {
                var buffer = SystemAPI.GetSingletonBuffer<PlayerPositionElement>(true);
                int count = buffer.Length;
                playerPositions = new NativeArray<double2>(count, Allocator.TempJob);
                playerSensorRanges = new NativeArray<float>(count, Allocator.TempJob);
                for (int i = 0; i < count; i++)
                {
                    playerPositions[i] = buffer[i].Position;
                    playerSensorRanges[i] = buffer[i].SensorRange;
                }
            }
            else
            {
                var posList = new NativeList<double2>(4, Allocator.Temp);
                var rangeList = new NativeList<float>(4, Allocator.Temp);
                foreach (var (worldPos, sensor, _) in
                    SystemAPI.Query<RefRO<WorldPosition>, RefRO<SensorContact>, RefRO<PlayerTag>>())
                {
                    posList.Add(worldPos.ValueRO.Value);
                    rangeList.Add(sensor.ValueRO.SensorRange > 0f
                        ? sensor.ValueRO.SensorRange
                        : config.Sensor.DefaultRange);
                }

                playerPositions = new NativeArray<double2>(posList.Length, Allocator.TempJob);
                playerSensorRanges = new NativeArray<float>(posList.Length, Allocator.TempJob);
                for (int i = 0; i < posList.Length; i++)
                {
                    playerPositions[i] = posList[i];
                    playerSensorRanges[i] = rangeList[i];
                }
                posList.Dispose();
                rangeList.Dispose();
            }

            if (playerPositions.Length == 0)
            {
                playerPositions.Dispose();
                playerSensorRanges.Dispose();
                return;
            }

            float t0 = config.Bounds.Tier0MaxDistance;
            float hysteresisPercent = config.Bounds.HysteresisPercent > 0f ? config.Bounds.HysteresisPercent : 0.15f;
            float t0Hysteresis = t0 * hysteresisPercent;
            double tier0Inner = t0 - t0Hysteresis;

            var zoneList = new NativeList<AuthorityZone>(6, Allocator.Temp);
            for (int a = 0; a < playerPositions.Length; a++)
            {
                for (int b = a + 1; b < playerPositions.Length; b++)
                {
                    double2 delta = playerPositions[a] - playerPositions[b];
                    double interDist = math.sqrt(delta.x * delta.x + delta.y * delta.y);

                    if (interDist < PlayerProximityThreshold)
                    {
                        double expandedRadius = interDist * 0.5 + t0;
                        double expandedInner = interDist * 0.5 + (t0 - t0Hysteresis);

                        zoneList.Add(new AuthorityZone
                        {
                            Center = (playerPositions[a] + playerPositions[b]) * 0.5,
                            RadiusSq = expandedRadius * expandedRadius,
                            InnerRadiusSq = expandedInner * expandedInner
                        });
                    }
                }
            }

            var authorityZones = new NativeArray<AuthorityZone>(zoneList.Length, Allocator.TempJob);
            for (int i = 0; i < zoneList.Length; i++)
                authorityZones[i] = zoneList[i];
            zoneList.Dispose();

            var entities = _evalQuery.ToEntityArray(Allocator.TempJob);

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
                PlayerPositions = playerPositions,
                PlayerSensorRanges = playerSensorRanges,
                AuthorityZones = authorityZones,
                DefaultSensorRange = config.Sensor.DefaultRange,
                EngagementBuffer = config.Sensor.EngagementBuffer,
                Tier0MaxDistanceSq = (double)t0 * t0,
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

        struct AuthorityZone
        {
            public double2 Center;
            public double RadiusSq;
            public double InnerRadiusSq;
        }

        [BurstCompile]
        partial struct EvaluateTierJob : IJobEntity
        {
            [ReadOnly] public NativeArray<Unity.Entities.Entity> Entities;

            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<double2> PlayerPositions;
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<float> PlayerSensorRanges;
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<AuthorityZone> AuthorityZones;
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

                double minDistSq = double.MaxValue;
                float closestPlayerSensorRange = DefaultSensorRange;

                for (int p = 0; p < PlayerPositions.Length; p++)
                {
                    double2 delta = worldPos.Value - PlayerPositions[p];
                    double dSq = delta.x * delta.x + delta.y * delta.y;
                    if (dSq < minDistSq)
                    {
                        minDistSq = dSq;
                        closestPlayerSensorRange = PlayerSensorRanges[p];
                    }
                }

                bool inAuthorityZone = false;
                bool inAuthorityZoneInner = false;
                for (int z = 0; z < AuthorityZones.Length; z++)
                {
                    double2 delta = worldPos.Value - AuthorityZones[z].Center;
                    double dSq = delta.x * delta.x + delta.y * delta.y;
                    if (dSq < AuthorityZones[z].RadiusSq)
                    {
                        inAuthorityZone = true;
                        if (dSq < AuthorityZones[z].InnerRadiusSq)
                            inAuthorityZoneInner = true;
                        break;
                    }
                }

                double distSq = minDistSq;

                var entityRef = Entities[entityIndex];
                if (AsteroidDataLookup.HasComponent(entityRef))
                {
                    float size = AsteroidDataLookup[entityRef].Size;
                    float sizeFactor = math.clamp(size / AsteroidReferenceSize, AsteroidMinSizeFactor, 1f);
                    distSq = distSq / ((double)sizeFactor * sizeFactor);
                }

                var currentTier = tierData.Tier;
                var targetTier = currentTier;

                if (inAuthorityZoneInner && currentTier != SimulationTier.Loaded)
                {
                    targetTier = SimulationTier.Loaded;
                }
                else if (!inAuthorityZone)
                {
                    float entityRange = sensor.SensorRange > 0f ? sensor.SensorRange : DefaultSensorRange;
                    float maxDetectionRange = math.max(closestPlayerSensorRange, entityRange);
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
                }

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

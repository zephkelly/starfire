using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
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
                    ComponentType.ReadOnly<SensorContact>(),
                    ComponentType.ReadOnly<ShipTag>()
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
            float playerSensorRange = config.DefaultSensorRange;
            foreach (var (worldPos, sensor, _) in
                SystemAPI.Query<RefRO<WorldPosition>, RefRO<SensorContact>, RefRO<PlayerTag>>())
            {
                playerWorldPos = worldPos.ValueRO.Value;
                playerSensorRange = sensor.ValueRO.SensorRange > 0f
                    ? sensor.ValueRO.SensorRange
                    : config.DefaultSensorRange;
                break;
            }

            var entities = _evalQuery.ToEntityArray(Allocator.TempJob);

            float hysteresisPercent = config.HysteresisPercent > 0f ? config.HysteresisPercent : 0.15f;
            float t0Hysteresis = config.Tier0MaxDistance * hysteresisPercent;
            double tier0Inner = config.Tier0MaxDistance - t0Hysteresis;

            state.Dependency = new EvaluateTierJob
            {
                Entities = entities,
                PlayerWorldPos = playerWorldPos,
                PlayerSensorRange = playerSensorRange,
                DefaultSensorRange = config.DefaultSensorRange,
                EngagementBuffer = config.EngagementBuffer,
                Tier0MaxDistanceSq = (double)config.Tier0MaxDistance * config.Tier0MaxDistance,
                Tier0InnerThresholdSq = tier0Inner * tier0Inner,
                Tier1MaxDistance = config.Tier1MaxDistance,
                HysteresisPercent = hysteresisPercent,
                TierChangeCooldown = config.TierChangeCooldown,
                ElapsedTime = elapsedTime,
                ChangedEntities = _pendingChanges.AsParallelWriter()
            }.ScheduleParallel(_evalQuery, state.Dependency);

            state.Dependency.Complete();
            entities.Dispose();

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var hullLookup = SystemAPI.GetComponentLookup<ShipHull>(true);
            var propulsionLookup = SystemAPI.GetComponentLookup<ShipPropulsion>(true);
            var rotationLookup = SystemAPI.GetComponentLookup<ShipRotation>(true);
            var velLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(false);
            var worldPosLookup = SystemAPI.GetComponentLookup<WorldPosition>(true);
            var sensorLookup = SystemAPI.GetComponentLookup<SensorContact>(false);
            var snapshotLookup = SystemAPI.GetComponentLookup<ShipSnapshot>(false);

            int applied = 0;
            while (applied < MaxChangesPerFrame && _pendingChanges.TryDequeue(out var change))
            {
                if (!state.EntityManager.Exists(change.Entity))
                    continue;

                ApplyTierChange(ref state, change, ecb,
                    ref hullLookup, ref propulsionLookup, ref rotationLookup,
                    ref velLookup, ref worldPosLookup, ref sensorLookup, ref snapshotLookup);
                applied++;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_queueCreated)
                _pendingChanges.Dispose();
        }

        void ApplyTierChange(ref SystemState state, TierChange change, EntityCommandBuffer ecb,
            ref ComponentLookup<ShipHull> hullLookup,
            ref ComponentLookup<ShipPropulsion> propulsionLookup,
            ref ComponentLookup<ShipRotation> rotationLookup,
            ref ComponentLookup<PhysicsVelocity> velLookup,
            ref ComponentLookup<WorldPosition> worldPosLookup,
            ref ComponentLookup<SensorContact> sensorLookup,
            ref ComponentLookup<ShipSnapshot> snapshotLookup)
        {
            var entity = change.Entity;
            var em = state.EntityManager;

            switch (change.NewTier)
            {
                case SimulationTier.Loaded:
                    em.SetComponentEnabled<RichTierTag>(entity, true);
                    em.SetComponentEnabled<VisualTierTag>(entity, true);
                    em.SetComponentEnabled<SensorTierTag>(entity, false);
                    if (!em.HasComponent<PhysicsWorldIndex>(entity))
                        ecb.AddSharedComponent(entity, new PhysicsWorldIndex());
                    if (change.OldTier == SimulationTier.Sensor)
                        RestorePhysicsVelocity(entity, ref velLookup, ref snapshotLookup);
                    ecb.RemoveComponent<DisableRendering>(entity);
                    break;

                case SimulationTier.Active:
                    em.SetComponentEnabled<RichTierTag>(entity, true);
                    em.SetComponentEnabled<VisualTierTag>(entity, false);
                    em.SetComponentEnabled<SensorTierTag>(entity, false);
                    if (!em.HasComponent<PhysicsWorldIndex>(entity))
                        ecb.AddSharedComponent(entity, new PhysicsWorldIndex());
                    if (change.OldTier == SimulationTier.Sensor)
                        RestorePhysicsVelocity(entity, ref velLookup, ref snapshotLookup);
                    if (!em.HasComponent<DisableRendering>(entity))
                        ecb.AddComponent<DisableRendering>(entity);
                    break;

                case SimulationTier.Sensor:
                    em.SetComponentEnabled<RichTierTag>(entity, false);
                    em.SetComponentEnabled<VisualTierTag>(entity, false);
                    em.SetComponentEnabled<SensorTierTag>(entity, true);
                    if (em.HasComponent<PhysicsWorldIndex>(entity))
                        ecb.RemoveComponent<PhysicsWorldIndex>(entity);
                    if (!em.HasComponent<DisableRendering>(entity))
                        ecb.AddComponent<DisableRendering>(entity);

                    if (change.OldTier <= SimulationTier.Active)
                        SnapshotToSensor(entity,
                            ref hullLookup, ref propulsionLookup, ref rotationLookup,
                            ref velLookup, ref worldPosLookup, ref sensorLookup, ref snapshotLookup);
                    break;
            }
        }

        static void RestorePhysicsVelocity(Unity.Entities.Entity entity,
            ref ComponentLookup<PhysicsVelocity> velLookup,
            ref ComponentLookup<ShipSnapshot> snapshotLookup)
        {
            var snapshot = snapshotLookup[entity];
            var vel = velLookup[entity];
            vel.Linear = new float3((float)snapshot.Velocity.x, (float)snapshot.Velocity.y, 0f);
            vel.Angular = float3.zero;
            velLookup[entity] = vel;
        }

        static void SnapshotToSensor(Unity.Entities.Entity entity,
            ref ComponentLookup<ShipHull> hullLookup,
            ref ComponentLookup<ShipPropulsion> propulsionLookup,
            ref ComponentLookup<ShipRotation> rotationLookup,
            ref ComponentLookup<PhysicsVelocity> velLookup,
            ref ComponentLookup<WorldPosition> worldPosLookup,
            ref ComponentLookup<SensorContact> sensorLookup,
            ref ComponentLookup<ShipSnapshot> snapshotLookup)
        {
            var hull = hullLookup[entity];
            var propulsion = propulsionLookup[entity];
            var rotation = rotationLookup[entity];
            var vel = velLookup[entity];
            var worldPos = worldPosLookup[entity];

            var sensor = sensorLookup[entity];
            sensor.HullPercent = hull.CurrentHealth / math.max(hull.MaxHealth, 0.001f);
            sensor.MaxSpeed = propulsion.MaxSpeed;
            sensor.Speed = math.length(vel.Linear.xy);
            sensor.Heading = rotation.CurrentHeading;
            sensor.CurrentAIState = 0;
            sensor.StateTimer = 0f;
            sensorLookup[entity] = sensor;

            var snapshot = snapshotLookup[entity];
            snapshot.Position = worldPos.Value;
            snapshot.Velocity = new double2(vel.Linear.x, vel.Linear.y);
            snapshot.Heading = rotation.CurrentHeading;
            snapshot.HullPercent = sensor.HullPercent;
            snapshot.PropulsionEfficiency = propulsion.CurrentHealth / math.max(propulsion.MaxHealth, 0.001f);
            snapshot.RotationEfficiency = rotation.CurrentHealth / math.max(rotation.MaxHealth, 0.001f);
            snapshotLookup[entity] = snapshot;
        }

        struct TierChange
        {
            public Unity.Entities.Entity Entity;
            public SimulationTier OldTier;
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

            public NativeQueue<TierChange>.ParallelWriter ChangedEntities;

            void Execute(
                [Unity.Entities.EntityIndexInQuery] int entityIndex,
                ref SimulationTierData tierData,
                in WorldPosition worldPos,
                in EntityIdentity identity,
                in SensorContact sensor)
            {
                if (ElapsedTime - tierData.LastUpdatedTime < TierChangeCooldown)
                    return;

                double2 delta = worldPos.Value - PlayerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
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

                tierData.Tier = targetTier;
                tierData.LastUpdatedTime = ElapsedTime;

                ChangedEntities.Enqueue(new TierChange
                {
                    Entity = Entities[entityIndex],
                    OldTier = currentTier,
                    NewTier = targetTier
                });
            }
        }
    }
}

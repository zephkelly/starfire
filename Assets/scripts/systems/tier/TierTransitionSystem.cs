using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using Starfire.Core;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierEvaluationSystem))]
    public partial struct TierTransitionSystem : ISystem
    {
        const int MaxFleetsPerTick = 3;

        EntityArchetype _shipArchetype;
        bool _archetypeCreated;
        float _lastUpdateTime;
        int _tickCounter;

        public void OnCreate(ref SystemState state)
        {
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
                _shipArchetype = state.EntityManager.CreateArchetype(
                    typeof(LocalTransform),
                    typeof(LocalToWorld),
                    typeof(WorldPosition),
                    typeof(EntityIdentity),
                    typeof(SimulationTierData),
                    typeof(ControlInput),
                    typeof(ShipTag),
                    typeof(ShipHull),
                    typeof(ShipRotation),
                    typeof(ShipPropulsion),
                    typeof(ShipSnapshot),
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

            double2 playerWorldPos = double2.zero;
            foreach (var (worldPos, _) in SystemAPI.Query<RefRO<WorldPosition>, RefRO<PlayerTag>>())
            {
                playerWorldPos = worldPos.ValueRO.Value;
                break;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            int tick = _tickCounter % 4;
            _tickCounter++;

            switch (tick)
            {
                case 0:
                    ProcessT2ToT3(ref state, config, playerWorldPos, ecb);
                    break;
                case 1:
                    ProcessT3ToT2(ref state, config, playerWorldPos, ecb);
                    break;
                case 2:
                    ProcessT3ToT4(ref state, config, playerWorldPos, ecb);
                    break;
                case 3:
                    ProcessT4ToT3(ref state, config, playerWorldPos, ecb);
                    break;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        void ProcessT2ToT3(ref SystemState state, SimulationConfig config, double2 playerWorldPos, EntityCommandBuffer ecb)
        {
            double tier2MaxDistSq = (double)config.Tier2MaxDistance * config.Tier2MaxDistance;
            double fleetGroupRadiusSq = (double)config.FleetGroupingRadius * config.FleetGroupingRadius;
            double cellSize = config.FleetGroupingRadius;

            var candidates = new NativeList<CandidateInfo>(Allocator.Temp);

            foreach (var (tierData, worldPos, identity, snapshot, entity) in
                SystemAPI.Query<RefRO<SimulationTierData>, RefRO<WorldPosition>, RefRO<EntityIdentity>, RefRO<ShipSnapshot>>()
                    .WithAll<ShipTag, SensorTierTag>()
                    .WithNone<PlayerTag>()
                    .WithEntityAccess())
            {
                if (identity.ValueRO.Persistence == 2) continue;
                if (identity.ValueRO.Persistence == 1) continue;

                double2 delta = worldPos.ValueRO.Value - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq <= tier2MaxDistSq) continue;

                candidates.Add(new CandidateInfo
                {
                    Entity = entity,
                    Position = worldPos.ValueRO.Value,
                    FactionId = identity.ValueRO.FactionId,
                    EntityId = identity.ValueRO.Id,
                    Snapshot = snapshot.ValueRO
                });
            }

            if (candidates.Length < config.MinFleetSize)
            {
                candidates.Dispose();
                return;
            }

            var cellMap = new NativeParallelMultiHashMap<long, int>(candidates.Length, Allocator.Temp);
            for (int i = 0; i < candidates.Length; i++)
            {
                long cellX = (long)math.floor(candidates[i].Position.x / cellSize);
                long cellY = (long)math.floor(candidates[i].Position.y / cellSize);
                long key = CellKey(candidates[i].FactionId, cellX, cellY);
                cellMap.Add(key, i);
            }

            var grouped = new NativeArray<bool>(candidates.Length, Allocator.Temp);

            for (int i = 0; i < candidates.Length; i++)
            {
                if (grouped[i]) continue;

                var group = new NativeList<int>(Allocator.Temp);
                group.Add(i);
                grouped[i] = true;

                long cellX = (long)math.floor(candidates[i].Position.x / cellSize);
                long cellY = (long)math.floor(candidates[i].Position.y / cellSize);
                int factionId = candidates[i].FactionId;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        long neighborKey = CellKey(factionId, cellX + dx, cellY + dy);
                        if (!cellMap.TryGetFirstValue(neighborKey, out int j, out var it))
                            continue;
                        do
                        {
                            if (j <= i || grouped[j]) continue;
                            double2 delta = candidates[j].Position - candidates[i].Position;
                            double distSq = delta.x * delta.x + delta.y * delta.y;
                            if (distSq <= fleetGroupRadiusSq)
                            {
                                group.Add(j);
                                grouped[j] = true;
                            }
                        } while (cellMap.TryGetNextValue(out j, ref it));
                    }
                }

                if (group.Length >= config.MinFleetSize)
                    CreateFleet(ref state, candidates, group, ecb);

                group.Dispose();
            }

            grouped.Dispose();
            cellMap.Dispose();
            candidates.Dispose();
        }

        static long CellKey(int factionId, long cellX, long cellY)
        {
            return (long)factionId * 73856093L + cellX * 19349663L + cellY * 83492791L;
        }

        void CreateFleet(ref SystemState state, NativeList<CandidateInfo> candidates, NativeList<int> group, EntityCommandBuffer ecb)
        {
            double2 avgPos = double2.zero;
            float totalStrength = 0f;
            float totalHP = 0f;

            for (int k = 0; k < group.Length; k++)
            {
                avgPos += candidates[group[k]].Position;
                totalStrength += 50f;
                totalHP += candidates[group[k]].Snapshot.HullPercent * 100f;
            }
            avgPos /= group.Length;

            var fleetEntity = ecb.CreateEntity();
            ecb.AddComponent(fleetEntity, new FleetTag());
            ecb.AddComponent(fleetEntity, new FleetData
            {
                FleetId = candidates[group[0]].EntityId,
                FactionIndex = candidates[group[0]].FactionId,
                Position = avgPos,
                Velocity = double2.zero,
                Rotation = 0f,
                MemberCount = group.Length,
                Formation = 0,
                TotalStrength = totalStrength,
                TotalHP = totalHP,
                CurrentBehavior = 0,
                TargetFleetId = -1,
                LastUpdateTime = (float)SystemAPI.Time.ElapsedTime
            });

            var buffer = ecb.AddBuffer<FleetMember>(fleetEntity);
            for (int k = 0; k < group.Length; k++)
            {
                var c = candidates[group[k]];
                float memberAngle = (float)k / group.Length * math.PI * 2f;
                var offset = new float2(math.cos(memberAngle), math.sin(memberAngle)) * 100f;

                buffer.Add(new FleetMember
                {
                    EntityId = c.EntityId,
                    FormationSlot = k,
                    FormationOffset = offset,
                    Strength = 50f,
                    HP = c.Snapshot.HullPercent * 100f,
                    Persistence = 0,
                    Snapshot = c.Snapshot
                });

                ecb.DestroyEntity(c.Entity);
            }
        }

        void ProcessT3ToT2(ref SystemState state, SimulationConfig config, double2 playerWorldPos, EntityCommandBuffer ecb)
        {
            var origin = SystemAPI.GetSingleton<WorldOrigin>();
            double threshold = config.Tier2MaxDistance - config.Tier2Hysteresis;
            double thresholdSq = threshold * threshold;
            int fleetsProcessed = 0;

            foreach (var (fleetData, entity) in
                SystemAPI.Query<RefRO<FleetData>>()
                    .WithAll<FleetTag>()
                    .WithEntityAccess())
            {
                if (fleetsProcessed >= MaxFleetsPerTick)
                    break;

                double2 delta = fleetData.ValueRO.Position - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq >= thresholdSq)
                    continue;

                var buffer = state.EntityManager.GetBuffer<FleetMember>(entity);

                for (int i = 0; i < buffer.Length; i++)
                {
                    var member = buffer[i];
                    var memberPos = fleetData.ValueRO.Position + (double2)member.FormationOffset;
                    var localPos = (float2)(memberPos - origin.Value);

                    var shipEntity = ecb.CreateEntity(_shipArchetype);

                    ecb.SetComponent(shipEntity, LocalTransform.FromPositionRotation(
                        new float3(localPos.x, localPos.y, 0f), quaternion.identity));

                    ecb.SetComponent(shipEntity, new WorldPosition { Value = memberPos });

                    ecb.SetComponent(shipEntity, new EntityIdentity
                    {
                        Id = member.EntityId,
                        EntityType = (byte)Starfire.Entity.EntityType.Ship,
                        Persistence = member.Persistence,
                        FactionId = fleetData.ValueRO.FactionIndex,
                        ConfigId = member.Snapshot.ConfigId
                    });

                    ecb.SetComponent(shipEntity, new SimulationTierData
                    {
                        Tier = SimulationTier.Sensor,
                        LastUpdatedTime = (float)SystemAPI.Time.ElapsedTime
                    });

                    ecb.SetComponent(shipEntity, new ShipHull
                    {
                        ConfigId = member.Snapshot.ConfigId,
                        CurrentHealth = member.Snapshot.HullPercent * 100f,
                        MaxHealth = 100f,
                        EfficiencyCoefficient = 1f,
                        IsEnabled = 1
                    });

                    ecb.SetComponent(shipEntity, new ShipPropulsion
                    {
                        ConfigId = member.Snapshot.ConfigId,
                        CurrentHealth = member.Snapshot.PropulsionEfficiency * 100f,
                        MaxHealth = 100f,
                        MaxSpeed = 200f,
                        Acceleration = 60f,
                        DragCoefficient = 0.4f,
                        IsEnabled = 1
                    });

                    ecb.SetComponent(shipEntity, new ShipRotation
                    {
                        ConfigId = member.Snapshot.ConfigId,
                        CurrentHealth = member.Snapshot.RotationEfficiency * 100f,
                        MaxHealth = 100f,
                        TurnRate = 180f,
                        CurrentHeading = member.Snapshot.Heading,
                        IsEnabled = 1
                    });

                    ecb.SetComponent(shipEntity, member.Snapshot);

                    ecb.SetComponent(shipEntity, new SensorContact
                    {
                        HullPercent = member.Snapshot.HullPercent,
                        ShieldPercent = member.Snapshot.ShieldPercent,
                        Speed = (float)math.length(member.Snapshot.Velocity),
                        Heading = member.Snapshot.Heading,
                        MaxSpeed = 200f,
                        WeaponRange = 500f,
                        SensorRange = 2000f,
                        CombatStrength = member.Strength,
                        CurrentAIState = member.Snapshot.AIState,
                        StateTimer = 0f,
                        TargetEntityId = -1,
                        Waypoint = member.Snapshot.Waypoint,
                        ShieldsActive = 1,
                        WeaponsArmed = 1
                    });

                    ecb.SetComponent(shipEntity, new PhysicsDamping { Linear = 0.05f, Angular = 5f });
                    ecb.SetComponent(shipEntity, new PhysicsGravityFactor { Value = 0f });

                    ecb.SetComponentEnabled<RichTierTag>(shipEntity, false);
                    ecb.SetComponentEnabled<VisualTierTag>(shipEntity, false);
                    ecb.SetComponentEnabled<SensorTierTag>(shipEntity, true);
                    ecb.RemoveComponent<PhysicsWorldIndex>(shipEntity);
                }

                ecb.DestroyEntity(entity);
                fleetsProcessed++;
            }
        }

        void ProcessT3ToT4(ref SystemState state, SimulationConfig config, double2 playerWorldPos, EntityCommandBuffer ecb)
        {
            double tier3MaxDistSq = (double)config.Tier3MaxDistance * config.Tier3MaxDistance;

            foreach (var (fleetData, entity) in
                SystemAPI.Query<RefRO<FleetData>>()
                    .WithAll<FleetTag>()
                    .WithEntityAccess())
            {
                double2 delta = fleetData.ValueRO.Position - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq <= tier3MaxDistSq)
                    continue;

                var dormantEntity = ecb.CreateEntity();
                ecb.AddComponent(dormantEntity, new DormantTag());
                ecb.AddComponent(dormantEntity, new DormantRecord
                {
                    ChunkX = (long)(fleetData.ValueRO.Position.x / 1000.0),
                    ChunkY = (long)(fleetData.ValueRO.Position.y / 1000.0),
                    EntityType = (byte)Starfire.Entity.EntityType.Ship,
                    FactionIndex = fleetData.ValueRO.FactionIndex,
                    Count = fleetData.ValueRO.MemberCount,
                    Seed = (uint)(fleetData.ValueRO.FleetId * 31 + 17),
                    Persistence = 0,
                    Snapshot = default
                });

                ecb.DestroyEntity(entity);
            }
        }

        void ProcessT4ToT3(ref SystemState state, SimulationConfig config, double2 playerWorldPos, EntityCommandBuffer ecb)
        {
            double threshold = config.Tier3MaxDistance - config.Tier3Hysteresis;
            double thresholdSq = threshold * threshold;

            foreach (var (dormantRecord, entity) in
                SystemAPI.Query<RefRO<DormantRecord>>()
                    .WithAll<DormantTag>()
                    .WithEntityAccess())
            {
                var chunkCenter = new double2(
                    dormantRecord.ValueRO.ChunkX * 1000.0 + 500.0,
                    dormantRecord.ValueRO.ChunkY * 1000.0 + 500.0);

                double2 delta = chunkCenter - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq >= thresholdSq)
                    continue;

                int memberCount = math.max(dormantRecord.ValueRO.Count, config.MinFleetSize);
                var rng = new Unity.Mathematics.Random(dormantRecord.ValueRO.Seed);

                var fleetEntity = ecb.CreateEntity();
                ecb.AddComponent(fleetEntity, new FleetTag());
                ecb.AddComponent(fleetEntity, new FleetData
                {
                    FleetId = (int)dormantRecord.ValueRO.Seed,
                    FactionIndex = dormantRecord.ValueRO.FactionIndex,
                    Position = chunkCenter,
                    Velocity = (double2)(rng.NextFloat2Direction() * rng.NextFloat(10f, 50f)),
                    Rotation = rng.NextFloat(0f, 360f),
                    MemberCount = memberCount,
                    Formation = 0,
                    TotalStrength = memberCount * 50f,
                    TotalHP = memberCount * 100f,
                    CurrentBehavior = 0,
                    TargetFleetId = -1,
                    LastUpdateTime = (float)SystemAPI.Time.ElapsedTime
                });

                var buffer = ecb.AddBuffer<FleetMember>(fleetEntity);
                for (int i = 0; i < memberCount; i++)
                {
                    float angle = (float)i / memberCount * math.PI * 2f;
                    var offset = new float2(math.cos(angle), math.sin(angle)) * 100f;

                    buffer.Add(new FleetMember
                    {
                        EntityId = rng.NextInt(100000, 999999),
                        FormationSlot = i,
                        FormationOffset = offset,
                        Strength = 50f,
                        HP = 100f,
                        Persistence = 0,
                        Snapshot = new ShipSnapshot
                        {
                            ConfigId = 0,
                            FactionIndex = dormantRecord.ValueRO.FactionIndex,
                            Persistence = 0,
                            HullPercent = 1f,
                            ShieldPercent = 1f,
                            PropulsionEfficiency = 1f,
                            RotationEfficiency = 1f,
                            Position = chunkCenter + (double2)offset,
                            Velocity = double2.zero,
                            Heading = rng.NextFloat(0f, 360f),
                            AIState = 0,
                            TargetEntityId = -1,
                            Waypoint = double2.zero
                        }
                    });
                }

                ecb.DestroyEntity(entity);
            }
        }

        struct CandidateInfo
        {
            public Unity.Entities.Entity Entity;
            public double2 Position;
            public int FactionId;
            public int EntityId;
            public ShipSnapshot Snapshot;
        }

    }
}

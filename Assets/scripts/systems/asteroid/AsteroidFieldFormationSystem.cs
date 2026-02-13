using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TierTransitionCleanupSystem))]
    public partial struct AsteroidFieldFormationSystem : ISystem
    {
        const int MaxFieldsPerTick = 5;

        float _lastUpdateTime;

        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTime = -0.833f;
            state.RequireForUpdate<SimulationConfig>();
            state.RequireForUpdate<PlayerTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            if (elapsedTime - _lastUpdateTime < 1.0f)
                return;
            _lastUpdateTime = elapsedTime;

            var config = SystemAPI.GetSingleton<SimulationConfig>();

            double2 playerWorldPos = double2.zero;
            foreach (var (worldPos, _) in SystemAPI.Query<RefRO<WorldPosition>, RefRO<PlayerTag>>())
            {
                playerWorldPos = worldPos.ValueRO.Value;
                break;
            }

            double tier2MaxDistSq = (double)config.Bounds.Tier2MaxDistance * config.Bounds.Tier2MaxDistance;
            double groupRadius = config.Fleet.GroupingRadius * 2.0;
            double groupRadiusSq = groupRadius * groupRadius;

            var candidates = new NativeList<CandidateInfo>(Allocator.Temp);

            foreach (var (tierData, worldPos, identity, asteroid, entity) in
                SystemAPI.Query<RefRO<SimulationTierData>, RefRO<WorldPosition>, RefRO<EntityIdentity>, RefRO<AsteroidData>>()
                    .WithAll<AsteroidTag, SensorTierTag>()
                    .WithNone<PlayerTag>()
                    .WithEntityAccess())
            {
                if (identity.ValueRO.Persistence == 2) continue;

                double2 delta = worldPos.ValueRO.Value - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq <= tier2MaxDistSq) continue;

                candidates.Add(new CandidateInfo
                {
                    Entity = entity,
                    Position = worldPos.ValueRO.Value,
                    EntityId = identity.ValueRO.Id,
                    Size = asteroid.ValueRO.Size,
                    Composition = asteroid.ValueRO.Composition,
                    TypeId = asteroid.ValueRO.TypeId,
                    OrbitalVelocity = asteroid.ValueRO.OrbitalVelocity,
                    ParentStarId = asteroid.ValueRO.ParentStarId
                });
            }

            if (candidates.Length < 3)
            {
                candidates.Dispose();
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var cellMap = new NativeParallelMultiHashMap<long, int>(candidates.Length, Allocator.Temp);
            for (int i = 0; i < candidates.Length; i++)
            {
                long cellX = (long)math.floor(candidates[i].Position.x / groupRadius);
                long cellY = (long)math.floor(candidates[i].Position.y / groupRadius);
                long key = cellX * 19349663L + cellY * 83492791L;
                cellMap.Add(key, i);
            }

            var grouped = new NativeArray<bool>(candidates.Length, Allocator.Temp);
            int fieldsCreated = 0;

            for (int i = 0; i < candidates.Length; i++)
            {
                if (fieldsCreated >= MaxFieldsPerTick) break;
                if (grouped[i]) continue;

                var group = new NativeList<int>(Allocator.Temp);
                group.Add(i);
                grouped[i] = true;

                long cellX = (long)math.floor(candidates[i].Position.x / groupRadius);
                long cellY = (long)math.floor(candidates[i].Position.y / groupRadius);

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        long neighborKey = (cellX + dx) * 19349663L + (cellY + dy) * 83492791L;
                        if (!cellMap.TryGetFirstValue(neighborKey, out int j, out var it))
                            continue;
                        do
                        {
                            if (j <= i || grouped[j]) continue;
                            double2 d = candidates[j].Position - candidates[i].Position;
                            double dSq = d.x * d.x + d.y * d.y;
                            if (dSq <= groupRadiusSq)
                            {
                                group.Add(j);
                                grouped[j] = true;
                            }
                        } while (cellMap.TryGetNextValue(out j, ref it));
                    }
                }

                if (group.Length >= 3)
                {
                    CreateField(candidates, group, ecb, elapsedTime);
                    fieldsCreated++;
                }

                group.Dispose();
            }

            grouped.Dispose();
            cellMap.Dispose();
            candidates.Dispose();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        static void CreateField(NativeList<CandidateInfo> candidates, NativeList<int> group,
            EntityCommandBuffer ecb, float elapsedTime)
        {
            double2 avgPos = double2.zero;
            float totalMass = 0f;
            float maxDist = 0f;
            int compositionCounts0 = 0, compositionCounts1 = 0, compositionCounts2 = 0, compositionCounts3 = 0;

            for (int k = 0; k < group.Length; k++)
            {
                avgPos += candidates[group[k]].Position;
                totalMass += candidates[group[k]].Size * candidates[group[k]].Size;
                switch (candidates[group[k]].Composition)
                {
                    case 0: compositionCounts0++; break;
                    case 1: compositionCounts1++; break;
                    case 2: compositionCounts2++; break;
                    default: compositionCounts3++; break;
                }
            }
            avgPos /= group.Length;

            for (int k = 0; k < group.Length; k++)
            {
                double2 d = candidates[group[k]].Position - avgPos;
                float dist = (float)math.length(d);
                if (dist > maxDist) maxDist = dist;
            }

            byte dominant = 0;
            int maxCount = compositionCounts0;
            if (compositionCounts1 > maxCount) { dominant = 1; maxCount = compositionCounts1; }
            if (compositionCounts2 > maxCount) { dominant = 2; maxCount = compositionCounts2; }
            if (compositionCounts3 > maxCount) { dominant = 3; }

            uint seed = (uint)(candidates[group[0]].EntityId * 73856093 + group.Length * 19349663);

            var fieldEntity = ecb.CreateEntity();
            ecb.AddComponent(fieldEntity, new AsteroidFieldTag());
            ecb.AddComponent(fieldEntity, new AsteroidFieldData
            {
                Position = avgPos,
                Radius = maxDist + 500f,
                Count = group.Length,
                DominantComposition = dominant,
                TotalMass = totalMass,
                Seed = seed,
                ParentStarId = candidates[group[0]].ParentStarId,
                LastUpdateTime = elapsedTime
            });

            var buffer = ecb.AddBuffer<AsteroidFieldMember>(fieldEntity);
            for (int k = 0; k < group.Length; k++)
            {
                var c = candidates[group[k]];
                buffer.Add(new AsteroidFieldMember
                {
                    EntityId = c.EntityId,
                    Position = c.Position,
                    Size = c.Size,
                    Composition = c.Composition,
                    TypeId = c.TypeId,
                    OrbitalVelocity = c.OrbitalVelocity
                });

                ecb.DestroyEntity(c.Entity);
            }
        }

        struct CandidateInfo
        {
            public Unity.Entities.Entity Entity;
            public double2 Position;
            public int EntityId;
            public float Size;
            public byte Composition;
            public byte TypeId;
            public float2 OrbitalVelocity;
            public int ParentStarId;
        }
    }
}

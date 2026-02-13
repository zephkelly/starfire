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
    public partial struct AsteroidDormantRevivalSystem : ISystem
    {
        const int MaxRevivalsPerTick = 3;

        float _lastUpdateTime;

        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTime = -0.917f;
            state.RequireForUpdate<SimulationConfig>();
            state.RequireForUpdate<AsteroidConfig>();
            state.RequireForUpdate<PlayerTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            if (elapsedTime - _lastUpdateTime < 1.0f)
                return;
            _lastUpdateTime = elapsedTime;

            var config = SystemAPI.GetSingleton<SimulationConfig>();
            var asteroidConfig = SystemAPI.GetSingleton<AsteroidConfig>();

            double2 playerWorldPos = double2.zero;
            foreach (var (worldPos, _) in SystemAPI.Query<RefRO<WorldPosition>, RefRO<PlayerTag>>())
            {
                playerWorldPos = worldPos.ValueRO.Value;
                break;
            }

            var stars = new NativeList<StarInfo>(Allocator.Temp);
            foreach (var (starData, starWorldPos) in
                SystemAPI.Query<RefRO<StarData>, RefRO<WorldPosition>>()
                    .WithAll<StarTag>())
            {
                stars.Add(new StarInfo
                {
                    Position = starWorldPos.ValueRO.Value,
                    GravityStrength = starData.ValueRO.GravityStrength,
                    GravityRangeSq = starData.ValueRO.GravityRange * starData.ValueRO.GravityRange
                });
            }

            double threshold = config.Bounds.Tier3MaxDistance - config.Bounds.Tier3Hysteresis;
            double thresholdSq = threshold * threshold;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            int revivalsThisTick = 0;

            foreach (var (dormantRecord, entity) in
                SystemAPI.Query<RefRO<DormantRecord>>()
                    .WithAll<DormantTag>()
                    .WithEntityAccess())
            {
                if (revivalsThisTick >= MaxRevivalsPerTick)
                    break;

                if (dormantRecord.ValueRO.EntityType != (byte)Starfire.Entity.EntityType.Asteroid)
                    continue;

                var chunkCenter = new double2(
                    dormantRecord.ValueRO.ChunkX * 1000.0 + 500.0,
                    dormantRecord.ValueRO.ChunkY * 1000.0 + 500.0);

                double2 delta = chunkCenter - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq >= thresholdSq)
                    continue;

                int nearestStar = -1;
                double nearestDistSq = double.MaxValue;
                for (int s = 0; s < stars.Length; s++)
                {
                    double2 sd = chunkCenter - stars[s].Position;
                    double sdSq = sd.x * sd.x + sd.y * sd.y;
                    if (sdSq < nearestDistSq && sdSq <= stars[s].GravityRangeSq)
                    {
                        nearestDistSq = sdSq;
                        nearestStar = s;
                    }
                }

                int count = math.max(dormantRecord.ValueRO.Count, 3);
                var rng = new Random(dormantRecord.ValueRO.Seed);
                bool clockwise = rng.NextBool();

                var fieldEntity = ecb.CreateEntity();
                ecb.AddComponent(fieldEntity, new AsteroidFieldTag());
                ecb.AddComponent(fieldEntity, new AsteroidFieldData
                {
                    Position = chunkCenter,
                    Radius = 2000f,
                    Count = count,
                    DominantComposition = 0,
                    TotalMass = count * 4f,
                    Seed = dormantRecord.ValueRO.Seed,
                    ParentStarId = -1,
                    LastUpdateTime = elapsedTime
                });

                var buffer = ecb.AddBuffer<AsteroidFieldMember>(fieldEntity);
                for (int i = 0; i < count; i++)
                {
                    float angle = rng.NextFloat(0f, math.PI * 2f);
                    float dist = rng.NextFloat(100f, 2000f);
                    var memberPos = chunkCenter + new double2(math.cos(angle) * dist, math.sin(angle) * dist);

                    float2 orbitalVelocity = float2.zero;
                    if (nearestStar >= 0)
                    {
                        var star = stars[nearestStar];
                        float2 radial = (float2)(memberPos - star.Position);
                        float radialDist = math.length(radial);
                        if (radialDist > 1f)
                        {
                            float speed = math.sqrt(star.GravityStrength / radialDist);
                            float2 tangent = new float2(-radial.y, radial.x) / radialDist;
                            if (clockwise) tangent = -tangent;
                            orbitalVelocity = tangent * speed;
                        }
                    }

                    float size = rng.NextFloat(0.3f, 3.0f);
                    byte composition = (byte)rng.NextInt(0, 4);
                    buffer.Add(new AsteroidFieldMember
                    {
                        EntityId = rng.NextInt(100000, 999999),
                        Position = memberPos,
                        Size = size,
                        Composition = composition,
                        TypeId = AsteroidConfig.ComputeTypeId(
                            size, composition, asteroidConfig.Type0MaxSize, asteroidConfig.Type1MaxSize),
                        OrbitalVelocity = orbitalVelocity
                    });
                }

                ecb.DestroyEntity(entity);
                revivalsThisTick++;
            }

            stars.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        struct StarInfo
        {
            public double2 Position;
            public float GravityStrength;
            public double GravityRangeSq;
        }
    }
}

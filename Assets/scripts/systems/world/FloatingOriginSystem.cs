using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Starfire.Entity;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(WorldBoundsWrapSystem))]
    [BurstCompile]
    public partial struct ServerFloatingOriginSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldOrigin>();
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float2 playerLocalPos = float2.zero;
            bool foundPlayer = false;

            foreach (var (transform, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerTag>>())
            {
                playerLocalPos = transform.ValueRO.Position.xy;
                foundPlayer = true;
                break;
            }

            if (!foundPlayer)
                return;

            var origin = SystemAPI.GetSingleton<WorldOrigin>();

            if (math.lengthsq(playerLocalPos) <= origin.RebaseThreshold * origin.RebaseThreshold)
                return;

            var offset = new double2(playerLocalPos.x, playerLocalPos.y);
            origin.Value += offset;
            SystemAPI.SetSingleton(origin);

            var floatOffset = new float3(playerLocalPos.x, playerLocalPos.y, 0f);

            new RebaseJob
            {
                Offset = floatOffset
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct RebaseJob : IJobEntity
        {
            public float3 Offset;

            void Execute(ref LocalTransform transform)
            {
                transform.Position -= Offset;
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct ClientFloatingOriginSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldOrigin>();
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double2 playerWorldPos = double2.zero;
            bool foundPlayer = false;

            foreach (var (worldPos, _) in SystemAPI.Query<RefRO<WorldPosition>, RefRO<PlayerTag>>())
            {
                playerWorldPos = worldPos.ValueRO.Value;
                foundPlayer = true;
                break;
            }

            if (!foundPlayer)
                return;

            var origin = SystemAPI.GetSingleton<WorldOrigin>();
            double2 localDelta = playerWorldPos - origin.Value;

            if (math.lengthsq(localDelta) <= (double)origin.RebaseThreshold * origin.RebaseThreshold)
                return;

            origin.Value = playerWorldPos;
            SystemAPI.SetSingleton(origin);
        }
    }
}

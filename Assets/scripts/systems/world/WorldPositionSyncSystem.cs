using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct ServerWorldPositionSyncSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldOrigin>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var origin = SystemAPI.GetSingleton<WorldOrigin>();

            new ServerSyncJob
            {
                OriginValue = origin.Value
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ServerSyncJob : IJobEntity
        {
            public double2 OriginValue;

            void Execute(ref WorldPosition worldPos, in LocalTransform transform, in SimulationTierData tier)
            {
                if (tier.Tier <= SimulationTier.Active)
                {
                    if (!math.isnan(transform.Position.x) && !math.isnan(transform.Position.y))
                        worldPos.Value = OriginValue + new double2(transform.Position.x, transform.Position.y);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClientFloatingOriginSystem))]
    [BurstCompile]
    public partial struct ClientWorldPositionSyncSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldOrigin>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var origin = SystemAPI.GetSingleton<WorldOrigin>();

            new ClientSyncJob
            {
                OriginValue = origin.Value
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithNone(typeof(PredictedGhost))]
        partial struct ClientSyncJob : IJobEntity
        {
            public double2 OriginValue;

            void Execute(in WorldPosition worldPos, ref LocalTransform transform)
            {
                float2 localPos = (float2)(worldPos.Value - OriginValue);
                transform.Position = new float3(localPos.x, localPos.y, 0f);
            }
        }
    }
}

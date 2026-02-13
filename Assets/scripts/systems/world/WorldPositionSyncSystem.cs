using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Starfire.Entity;
using Starfire.Simulation;
using Unity.Collections;

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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ServerSyncJob().ScheduleParallel();
        }

        [BurstCompile]
        partial struct ServerSyncJob : IJobEntity
        {
            void Execute(ref WorldPosition worldPos, in LocalTransform transform, in SimulationTierData tier)
            {
                if (tier.Tier <= SimulationTier.Active)
                {
                    if (!math.isnan(transform.Position.x) && !math.isnan(transform.Position.y))
                        worldPos.Value = new double2(transform.Position.x, transform.Position.y);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct ClientWorldPositionSyncSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new LocalRichSyncJob().ScheduleParallel();
            new PredictedPlayerSyncJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(LocalEntityTag), typeof(RichTierTag))]
        partial struct LocalRichSyncJob : IJobEntity
        {
            void Execute(ref WorldPosition worldPos, in LocalTransform transform)
            {
                if (!math.isnan(transform.Position.x) && !math.isnan(transform.Position.y))
                    worldPos.Value = new double2(transform.Position.x, transform.Position.y);
            }
        }

        [BurstCompile]
        [WithAll(typeof(PlayerTag))]
        [WithNone(typeof(LocalEntityTag))]
        partial struct PredictedPlayerSyncJob : IJobEntity
        {
            void Execute(ref WorldPosition worldPos, in LocalTransform transform)
            {
                if (!math.isnan(transform.Position.x) && !math.isnan(transform.Position.y))
                    worldPos.Value = new double2(transform.Position.x, transform.Position.y);
            }
        }
    }
}

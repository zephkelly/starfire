using Unity.Entities;
using Unity.Mathematics;
using Starfire.Entity;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial class FleetAISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var fleetData in SystemAPI.Query<RefRW<FleetData>>().WithAll<FleetTag>())
            {
                if (elapsedTime - fleetData.ValueRO.LastUpdateTime < 1f)
                    continue;

                fleetData.ValueRW.LastUpdateTime = elapsedTime;

                switch (fleetData.ValueRO.CurrentBehavior)
                {
                    case 0: // Patrol
                        if (math.lengthsq(fleetData.ValueRO.Velocity) < 1.0)
                        {
                            var rng = new Unity.Mathematics.Random((uint)(fleetData.ValueRO.FleetId * 17 + (int)(elapsedTime * 10)));
                            fleetData.ValueRW.Velocity = (double2)(rng.NextFloat2Direction() * rng.NextFloat(20f, 60f));
                        }
                        fleetData.ValueRW.Position += fleetData.ValueRO.Velocity * 1.0;
                        break;

                    case 1: // Intercept
                        fleetData.ValueRW.Position += fleetData.ValueRO.Velocity * 1.0;
                        break;

                    case 2: // Retreat
                        fleetData.ValueRW.Position += fleetData.ValueRO.Velocity * 1.0;
                        break;

                    case 3: // Hold
                        break;
                }
            }
        }
    }
}

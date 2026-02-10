using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct StarGravitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StarTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }
    }
}

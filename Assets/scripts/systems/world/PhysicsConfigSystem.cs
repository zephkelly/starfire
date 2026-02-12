using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PhysicsConfigSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!SystemAPI.HasSingleton<PhysicsStep>())
            {
                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(entity, new PhysicsStep
                {
                    SimulationType = SimulationType.UnityPhysics,
                    Gravity = new float3(0f, 0f, 0f),
                    SolverIterationCount = 2,
                    MultiThreaded = 1
                });
            }

            World.MaximumDeltaTime = 1f / 30f;
            Enabled = false;
        }
    }
}

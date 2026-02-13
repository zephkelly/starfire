using Unity.Entities;
using Unity.NetCode;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServerZoneEventRelaySystem))]
    public partial class ServerWorldEventSystem : SystemBase
    {
        protected override void OnUpdate()
        {
        }
    }
}

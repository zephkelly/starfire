using Unity.Entities;
using Unity.NetCode;
using Starfire.Core;
using Starfire.Entity;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerInputApplySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (playerInput, controlInput) in SystemAPI.Query<RefRO<PlayerInput>, RefRW<ControlInput>>()
                .WithAll<PlayerTag>())
            {
                controlInput.ValueRW.Throttle = playerInput.ValueRO.Throttle;
                controlInput.ValueRW.MovementDirection = playerInput.ValueRO.MovementDirection;
                controlInput.ValueRW.AimDirection = playerInput.ValueRO.AimDirection;
                controlInput.ValueRW.FirePressed = playerInput.ValueRO.Fire.IsSet ? (byte)1 : (byte)0;
                controlInput.ValueRW.WarpPressed = playerInput.ValueRO.Warp.IsSet ? (byte)1 : (byte)0;
                controlInput.ValueRW.DriverType = 0;
            }
        }
    }
}

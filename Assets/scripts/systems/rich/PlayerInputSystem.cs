using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Starfire.Core;
using Starfire.Entity;
using Starfire.Network;
using NetPlayerInput = Starfire.Network.PlayerInput;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class PlayerInputSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var controller = PlayerController.Instance;
            if (controller == null)
                return;

            float throttle = controller.Throttle;
            float2 moveDir = controller.MovementDirection;
            bool fire = controller.FirePressed;
            bool warp = controller.WarpPressed;
            var mouseWorld = new float2(
                controller.MouseWorldPosition.x,
                controller.MouseWorldPosition.y);

            foreach (var (playerInput, controlInput, transform) in
                SystemAPI.Query<RefRW<NetPlayerInput>, RefRW<ControlInput>, RefRO<LocalTransform>>()
                    .WithAll<PlayerTag, GhostOwner>())
            {
                var shipPos = new float2(
                    transform.ValueRO.Position.x,
                    transform.ValueRO.Position.y);
                var aimDir = math.normalizesafe(mouseWorld - shipPos);

                controller.AimDirection = aimDir;

                playerInput.ValueRW.Throttle = throttle;
                playerInput.ValueRW.MovementDirection = moveDir;
                playerInput.ValueRW.AimDirection = aimDir;
                if (fire) playerInput.ValueRW.Fire.Set();
                if (warp) playerInput.ValueRW.Warp.Set();

                controlInput.ValueRW.Throttle = throttle;
                controlInput.ValueRW.MovementDirection = moveDir;
                controlInput.ValueRW.AimDirection = aimDir;
                controlInput.ValueRW.FirePressed = fire ? (byte)1 : (byte)0;
                controlInput.ValueRW.WarpPressed = warp ? (byte)1 : (byte)0;
                controlInput.ValueRW.DriverType = 0;
            }
        }
    }
}

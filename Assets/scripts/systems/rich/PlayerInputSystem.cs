using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
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
        Camera _mainCamera;

        protected override void OnUpdate()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null)
                return;

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null)
                return;

            float h = 0f;
            float v = 0f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;

            var moveDir = new float2(h, v);
            float throttle = math.length(moveDir) > 0.01f ? 1f : 0f;
            moveDir = math.normalizesafe(moveDir);

            Vector2 mouseScreen = mouse.position.ReadValue();
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -_mainCamera.transform.position.z));

            var mouseWorld2D = new float2(mouseWorldPos.x, mouseWorldPos.y);

            foreach (var (playerInput, controlInput, transform) in
                SystemAPI.Query<RefRW<NetPlayerInput>, RefRW<ControlInput>, RefRO<LocalTransform>>()
                    .WithAll<PlayerTag, GhostOwner>())
            {
                var shipPos = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
                var aimDir = math.normalizesafe(mouseWorld2D - shipPos);

                playerInput.ValueRW.Throttle = throttle;
                playerInput.ValueRW.MovementDirection = moveDir;
                playerInput.ValueRW.AimDirection = aimDir;
                if (mouse.leftButton.isPressed) playerInput.ValueRW.Fire.Set();
                if (keyboard.leftShiftKey.isPressed) playerInput.ValueRW.Warp.Set();

                controlInput.ValueRW.Throttle = throttle;
                controlInput.ValueRW.MovementDirection = moveDir;
                controlInput.ValueRW.AimDirection = aimDir;
                controlInput.ValueRW.FirePressed = mouse.leftButton.isPressed ? (byte)1 : (byte)0;
                controlInput.ValueRW.WarpPressed = keyboard.leftShiftKey.isPressed ? (byte)1 : (byte)0;
                controlInput.ValueRW.DriverType = 0;
            }
        }
    }
}

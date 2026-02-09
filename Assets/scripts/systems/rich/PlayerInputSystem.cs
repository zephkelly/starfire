using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using Starfire.Core;
using Starfire.Entity;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
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

            byte fire = mouse.leftButton.isPressed ? (byte)1 : (byte)0;
            byte warp = keyboard.leftShiftKey.isPressed ? (byte)1 : (byte)0;

            var mouseWorld2D = new float2(mouseWorldPos.x, mouseWorldPos.y);

            foreach (var (input, transform) in SystemAPI.Query<RefRW<ControlInput>, RefRO<LocalTransform>>().WithAll<PlayerTag>())
            {
                var shipPos = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
                var aimDir = math.normalizesafe(mouseWorld2D - shipPos);

                input.ValueRW.Throttle = throttle;
                input.ValueRW.MovementDirection = moveDir;
                input.ValueRW.AimDirection = aimDir;
                input.ValueRW.FirePressed = fire;
                input.ValueRW.WarpPressed = warp;
                input.ValueRW.DriverType = 0;
            }
        }
    }
}

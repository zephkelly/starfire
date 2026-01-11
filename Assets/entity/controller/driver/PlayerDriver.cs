using System;
using UnityEngine;

namespace Starfire.Entity
{
    public class PlayerDriver : IControllerDriver
    {
        private readonly IInputProvider inputProvider;

        private Vector2 movementDirection;
        private float rotationInput;
        private Vector2 aimDirection;
        private bool firePressed;
        private bool warpPressed;
        private bool hyperdrivePressed;

        public int Priority { get; }
        public bool IsActive { get; set; } = true;
        public bool IsWorldSpaceAim => false;

        public PlayerDriver(IInputProvider inputProvider, int priority = 10)
        {
            this.inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
            Priority = priority;

            Subscribe();
        }

        private void Subscribe()
        {
            inputProvider.OnMove += HandleMove;
            inputProvider.OnRotate += HandleRotate;
            inputProvider.OnAim += HandleAim;
            inputProvider.OnFire += HandleFire;
            inputProvider.OnWarpPressed += HandleWarpPressed;
            inputProvider.OnWarpReleased += HandleWarpReleased;
            inputProvider.OnHyperdrivePressed += HandleHyperdrivePressed;
            inputProvider.OnHyperdriveReleased += HandleHyperdriveReleased;
        }

        public void Unsubscribe()
        {
            inputProvider.OnMove -= HandleMove;
            inputProvider.OnRotate -= HandleRotate;
            inputProvider.OnAim -= HandleAim;
            inputProvider.OnFire -= HandleFire;
            inputProvider.OnWarpPressed -= HandleWarpPressed;
            inputProvider.OnWarpReleased -= HandleWarpReleased;
            inputProvider.OnHyperdrivePressed -= HandleHyperdrivePressed;
            inputProvider.OnHyperdriveReleased -= HandleHyperdriveReleased;
        }

        private void HandleMove(Vector2 direction) => movementDirection = direction;
        private void HandleRotate(float rotation) => rotationInput = rotation;
        private void HandleAim(Vector2 aim) => aimDirection = aim;
        private void HandleFire(bool pressed) => firePressed = pressed;
        private void HandleWarpPressed() => warpPressed = true;
        private void HandleWarpReleased() => warpPressed = false;
        private void HandleHyperdrivePressed() => hyperdrivePressed = true;
        private void HandleHyperdriveReleased() => hyperdrivePressed = false;

        public Vector2 GetMovementDirection() => movementDirection;
        public float GetThrottle() => 1f;
        public Vector2 GetDesiredAcceleration() => Vector2.zero;  // Player uses direction/throttle
        public float GetRotationInput() => rotationInput;
        public Vector2 GetAimDirection() => aimDirection;
        public bool IsFirePressed() => firePressed;
        public bool IsWarpPressed() => warpPressed;
        public bool IsHyperdrivePressed() => hyperdrivePressed;
    }
}

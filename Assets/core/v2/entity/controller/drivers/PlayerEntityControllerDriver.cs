using System;
using UnityEngine;

namespace StarfireV2
{
    [Serializable]
    public class PlayerEntityControllerDriver : IEntityControllerDriver
    {
        [SerializeField, Tooltip("Reference to the global input provider configuration. If null, will auto-discover via InputProviderRegistry.")]
        private InputProviderReference inputProviderReference;

        private IInputProvider inputProvider;
        private bool isInitialized;

        private Vector2 movementDirection;
        private float rotationInput;
        private Vector2 aimDirection;
        private bool firePressed;
        private bool warpPressed;
        private bool hyperdrivePressed;

        [field: SerializeField]
        public int Priority { get; private set; } = 10;
        public bool IsActive { get; set; } = true;
        public bool IsWorldSpaceAim => false;

        // Parameterless constructor for serialization
        public PlayerEntityControllerDriver() { }

        public PlayerEntityControllerDriver(IInputProvider inputProvider, int priority = 10)
        {
            this.inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
            Priority = priority;

            Subscribe();
            isInitialized = true;
        }

        /// <summary>
        /// Ensures the driver is initialized with an input provider.
        /// Called automatically when driver becomes active, or can be called manually.
        /// Uses InputProviderReference if assigned, otherwise falls back to InputProviderRegistry.
        /// </summary>
        public void EnsureInitialized()
        {
            if (isInitialized) return;

            // Try ScriptableObject reference first
            if (inputProviderReference != null)
            {
                inputProvider = inputProviderReference.GetProvider();
            }
            // Fallback to global registry
            else if (InputProviderRegistry.Instance != null)
            {
                inputProvider = InputProviderRegistry.Instance.GetProvider();
            }

            if (inputProvider != null)
            {
                Subscribe();
                isInitialized = true;
            }
            else
            {
                Debug.LogWarning("[PlayerEntityControllerDriver] No input provider found. " +
                    "Assign an InputProviderReference or ensure InputProviderRegistry exists in the scene.");
            }
        }

        public void Initialize(IInputProvider inputProvider)
        {
            if (isInitialized)
                Unsubscribe();

            this.inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
            Subscribe();
            isInitialized = true;
        }

        private void Subscribe()
        {
            if (inputProvider == null) return;

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
            if (inputProvider == null) return;

            inputProvider.OnMove -= HandleMove;
            inputProvider.OnRotate -= HandleRotate;
            inputProvider.OnAim -= HandleAim;
            inputProvider.OnFire -= HandleFire;
            inputProvider.OnWarpPressed -= HandleWarpPressed;
            inputProvider.OnWarpReleased -= HandleWarpReleased;
            inputProvider.OnHyperdrivePressed -= HandleHyperdrivePressed;
            inputProvider.OnHyperdriveReleased -= HandleHyperdriveReleased;

            isInitialized = false;
        }

        private void HandleMove(Vector2 direction) => movementDirection = direction;
        private void HandleRotate(float rotation) => rotationInput = rotation;
        private void HandleAim(Vector2 aim) => aimDirection = aim;
        private void HandleFire(bool pressed) => firePressed = pressed;
        private void HandleWarpPressed() => warpPressed = true;
        private void HandleWarpReleased() => warpPressed = false;
        private void HandleHyperdrivePressed() => hyperdrivePressed = true;
        private void HandleHyperdriveReleased() => hyperdrivePressed = false;

        public bool HasAimTarget => true; // Player always has mouse/stick aim

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

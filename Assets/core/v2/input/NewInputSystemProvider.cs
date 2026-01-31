using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarfireV2
{
    public class NewInputSystemProvider : MonoBehaviour, IInputProvider
    {
        public event Action<Vector2> OnMove;
        public event Action<float> OnRotate;
        public event Action<Vector2> OnAim;
        public event Action<bool> OnFire;
        public event Action OnWarpPressed;
        public event Action OnWarpReleased;
        public event Action OnHyperdrivePressed;
        public event Action OnHyperdriveReleased;
        public event Action<bool> OnInputDeviceChanged;
        public event Action<float> OnZoom;

        [SerializeField] private InputActionAsset inputActions;

        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction aimAction;
        private InputAction fireAction;
        private InputAction warpAction;
        private InputAction hyperdriveAction;
        private InputAction zoomAction;

        private bool isUsingGamepad;
        private Vector2 lastMousePosition;

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError("[NewInputSystemProvider] No InputActionAsset assigned.");
                return;
            }

            playerMap = inputActions.FindActionMap("Player");
            if (playerMap == null)
            {
                Debug.LogError("[NewInputSystemProvider] 'Player' action map not found in InputActionAsset.");
                return;
            }

            moveAction = playerMap.FindAction("Move");
            aimAction = playerMap.FindAction("Aim");
            fireAction = playerMap.FindAction("Fire");
            warpAction = playerMap.FindAction("Warp");
            hyperdriveAction = playerMap.FindAction("Hyperdrive");
            zoomAction = playerMap.FindAction("Zoom");

            if (moveAction != null)
                moveAction.performed += OnMovePerformed;
            if (moveAction != null)
                moveAction.canceled += OnMoveCanceled;

            if (aimAction != null)
            {
                aimAction.performed += OnAimPerformed;
                aimAction.canceled += OnAimCanceled;
            }

            if (fireAction != null)
            {
                fireAction.started += OnFireStarted;
                fireAction.canceled += OnFireCanceled;
            }

            if (warpAction != null)
            {
                warpAction.started += OnWarpStarted;
                warpAction.canceled += OnWarpCanceled;
            }

            if (hyperdriveAction != null)
            {
                hyperdriveAction.started += OnHyperdriveStarted;
                hyperdriveAction.canceled += OnHyperdriveCanceled;
            }

            if (zoomAction != null)
            {
                zoomAction.performed += OnZoomPerformed;
                zoomAction.canceled += OnZoomCanceled;
            }

            playerMap.Enable();
        }

        private void OnDisable()
        {
            if (playerMap == null) return;

            if (moveAction != null)
            {
                moveAction.performed -= OnMovePerformed;
                moveAction.canceled -= OnMoveCanceled;
            }

            if (aimAction != null)
            {
                aimAction.performed -= OnAimPerformed;
                aimAction.canceled -= OnAimCanceled;
            }

            if (fireAction != null)
            {
                fireAction.started -= OnFireStarted;
                fireAction.canceled -= OnFireCanceled;
            }

            if (warpAction != null)
            {
                warpAction.started -= OnWarpStarted;
                warpAction.canceled -= OnWarpCanceled;
            }

            if (hyperdriveAction != null)
            {
                hyperdriveAction.started -= OnHyperdriveStarted;
                hyperdriveAction.canceled -= OnHyperdriveCanceled;
            }

            if (zoomAction != null)
            {
                zoomAction.performed -= OnZoomPerformed;
                zoomAction.canceled -= OnZoomCanceled;
            }

            playerMap.Disable();
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            UpdateDeviceState(ctx);
            OnMove?.Invoke(ctx.ReadValue<Vector2>());
        }

        private void OnMoveCanceled(InputAction.CallbackContext ctx)
        {
            OnMove?.Invoke(Vector2.zero);
        }

        private void OnAimPerformed(InputAction.CallbackContext ctx)
        {
            bool isMouse = ctx.control.device is UnityEngine.InputSystem.Mouse;
            if (isMouse)
            {
                // Only switch to mouse mode on actual mouse movement, not passive position updates
                Vector2 pos = ctx.ReadValue<Vector2>();
                if ((pos - lastMousePosition).sqrMagnitude > 4f) // ~2px threshold
                {
                    lastMousePosition = pos;
                    UpdateDeviceState(ctx);
                    OnAim?.Invoke(pos);
                }
            }
            else
            {
                UpdateDeviceState(ctx);
                OnAim?.Invoke(ctx.ReadValue<Vector2>());
            }
        }

        private void OnAimCanceled(InputAction.CallbackContext ctx)
        {
            // Don't forward zero — the AimReticle holds its last direction on zero input,
            // and mouse aim doesn't need a canceled event.
        }

        private void UpdateDeviceState(InputAction.CallbackContext ctx)
        {
            bool gamepad = ctx.control.device is Gamepad;
            if (gamepad != isUsingGamepad)
            {
                isUsingGamepad = gamepad;
                OnInputDeviceChanged?.Invoke(isUsingGamepad);
            }
        }

        private void OnFireStarted(InputAction.CallbackContext ctx) { UpdateDeviceState(ctx); OnFire?.Invoke(true); }
        private void OnFireCanceled(InputAction.CallbackContext ctx) => OnFire?.Invoke(false);

        private void OnWarpStarted(InputAction.CallbackContext ctx) => OnWarpPressed?.Invoke();
        private void OnWarpCanceled(InputAction.CallbackContext ctx) => OnWarpReleased?.Invoke();

        private void OnHyperdriveStarted(InputAction.CallbackContext ctx) => OnHyperdrivePressed?.Invoke();
        private void OnHyperdriveCanceled(InputAction.CallbackContext ctx) => OnHyperdriveReleased?.Invoke();

        private void OnZoomPerformed(InputAction.CallbackContext ctx)
        {
            UpdateDeviceState(ctx);
            OnZoom?.Invoke(ctx.ReadValue<float>());
        }

        private void OnZoomCanceled(InputAction.CallbackContext ctx)
        {
            OnZoom?.Invoke(0f);
        }

        /// <summary>
        /// Whether the current aim input is coming from a gamepad (right stick direction)
        /// vs mouse (screen-space position). Useful for the driver to interpret aim data correctly.
        /// </summary>
        public bool IsUsingGamepad => isUsingGamepad;
    }
}

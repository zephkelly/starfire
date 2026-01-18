using System;
using UnityEngine;

namespace StarfireV2
{
    public class OldInputProvider : MonoBehaviour, IInputProvider
    {
        public event Action<Vector2> OnMove;
        public event Action<float> OnRotate;
        public event Action<Vector2> OnAim;
        public event Action<bool> OnFire;
        public event Action OnWarpPressed;
        public event Action OnWarpReleased;
        public event Action OnHyperdrivePressed;
        public event Action OnHyperdriveReleased;

        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private KeyCode fireKey = KeyCode.Space;
        [SerializeField] private KeyCode warpKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode hyperdriveKey = KeyCode.H;

        private Vector2 lastMoveInput;
        private bool lastFireState;

        private void Update()
        {
            UpdateMovement();
            UpdateAim();
            UpdateFire();
            UpdateWarp();
            UpdateHyperdrive();
        }

        private void UpdateMovement()
        {
            var moveInput = new Vector2(
                Input.GetAxis(horizontalAxis),
                Input.GetAxis(verticalAxis)
            );

            if (moveInput != lastMoveInput)
            {
                lastMoveInput = moveInput;
                OnMove?.Invoke(moveInput);
            }
        }

        private void UpdateAim()
        {
            Vector2 mousePosition = Input.mousePosition;
            OnAim?.Invoke(mousePosition);
        }

        private void UpdateFire()
        {
            bool fireState = Input.GetKey(fireKey) || Input.GetMouseButton(0);

            if (fireState != lastFireState)
            {
                lastFireState = fireState;
                OnFire?.Invoke(fireState);
            }
        }

        private void UpdateWarp()
        {
            if (Input.GetKeyDown(warpKey))
            {
                OnWarpPressed?.Invoke();
            }
            if (Input.GetKeyUp(warpKey))
            {
                OnWarpReleased?.Invoke();
            }
        }

        private void UpdateHyperdrive()
        {
            if (Input.GetKeyDown(hyperdriveKey))
            {
                OnHyperdrivePressed?.Invoke();
            }
            if (Input.GetKeyUp(hyperdriveKey))
            {
                OnHyperdriveReleased?.Invoke();
            }
        }
    }
}

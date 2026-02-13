using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Starfire.Core
{
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        public static PlayerController Instance { get; private set; }

        [SerializeField] InputActionAsset _inputActions;

        InputAction _moveAction;
        InputAction _fireAction;
        InputAction _warpAction;
        InputAction _mousePositionAction;
        InputAction _escapeAction;
        InputAction _scrollAction;
        InputAction _zoomAction;

        Camera _camera;

        public float Throttle { get; private set; }
        public float2 MovementDirection { get; private set; }
        public float2 AimDirection { get; set; }
        public bool FirePressed { get; private set; }
        public bool WarpPressed { get; private set; }
        public bool EscapePressed { get; private set; }
        public float ScrollDelta { get; private set; }
        public float GamepadZoomInput { get; private set; }
        public Vector2 MouseScreenPosition { get; private set; }
        public Vector3 MouseWorldPosition { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnEnable()
        {
            if (_inputActions == null)
                return;

            var shipMap = _inputActions.FindActionMap("Ship");
            _moveAction = shipMap?.FindAction("Move");
            _fireAction = shipMap?.FindAction("Fire");
            _warpAction = shipMap?.FindAction("Warp");
            _mousePositionAction = shipMap?.FindAction("MousePosition");
            _zoomAction = shipMap?.FindAction("Zoom");

            var uiMap = _inputActions.FindActionMap("UI");
            _escapeAction = uiMap?.FindAction("Escape");
            _scrollAction = uiMap?.FindAction("ScrollWheel");

            _inputActions.Enable();
        }

        void OnDisable()
        {
            if (_inputActions != null)
                _inputActions.Disable();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetCamera(Camera camera)
        {
            _camera = camera;
        }

        void Update()
        {
            ReadMovement();
            ReadActions();
            ReadMousePosition();
            ReadUIActions();
        }

        void ReadMovement()
        {
            if (_moveAction == null)
                return;

            var move = _moveAction.ReadValue<Vector2>();
            var moveDir = new float2(move.x, move.y);
            Throttle = math.length(moveDir) > 0.01f ? 1f : 0f;
            MovementDirection = math.normalizesafe(moveDir);
        }

        void ReadActions()
        {
            FirePressed = _fireAction != null && _fireAction.IsPressed();
            WarpPressed = _warpAction != null && _warpAction.IsPressed();
        }

        void ReadMousePosition()
        {
            if (_mousePositionAction != null)
                MouseScreenPosition = _mousePositionAction.ReadValue<Vector2>();

            if (_camera != null)
            {
                MouseWorldPosition = _camera.ScreenToWorldPoint(
                    new Vector3(MouseScreenPosition.x, MouseScreenPosition.y,
                        -_camera.transform.position.z));
            }
        }

        void ReadUIActions()
        {
            EscapePressed = _escapeAction != null && _escapeAction.WasPressedThisFrame();
            ScrollDelta = _scrollAction != null ? _scrollAction.ReadValue<Vector2>().y : 0f;
            GamepadZoomInput = _zoomAction != null ? _zoomAction.ReadValue<float>() : 0f;
        }
    }
}

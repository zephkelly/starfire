using Starfire.Core.V2.World;
using Starfire.Entity;
using UnityEngine;

namespace Starfire.Core.Cam
{
    public class EntityCameraTarget : MonoBehaviour, ICameraTarget
    {
        [SerializeField] private float priority = 1f;

        private EntityControllerBase _controller;
        private Rigidbody2D _rigidbody;
        private Camera _mainCamera;

        private Vector2 _previousPosition;
        private Vector2 _currentPosition;
        private bool _initialized;

        public Vector2 Position => transform.position;

        public Vector2 InterpolatedPosition
        {
            get
            {
                if (!_initialized || TargetType == CameraTargetType.Transform)
                {
                    return Position;
                }

                float timeSinceFixedUpdate = Time.time - Time.fixedTime;
                float t = Mathf.Clamp01(timeSinceFixedUpdate / Time.fixedDeltaTime);
                return Vector2.Lerp(_previousPosition, _currentPosition, t);
            }
        }

        public Vector2 Velocity
        {
            get
            {
                if (_controller != null && _controller.Rigidbody != null)
                {
                    return _controller.Rigidbody.linearVelocity;
                }
                if (_rigidbody != null)
                {
                    return _rigidbody.linearVelocity;
                }
                return Vector2.zero;
            }
        }

        public Vector2 FocusDirection
        {
            get
            {
                if (_controller == null || _mainCamera == null) return Vector2.zero;

                var driver = _controller.DriverStack.GetActiveDriver();
                if (driver == null) return Vector2.zero;

                Vector2 aimScreenPos = driver.GetAimDirection();
                Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(aimScreenPos.x, aimScreenPos.y, 0f));
                Vector2 aimWorld = new Vector2(worldPos.x, worldPos.y);

                Vector2 direction = aimWorld - Position;
                return direction.normalized;
            }
        }

        public bool HasFocus
        {
            get
            {
                if (_controller == null) return false;
                var driver = _controller.DriverStack.GetActiveDriver();
                return driver != null;
            }
        }

        public bool IsValid => this != null && gameObject.activeInHierarchy;

        public float Priority => priority;

        public CameraTargetType TargetType
        {
            get
            {
                if (_controller != null && _controller.Rigidbody != null)
                {
                    return CameraTargetType.PhysicsBody;
                }
                if (_rigidbody != null)
                {
                    return CameraTargetType.PhysicsBody;
                }
                return CameraTargetType.Transform;
            }
        }

        private void Awake()
        {
            _controller = GetComponent<EntityControllerBase>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            _currentPosition = transform.position;
            _previousPosition = _currentPosition;
            _initialized = true;
        }

        private void OnEnable()
        {
            if (WorldGenerationService.Instance != null)
            {
                WorldGenerationService.Instance.OnOriginShift += OnOriginShift;
            }
        }

        private void OnDisable()
        {
            if (WorldGenerationService.Instance != null)
            {
                WorldGenerationService.Instance.OnOriginShift -= OnOriginShift;
            }
        }

        private void FixedUpdate()
        {
            _previousPosition = _currentPosition;
            _currentPosition = transform.position;
        }

        public void SetPriority(float value)
        {
            priority = value;
        }

        /// <summary>
        /// Called when a floating origin shift occurs. Updates cached interpolation positions.
        /// </summary>
        public void OnOriginShift(Vector2 shiftAmount)
        {
            _previousPosition += shiftAmount;
            _currentPosition += shiftAmount;
        }
    }
}

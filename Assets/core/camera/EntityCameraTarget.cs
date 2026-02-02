using Starfire.Core.V2.World;
using Starfire.Entity;
using StarfireV2;
using UnityEngine;

namespace Starfire.Core.Cam
{
    public class EntityCameraTarget : MonoBehaviour, ICameraTarget
    {
        [SerializeField] private float priority = 1f;

        private EntityControllerBase _controller;
        private StarfireV2.ShipController _v2Controller;
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
                if (_mainCamera == null) return Vector2.zero;

                // Try V1 controller
                if (_controller != null)
                {
                    var driver = _controller.DriverStack.GetActiveDriver();
                    if (driver != null)
                        return ComputeFocusDirection(driver.GetAimDirection(), driver.IsWorldSpaceAim);
                }

                // Try V2 controller
                if (_v2Controller != null)
                {
                    var driver = _v2Controller.DriverStack.GetActiveDriver();
                    if (driver != null)
                        return ComputeFocusDirection(driver.GetAimDirection(), driver.IsWorldSpaceAim);
                }

                return Vector2.zero;
            }
        }

        public bool HasFocus
        {
            get
            {
                if (_controller != null)
                    return _controller.DriverStack.GetActiveDriver() != null;
                if (_v2Controller != null)
                    return _v2Controller.DriverStack.GetActiveDriver() != null;
                return false;
            }
        }

        public bool IsWorldSpaceAim
        {
            get
            {
                if (_controller != null)
                {
                    var driver = _controller.DriverStack.GetActiveDriver();
                    if (driver != null) return driver.IsWorldSpaceAim;
                }
                if (_v2Controller != null)
                {
                    var driver = _v2Controller.DriverStack.GetActiveDriver();
                    if (driver != null) return driver.IsWorldSpaceAim;
                }
                return false;
            }
        }

        public float AimMaxRadius
        {
            get
            {
                if (_v2Controller != null)
                {
                    var driver = _v2Controller.DriverStack.GetActiveDriver();
                    if (driver != null) return driver.AimMaxRadius;
                }
                return 0f;
            }
        }

        public Vector2 FocusDirectionRaw
        {
            get
            {
                if (_mainCamera == null) return Vector2.zero;

                if (_controller != null)
                {
                    var driver = _controller.DriverStack.GetActiveDriver();
                    if (driver != null)
                        return ComputeFocusDirectionRaw(driver.GetAimDirection(), driver.IsWorldSpaceAim);
                }

                if (_v2Controller != null)
                {
                    var driver = _v2Controller.DriverStack.GetActiveDriver();
                    if (driver != null)
                        return ComputeFocusDirectionRaw(driver.GetAimDirection(), driver.IsWorldSpaceAim);
                }

                return Vector2.zero;
            }
        }

        private Vector2 ComputeFocusDirectionRaw(Vector2 aimRaw, bool isWorldSpace)
        {
            Vector2 aimWorld;
            if (isWorldSpace)
            {
                aimWorld = aimRaw;
            }
            else
            {
                Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(aimRaw.x, aimRaw.y, 0f));
                aimWorld = new Vector2(worldPos.x, worldPos.y);
            }

            return aimWorld - Position;
        }

        private Vector2 ComputeFocusDirection(Vector2 aimRaw, bool isWorldSpace)
        {
            Vector2 raw = ComputeFocusDirectionRaw(aimRaw, isWorldSpace);
            return raw.sqrMagnitude > 0.001f ? raw.normalized : Vector2.zero;
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
            _v2Controller = GetComponent<StarfireV2.ShipController>();
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

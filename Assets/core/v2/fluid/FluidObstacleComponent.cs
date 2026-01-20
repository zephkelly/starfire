using UnityEngine;

namespace StarfireV2.Fluid
{
    /// <summary>
    /// Adapts a ship or entity to provide fluid obstacle data.
    /// Attach to any GameObject with a Rigidbody2D to make it affect the fluid simulation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FluidObstacleComponent : MonoBehaviour, IFluidObstacle
    {
        [Header("Fluid Interaction")]
        [Tooltip("Multiplier for how strongly this object affects the fluid (0-1)")]
        [Range(0f, 2f)]
        [SerializeField] private float velocityScale = 1f;

        [Tooltip("Scale multiplier for the obstacle bounds")]
        [Range(0.1f, 5f)]
        [SerializeField] private float boundsScale = 1f;

        [Header("Bounds Override")]
        [Tooltip("Use manual bounds instead of reading from collider")]
        [SerializeField] private bool useManualBounds = false;

        [Tooltip("Manual half-extents when not using collider bounds")]
        [SerializeField] private Vector2 manualHalfExtents = new Vector2(1f, 0.5f);

        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private Vector2 _cachedHalfExtents;
        private bool _boundsInitialized = false;
        private bool _registeredWithManager = false;

        #region IFluidObstacle Implementation

        public Vector2 Position => _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;

        public Vector2 Velocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector2.zero;

        public Vector2 HalfExtents
        {
            get
            {
                if (!_boundsInitialized)
                {
                    UpdateBoundsCache();
                }
                return _cachedHalfExtents * boundsScale;
            }
        }

        public float Rotation => _rigidbody != null ? _rigidbody.rotation * Mathf.Deg2Rad : transform.eulerAngles.z * Mathf.Deg2Rad;

        public float VelocityScale => velocityScale;

        public bool IsActive => enabled && gameObject.activeInHierarchy;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            UpdateBoundsCache();
        }

        private void OnEnable()
        {
            RegisterWithManager();
        }

        private void OnDisable()
        {
            UnregisterFromManager();
        }

        private void Start()
        {
            // Handle late initialization (manager created after this component)
            if (enabled)
            {
                RegisterWithManager();
            }
        }

        private void OnValidate()
        {
            // Update bounds cache when inspector values change
            _boundsInitialized = false;
        }

        #endregion

        #region Registration

        private void RegisterWithManager()
        {
            if (_registeredWithManager) return;

            if (FluidSimulationManager.Instance != null)
            {
                FluidSimulationManager.Instance.RegisterObstacle(this);
                _registeredWithManager = true;
            }
        }

        private void UnregisterFromManager()
        {
            if (FluidSimulationManager.Instance != null)
            {
                FluidSimulationManager.Instance.UnregisterObstacle(this);
            }
            _registeredWithManager = false;
        }

        private void Update()
        {
            // Retry registration if manager wasn't available during OnEnable/Start
            if (!_registeredWithManager)
            {
                RegisterWithManager();
            }
        }

        #endregion

        #region Bounds Calculation

        private void UpdateBoundsCache()
        {
            if (useManualBounds)
            {
                _cachedHalfExtents = manualHalfExtents;
            }
            else if (_collider != null)
            {
                // Extract bounds from collider
                var bounds = _collider.bounds;
                _cachedHalfExtents = new Vector2(bounds.extents.x, bounds.extents.y);
            }
            else
            {
                // Fallback to default
                _cachedHalfExtents = Vector2.one;
            }

            _boundsInitialized = true;
        }

        /// <summary>
        /// Manually refresh the bounds cache (call after changing collider size at runtime).
        /// </summary>
        public void RefreshBounds()
        {
            _boundsInitialized = false;
            UpdateBoundsCache();
        }

        /// <summary>
        /// Set custom half-extents at runtime.
        /// </summary>
        public void SetHalfExtents(Vector2 halfExtents)
        {
            useManualBounds = true;
            manualHalfExtents = halfExtents;
            _cachedHalfExtents = halfExtents;
            _boundsInitialized = true;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!_boundsInitialized && _collider == null)
            {
                _cachedHalfExtents = useManualBounds ? manualHalfExtents : Vector2.one;
            }

            Vector2 extents = HalfExtents;
            Vector3 pos = transform.position;
            float rot = transform.eulerAngles.z;

            // Draw ellipse approximation
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0, 0, rot), Vector3.one);

            // Draw ellipse as wireframe
            int segments = 32;
            Vector3 prevPoint = new Vector3(extents.x, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 nextPoint = new Vector3(
                    Mathf.Cos(angle) * extents.x,
                    Mathf.Sin(angle) * extents.y,
                    0
                );
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }

            Gizmos.matrix = oldMatrix;
        }

        #endregion
    }
}

using StarfireV2.Pooling;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Physics-based missile behavior using thruster forces for movement and steering.
    /// A main rear thruster provides forward acceleration via AddForce, while two side
    /// steering thrusters apply lateral forces via AddForceAtPosition to create torque.
    /// Supports object pooling and can be shot down by point defense.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class V2ThrustMissileBehavior : MonoBehaviour, IPoolable, IV2DamageReceiver
    {
        private V2ThrustMissileConfig _config;
        private float _baseSpeed;
        private float _lifetime;

        private Rigidbody2D _rigidbody;
        private IEntityController _owner;
        private Transform _target;
        private float _elapsedTime;
        private float _targetUpdateTimer;
        private float _weavePhase;
        private bool _isActive;
        private bool _homingActive;

        // Health
        private float _currentHealth;

        // Cached target state
        private Vector2 _lastTargetPosition;
        private Vector2 _lastTargetVelocity;

        // Thruster states (current values for smooth ramping)
        private float _currentMainThrust;
        private float _currentLeftThrust;
        private float _currentRightThrust;
        private float _targetLeftThrust;
        private float _targetRightThrust;

        // Thruster visuals
        private ThrusterVisual _mainThrusterVisual;
        private ThrusterVisual _leftSteeringVisual;
        private ThrusterVisual _rightSteeringVisual;

        public bool IsActive => _isActive;
        public Transform Target => _target;
        public float CurrentHealth => _currentHealth;
        public bool CanBeTargeted => _config != null && _config.canBeTargeted;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        public void Initialize(
            IEntityController owner,
            V2ThrustMissileConfig config,
            float baseSpeed,
            float lifetime,
            Transform preLockedTarget = null)
        {
            _owner = owner;
            _config = config;
            _baseSpeed = baseSpeed;
            _lifetime = lifetime;
            _elapsedTime = 0f;
            _targetUpdateTimer = 0f;
            _homingActive = false;
            _isActive = true;
            _currentHealth = config.maxHealth;
            _currentMainThrust = 0f;
            _currentLeftThrust = 0f;
            _currentRightThrust = 0f;
            _targetLeftThrust = 0f;
            _targetRightThrust = 0f;

            _weavePhase = Random.Range(0f, _config.phaseRandomization);

            if (config.targetingMode == V2MissileTargetingMode.PreLocked && preLockedTarget != null)
            {
                _target = preLockedTarget;
                CacheTargetState();
            }
            else
            {
                _target = null;
            }

            CreateThrusterVisuals();
        }

        #region Pooling

        public bool OnPoolGet()
        {
            _isActive = true;
            _homingActive = false;
            _elapsedTime = 0f;
            _targetUpdateTimer = 0f;
            _target = null;
            _lastTargetPosition = Vector2.zero;
            _lastTargetVelocity = Vector2.zero;
            _currentHealth = 0f;
            _currentMainThrust = 0f;
            _currentLeftThrust = 0f;
            _currentRightThrust = 0f;
            _targetLeftThrust = 0f;
            _targetRightThrust = 0f;
            return true;
        }

        public void OnPoolReturn()
        {
            _isActive = false;
            _homingActive = false;
            _owner = null;
            _target = null;
            _config = null;

            DestroyThrusterVisuals();

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }
        }

        #endregion

        #region Damage

        public void ReceiveDamage(V2DamageInfo damageInfo)
        {
            if (!_isActive || _config == null) return;
            if (_config.maxHealth <= 0f) return;
            if (!_config.canBeTargeted) return;
            if (damageInfo.Source != null && damageInfo.Source == _owner) return;

            float damage = damageInfo.DamageConfig != null
                ? damageInfo.BaseDamage * damageInfo.DamageConfig.hullDamageMultiplier
                : damageInfo.BaseDamage;

            _currentHealth -= damage;

            if (_currentHealth <= 0f)
            {
                _isActive = false;
                var projectile = GetComponent<V2Projectile>();
                if (projectile != null)
                    projectile.ForceDestroy();
                else
                    Destroy(gameObject);
            }
        }

        #endregion

        #region Physics Update

        private void FixedUpdate()
        {
            if (!_isActive || _config == null) return;

            _elapsedTime += Time.fixedDeltaTime;

            // Activate homing after delay
            if (!_homingActive && _elapsedTime >= _config.homingDelay)
            {
                _homingActive = true;
            }

            // Update target
            if (_homingActive)
            {
                UpdateTarget();
            }

            // Main thruster — always firing once active
            ApplyMainThrust();

            // Steering thrusters — only when homing is active
            if (_homingActive)
            {
                Vector2 desiredDirection = CalculateDesiredDirection();
                ApplySteeringThrusters(desiredDirection);

                // Weaving via lateral force
                if (_config.enableWeaving)
                {
                    ApplyWeaving();
                }
            }
            else
            {
                _targetLeftThrust = 0f;
                _targetRightThrust = 0f;
            }

            // Smooth thrust transitions
            SmoothThrusts(Time.fixedDeltaTime);

            // Update rotation to face velocity
            UpdateRotation();

            // Update thruster visuals
            UpdateVisuals();
        }

        private void ApplyMainThrust()
        {
            Vector2 thrustForce = (Vector2)transform.up * _config.mainThrustForce;
            Vector2 thrusterWorldPos = transform.TransformPoint(_config.mainThrusterLocalPosition);
            _rigidbody.AddForceAtPosition(thrustForce, thrusterWorldPos, ForceMode2D.Force);
            _currentMainThrust = _config.mainThrustForce;
        }

        private void ApplySteeringThrusters(Vector2 desiredDirection)
        {
            float angleToTarget = Vector2.SignedAngle((Vector2)transform.up, desiredDirection);

            // Normalize turn demand to 0-1 range
            float turnDemand = Mathf.Clamp01(Mathf.Abs(angleToTarget) / 45f);

            if (angleToTarget > 1f)
            {
                // Need to turn left (counter-clockwise) — fire right thruster
                _targetRightThrust = turnDemand * _config.steeringThrustForce;
                _targetLeftThrust = 0f;
            }
            else if (angleToTarget < -1f)
            {
                // Need to turn right (clockwise) — fire left thruster
                _targetLeftThrust = turnDemand * _config.steeringThrustForce;
                _targetRightThrust = 0f;
            }
            else
            {
                _targetLeftThrust = 0f;
                _targetRightThrust = 0f;
            }

            // Apply left thruster force (pushes missile right)
            if (_currentLeftThrust > 0.01f)
            {
                Vector2 leftWorldPos = transform.TransformPoint(_config.leftThrusterLocalPosition);
                Vector2 leftForceDir = (Vector2)transform.right; // Push right
                _rigidbody.AddForceAtPosition(leftForceDir * _currentLeftThrust, leftWorldPos, ForceMode2D.Force);
            }

            // Apply right thruster force (pushes missile left)
            if (_currentRightThrust > 0.01f)
            {
                Vector2 rightWorldPos = transform.TransformPoint(_config.rightThrusterLocalPosition);
                Vector2 rightForceDir = -(Vector2)transform.right; // Push left
                _rigidbody.AddForceAtPosition(rightForceDir * _currentRightThrust, rightWorldPos, ForceMode2D.Force);
            }
        }

        private void ApplyWeaving()
        {
            float weaveOffset = Mathf.Sin(_elapsedTime * _config.weaveFrequency * Mathf.PI * 2f + _weavePhase);

            float decayFactor = 1f;
            if (_config.weaveDecayRate > 0f && _target != null)
            {
                float distToTarget = Vector2.Distance(transform.position, _target.position);
                float normalizedDist = Mathf.Clamp01(distToTarget / _config.acquisitionRange);
                decayFactor = Mathf.Pow(normalizedDist, _config.weaveDecayRate);
            }

            float weaveForce = weaveOffset * _config.weaveForceAmplitude * decayFactor;

            // Apply perpendicular force at center of mass (no torque, just lateral push)
            Vector2 perpendicular = new Vector2(-transform.up.y, transform.up.x);
            _rigidbody.AddForce(perpendicular * weaveForce, ForceMode2D.Force);
        }

        private void SmoothThrusts(float deltaTime)
        {
            float responseRate = _config.steeringResponseTime > 0f
                ? deltaTime / _config.steeringResponseTime
                : 1f;

            _currentLeftThrust = Mathf.Lerp(_currentLeftThrust, _targetLeftThrust, responseRate);
            _currentRightThrust = Mathf.Lerp(_currentRightThrust, _targetRightThrust, responseRate);
        }

        private void UpdateRotation()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            if (velocity.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        #endregion

        #region Targeting

        private void UpdateTarget()
        {
            _targetUpdateTimer -= Time.fixedDeltaTime;

            if (_targetUpdateTimer > 0f && _target != null && _target.gameObject.activeInHierarchy)
            {
                CacheTargetState();
                return;
            }

            _targetUpdateTimer = _config.targetUpdateInterval;

            if (_config.targetingMode == V2MissileTargetingMode.PreLocked &&
                _target != null && _target.gameObject.activeInHierarchy)
            {
                CacheTargetState();
                return;
            }

            _target = FindBestTarget();
            if (_target != null)
            {
                CacheTargetState();
            }
        }

        private Transform FindBestTarget()
        {
            Vector2 position = transform.position;
            var colliders = Physics2D.OverlapCircleAll(position, _config.acquisitionRange, _config.targetLayers);

            Transform bestTarget = null;
            float bestDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col == null) continue;
                if (_owner != null && col.transform.IsChildOf(_owner.Transform)) continue;
                if (col.GetComponent<V2Projectile>() != null) continue;

                float distance = Vector2.Distance(position, col.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = col.transform;
                }
            }

            return bestTarget;
        }

        private void CacheTargetState()
        {
            if (_target == null) return;
            _lastTargetPosition = _target.position;
            var targetRb = _target.GetComponent<Rigidbody2D>();
            _lastTargetVelocity = targetRb != null ? targetRb.linearVelocity : Vector2.zero;
        }

        private Vector2 CalculateDesiredDirection()
        {
            if (!_homingActive || _target == null || !_target.gameObject.activeInHierarchy)
            {
                return _rigidbody.linearVelocity.normalized;
            }

            Vector2 myPosition = transform.position;
            Vector2 targetPos = _config.predictTargetPosition
                ? PredictInterceptPoint(myPosition)
                : _lastTargetPosition;

            Vector2 toTarget = targetPos - myPosition;
            return toTarget.sqrMagnitude < 0.001f
                ? _rigidbody.linearVelocity.normalized
                : toTarget.normalized;
        }

        private Vector2 PredictInterceptPoint(Vector2 myPosition)
        {
            Vector2 toTarget = _lastTargetPosition - myPosition;
            float distance = toTarget.magnitude;
            float currentSpeed = _rigidbody.linearVelocity.magnitude;
            if (currentSpeed < 0.001f) currentSpeed = _baseSpeed;

            float timeToHit = distance / currentSpeed;
            return _lastTargetPosition + _lastTargetVelocity * timeToHit;
        }

        #endregion

        #region Visuals

        private void CreateThrusterVisuals()
        {
            if (_config.thrusterVisualConfig == null) return;

            // Main thruster at rear
            var mainGO = new GameObject("MainThruster");
            mainGO.transform.SetParent(transform, false);
            mainGO.transform.localPosition = (Vector3)_config.mainThrusterLocalPosition;
            mainGO.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // Point backward
            _mainThrusterVisual = ThrusterVisual.CreateFromCode(
                mainGO.transform, _config.thrusterVisualConfig, _rigidbody);

            // Left steering thruster
            var leftGO = new GameObject("LeftSteeringThruster");
            leftGO.transform.SetParent(transform, false);
            leftGO.transform.localPosition = (Vector3)_config.leftThrusterLocalPosition;
            leftGO.transform.localRotation = Quaternion.Euler(0f, 0f, -90f); // Point right
            _leftSteeringVisual = ThrusterVisual.CreateFromCode(
                leftGO.transform, _config.thrusterVisualConfig, _rigidbody);

            // Right steering thruster
            var rightGO = new GameObject("RightSteeringThruster");
            rightGO.transform.SetParent(transform, false);
            rightGO.transform.localPosition = (Vector3)_config.rightThrusterLocalPosition;
            rightGO.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // Point left
            _rightSteeringVisual = ThrusterVisual.CreateFromCode(
                rightGO.transform, _config.thrusterVisualConfig, _rigidbody);
        }

        private void DestroyThrusterVisuals()
        {
            if (_mainThrusterVisual != null)
            {
                Destroy(_mainThrusterVisual.transform.parent.gameObject);
                _mainThrusterVisual = null;
            }
            if (_leftSteeringVisual != null)
            {
                Destroy(_leftSteeringVisual.transform.parent.gameObject);
                _leftSteeringVisual = null;
            }
            if (_rightSteeringVisual != null)
            {
                Destroy(_rightSteeringVisual.transform.parent.gameObject);
                _rightSteeringVisual = null;
            }
        }

        private void UpdateVisuals()
        {
            if (_mainThrusterVisual != null)
                _mainThrusterVisual.SetIntensity(1f);

            if (_leftSteeringVisual != null)
            {
                float intensity = _config.steeringThrustForce > 0f
                    ? _currentLeftThrust / _config.steeringThrustForce
                    : 0f;
                _leftSteeringVisual.SetIntensity(intensity);
            }

            if (_rightSteeringVisual != null)
            {
                float intensity = _config.steeringThrustForce > 0f
                    ? _currentRightThrust / _config.steeringThrustForce
                    : 0f;
                _rightSteeringVisual.SetIntensity(intensity);
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !_isActive) return;

            // Acquisition range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _config?.acquisitionRange ?? 50f);

            // Line to target
            if (_target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _target.position);

                if (_config != null && _config.predictTargetPosition)
                {
                    Vector2 predicted = PredictInterceptPoint(transform.position);
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(predicted, 0.5f);
                }
            }

            // Thruster positions
            if (_config != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.TransformPoint(_config.mainThrusterLocalPosition), 0.05f);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.TransformPoint(_config.leftThrusterLocalPosition), 0.03f);
                Gizmos.DrawWireSphere(transform.TransformPoint(_config.rightThrusterLocalPosition), 0.03f);
            }
        }
#endif
    }
}

using StarfireV2.Pooling;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Behavior component for homing missiles with weaving motion.
    /// Attaches to physics projectiles and applies steering forces to track targets.
    /// Implements IV2DamageReceiver to allow missiles to be shot down by point defense.
    /// Supports object pooling via IPoolable interface.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class V2MissileBehavior : MonoBehaviour, IPoolable, IV2DamageReceiver
    {
        private V2MissileConfig _config;
        private float _baseSpeed;
        private float _lifetime;

        private Rigidbody2D _rigidbody;
        private Transform _target;
        private IEntityController _owner;
        private float _elapsedTime;
        private float _targetUpdateTimer;
        private float _weavePhase;
        private bool _isActive;
        private bool _homingActive;

        // Health tracking
        private float _currentHealth;

        // Cached target state for prediction
        private Vector2 _lastTargetPosition;
        private Vector2 _lastTargetVelocity;

        /// <summary>
        /// Whether this missile behavior is currently active.
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// The current target being tracked, if any.
        /// </summary>
        public Transform Target => _target;

        /// <summary>
        /// Current health of the missile. When reduced to 0, the missile is destroyed.
        /// </summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>
        /// Whether this missile can be targeted and damaged by enemy defenses.
        /// </summary>
        public bool CanBeTargeted => _config != null && _config.canBeTargeted;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// Initializes the missile behavior with configuration and launch parameters.
        /// </summary>
        /// <param name="owner">The entity that fired this missile.</param>
        /// <param name="config">Missile configuration parameters.</param>
        /// <param name="baseSpeed">Base projectile speed from V2ProjectileConfig.</param>
        /// <param name="lifetime">Projectile lifetime in seconds.</param>
        /// <param name="preLockedTarget">Optional pre-locked target for PreLocked targeting mode.</param>
        public void Initialize(
            IEntityController owner,
            V2MissileConfig config,
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

            // Initialize health
            _currentHealth = config.maxHealth;

            // Generate random phase offset for weaving variety
            _weavePhase = Random.Range(0f, _config.phaseRandomization);

            // Handle pre-locked target
            if (config.targetingMode == V2MissileTargetingMode.PreLocked && preLockedTarget != null)
            {
                _target = preLockedTarget;
                CacheTargetState();
            }
            else
            {
                _target = null;
            }
        }

        public bool OnPoolGet()
        {
            _isActive = true;
            _homingActive = false;
            _elapsedTime = 0f;
            _targetUpdateTimer = 0f;
            _target = null;
            _lastTargetPosition = Vector2.zero;
            _lastTargetVelocity = Vector2.zero;
            _currentHealth = 0f; // Will be set properly in Initialize
            return true;
        }

        public void OnPoolReturn()
        {
            _isActive = false;
            _homingActive = false;
            _owner = null;
            _target = null;
            _config = null;
            _elapsedTime = 0f;
            _targetUpdateTimer = 0f;
            _weavePhase = 0f;
            _currentHealth = 0f;
            _lastTargetPosition = Vector2.zero;
            _lastTargetVelocity = Vector2.zero;
        }

        /// <summary>
        /// Receives damage from external sources (e.g., point defense weapons).
        /// When health reaches 0, the missile is destroyed.
        /// </summary>
        public void ReceiveDamage(V2DamageInfo damageInfo)
        {
            if (!_isActive || _config == null) return;

            // Invulnerable missiles (maxHealth <= 0) cannot be damaged
            if (_config.maxHealth <= 0f) return;

            // Cannot be damaged if not targetable
            if (!_config.canBeTargeted) return;

            // Don't take damage from our own owner
            if (damageInfo.Source != null && damageInfo.Source == _owner) return;

            // Apply damage (missiles don't have shields, so use hull damage calculation)
            float damage = damageInfo.DamageConfig != null
                ? damageInfo.BaseDamage * damageInfo.DamageConfig.hullDamageMultiplier
                : damageInfo.BaseDamage;

            _currentHealth -= damage;

            if (_currentHealth <= 0f)
            {
                DestroyMissile();
            }
        }

        /// <summary>
        /// Destroys the missile, triggering the associated V2Projectile's destruction.
        /// </summary>
        private void DestroyMissile()
        {
            _isActive = false;

            // Get the V2Projectile component to handle proper destruction/pooling
            var projectile = GetComponent<V2Projectile>();
            if (projectile != null)
            {
                // Force the projectile to return to pool or destroy
                projectile.ForceDestroy();
            }
            else
            {
                // Fallback: destroy the game object directly
                Destroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
            if (!_isActive || _config == null) return;

            _elapsedTime += Time.fixedDeltaTime;

            // Check if homing should activate
            if (!_homingActive && _elapsedTime >= _config.homingDelay)
            {
                _homingActive = true;
            }

            // Update target periodically
            if (_homingActive)
            {
                UpdateTarget();
            }

            // Calculate current speed based on acceleration curve
            float speedMultiplier = _config.enableAcceleration
                ? _config.speedCurve.Evaluate(_elapsedTime / _lifetime)
                : 1f;
            float currentSpeed = _baseSpeed * speedMultiplier;

            // Calculate desired direction
            Vector2 desiredDirection = CalculateDesiredDirection();

            // Apply weaving offset
            if (_config.enableWeaving)
            {
                desiredDirection = ApplyWeaving(desiredDirection);
            }

            // Steer towards desired direction
            SteerTowards(desiredDirection, currentSpeed);

            // Update rotation to face velocity
            UpdateRotation();
        }

        private void UpdateTarget()
        {
            _targetUpdateTimer -= Time.fixedDeltaTime;

            // Only update if timer expired or no target
            if (_targetUpdateTimer > 0f && _target != null && _target.gameObject.activeInHierarchy)
            {
                // Just update cached state
                CacheTargetState();
                return;
            }

            _targetUpdateTimer = _config.targetUpdateInterval;

            // If we have a valid pre-locked target, keep it
            if (_config.targetingMode == V2MissileTargetingMode.PreLocked &&
                _target != null && _target.gameObject.activeInHierarchy)
            {
                CacheTargetState();
                return;
            }

            // Find new target (AutoAcquire or fallback for lost PreLocked target)
            _target = FindBestTarget();

            if (_target != null)
            {
                CacheTargetState();
            }
        }

        private Transform FindBestTarget()
        {
            Vector2 position = transform.position;

            // Find all potential targets in acquisition range
            var colliders = Physics2D.OverlapCircleAll(
                position,
                _config.acquisitionRange,
                _config.targetLayers
            );

            Transform bestTarget = null;
            float bestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                if (collider == null) continue;

                // Skip if owned by us (or child of owner)
                if (_owner != null && collider.transform.IsChildOf(_owner.Transform))
                {
                    continue;
                }

                // Skip other projectiles
                if (collider.GetComponent<V2Projectile>() != null)
                {
                    continue;
                }

                // Skip if not an entity (optional - could remove to target anything)
                // var entity = collider.GetComponentInParent<IEntity>();
                // if (entity == null) continue;

                float distance = Vector2.Distance(position, collider.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = collider.transform;
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
            // If homing not active or no target, continue in current direction
            if (!_homingActive || _target == null || !_target.gameObject.activeInHierarchy)
            {
                return _rigidbody.linearVelocity.normalized;
            }

            Vector2 myPosition = transform.position;
            Vector2 targetPos;

            if (_config.predictTargetPosition)
            {
                targetPos = PredictInterceptPoint(myPosition);
            }
            else
            {
                targetPos = _lastTargetPosition;
            }

            Vector2 toTarget = targetPos - myPosition;
            if (toTarget.sqrMagnitude < 0.001f)
            {
                return _rigidbody.linearVelocity.normalized;
            }

            return toTarget.normalized;
        }

        private Vector2 PredictInterceptPoint(Vector2 myPosition)
        {
            // Simple linear prediction based on target velocity and distance
            Vector2 toTarget = _lastTargetPosition - myPosition;
            float distance = toTarget.magnitude;

            // Estimate time to reach target at current speed
            float currentSpeed = _rigidbody.linearVelocity.magnitude;
            if (currentSpeed < 0.001f) currentSpeed = _baseSpeed;

            float timeToHit = distance / currentSpeed;

            // Predict where target will be
            return _lastTargetPosition + _lastTargetVelocity * timeToHit;
        }

        private Vector2 ApplyWeaving(Vector2 desiredDirection)
        {
            // Calculate sinusoidal offset
            float weaveOffset = Mathf.Sin(_elapsedTime * _config.weaveFrequency * Mathf.PI * 2f + _weavePhase);

            // Calculate decay factor based on distance to target
            float decayFactor = 1f;
            if (_config.weaveDecayRate > 0f && _target != null)
            {
                float distToTarget = Vector2.Distance(transform.position, _target.position);
                float normalizedDist = Mathf.Clamp01(distToTarget / _config.acquisitionRange);
                decayFactor = Mathf.Pow(normalizedDist, _config.weaveDecayRate);
            }

            float amplitude = _config.weaveAmplitude * weaveOffset * decayFactor;

            // Get perpendicular direction (90 degrees rotated)
            Vector2 perpendicular = new Vector2(-desiredDirection.y, desiredDirection.x);

            // Blend weave with desired direction
            // Use a small factor to influence direction rather than directly adding offset
            Vector2 weavedDirection = desiredDirection + perpendicular * amplitude * 0.15f;

            return weavedDirection.normalized;
        }

        private void SteerTowards(Vector2 desiredDirection, float speed)
        {
            Vector2 currentVelocity = _rigidbody.linearVelocity;
            Vector2 currentDirection = currentVelocity.normalized;

            // Handle zero velocity case
            if (currentVelocity.sqrMagnitude < 0.001f)
            {
                _rigidbody.linearVelocity = desiredDirection * speed;
                return;
            }

            // Calculate turn amount limited by turn rate
            float maxTurnThisFrame = _config.turnRate * Time.fixedDeltaTime;
            float angleToTarget = Vector2.SignedAngle(currentDirection, desiredDirection);
            float turnAmount = Mathf.Clamp(angleToTarget, -maxTurnThisFrame, maxTurnThisFrame);

            // Rotate current direction by turn amount
            Vector2 newDirection = RotateVector(currentDirection, turnAmount);

            // Set new velocity
            _rigidbody.linearVelocity = newDirection * speed;
        }

        private void UpdateRotation()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            if (velocity.sqrMagnitude > 0.01f)
            {
                // Point sprite "up" direction towards velocity
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private static Vector2 RotateVector(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !_isActive) return;

            // Draw acquisition range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _config?.acquisitionRange ?? 50f);

            // Draw line to target
            if (_target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _target.position);

                // Draw predicted intercept point
                if (_config != null && _config.predictTargetPosition)
                {
                    Vector2 predicted = PredictInterceptPoint(transform.position);
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(predicted, 0.5f);
                    Gizmos.DrawLine(transform.position, predicted);
                }
            }
        }
#endif
    }
}

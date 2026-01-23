using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Handles the visual representation of a weapon, including turret rotation and muzzle tracking.
    /// Attach to weapon visual prefabs instantiated at hardpoints.
    /// </summary>
    public class V2WeaponVisual : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("The transform where projectiles spawn from.")]
        [SerializeField] private Transform muzzlePoint;

        [Tooltip("The sprite renderer for the weapon visual.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private V2TurretSettings _settings;
        private float _currentRotation;
        private Vector2 _targetDirection = Vector2.up;
        private bool _isTurret;
        private float _halfFiringArc;

        /// <summary>
        /// Initializes the weapon visual with turret settings.
        /// </summary>
        /// <param name="settings">The turret configuration settings.</param>
        public void Initialize(V2TurretSettings settings)
        {
            _settings = settings ?? V2TurretSettings.Fixed;
            _isTurret = _settings.isTurret;
            _halfFiringArc = _settings.firingArc / 2f;
            _currentRotation = transform.localEulerAngles.z;
        }

        /// <summary>
        /// Sets the target direction for turret aiming.
        /// </summary>
        /// <param name="worldDirection">The direction to aim in world space.</param>
        public void SetTargetDirection(Vector2 worldDirection)
        {
            if (worldDirection.sqrMagnitude > 0.001f)
            {
                _targetDirection = worldDirection.normalized;
            }
        }

        /// <summary>
        /// Checks if the weapon is aimed within tolerance of the target.
        /// Always returns true for fixed (non-turret) weapons.
        /// </summary>
        /// <returns>True if aimed at target, false otherwise.</returns>
        public bool IsAimedAtTarget()
        {
            if (!_isTurret) return true;

            Vector2 currentDir = GetMuzzleDirection();
            float angle = Vector2.Angle(currentDir, _targetDirection);
            return angle <= _settings.firingTolerance;
        }

        /// <summary>
        /// Gets the world-space position of the muzzle point.
        /// Falls back to transform position if no muzzle point is assigned.
        /// </summary>
        /// <returns>The muzzle position in world space.</returns>
        public Vector2 GetMuzzlePosition()
        {
            return muzzlePoint != null ? muzzlePoint.position : transform.position;
        }

        /// <summary>
        /// Gets the world-space firing direction from the muzzle.
        /// </summary>
        /// <returns>The normalized firing direction in world space.</returns>
        public Vector2 GetMuzzleDirection()
        {
            if (muzzlePoint != null)
            {
                return muzzlePoint.up;
            }

            // Default to transform up (compensate for sprite orientation)
            return transform.up;
        }

        private void Update()
        {
            if (!_isTurret || _settings == null) return;

            UpdateTurretRotation();
        }

        private void UpdateTurretRotation()
        {
            // Convert target direction to local space relative to parent
            Vector2 localTarget = _targetDirection;
            if (transform.parent != null)
            {
                localTarget = transform.parent.InverseTransformDirection(_targetDirection);
            }

            // Calculate target angle (accounting for sprite orientation - typically -90 offset)
            float targetAngle = Mathf.Atan2(localTarget.y, localTarget.x) * Mathf.Rad2Deg - 90f;

            // Clamp to firing arc if limited
            if (_settings.firingArc < 360f)
            {
                targetAngle = ClampToFiringArc(targetAngle);
            }

            // Smoothly rotate towards target
            _currentRotation = Mathf.MoveTowardsAngle(
                _currentRotation,
                targetAngle,
                _settings.rotationSpeed * Time.deltaTime
            );

            // Apply rotation
            transform.localRotation = Quaternion.Euler(0f, 0f, _currentRotation);
        }

        private float ClampToFiringArc(float angle)
        {
            // Normalize angle to -180 to 180 range
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;

            // Clamp to half arc on either side of center (0 = forward)
            return Mathf.Clamp(angle, -_halfFiringArc, _halfFiringArc);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw muzzle point
            if (muzzlePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(muzzlePoint.position, 0.05f);
                Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.up * 0.3f);
            }

            // Draw current aim direction
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, GetMuzzleDirection() * 0.4f);

            // Draw target direction
            if (_targetDirection.sqrMagnitude > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, _targetDirection * 0.3f);
            }
        }
#endif
    }
}

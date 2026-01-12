using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Visual representation of a weapon, instantiated at hardpoints.
    /// Handles turret rotation and provides muzzle point for projectile spawning.
    /// </summary>
    public class WeaponVisual : MonoBehaviour
    {
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private TurretSettings _turretSettings;
        private float _currentRotation;
        private Vector2 _targetDirection;
        private bool _isTurret;
        private float _baseAngle;

        public Transform MuzzlePoint => muzzlePoint;
        public SpriteRenderer SpriteRenderer => spriteRenderer;

        /// <summary>
        /// Initialize the weapon visual with turret settings.
        /// </summary>
        /// <param name="settings">Turret settings, or null for fixed weapons</param>
        public void Initialize(TurretSettings settings)
        {
            _turretSettings = settings;
            _isTurret = settings != null && settings.isTurret;
            _currentRotation = transform.localEulerAngles.z;
            _baseAngle = transform.parent != null ? transform.parent.eulerAngles.z : 0f;
        }

        /// <summary>
        /// Set the target direction for turret aiming (world space).
        /// </summary>
        public void SetTargetDirection(Vector2 worldDirection)
        {
            _targetDirection = worldDirection;
        }

        /// <summary>
        /// Get the current muzzle firing direction (world space).
        /// </summary>
        public Vector2 GetMuzzleDirection()
        {
            if (muzzlePoint != null)
                return muzzlePoint.up;
            return transform.up;
        }

        /// <summary>
        /// Get the current muzzle position (world space).
        /// </summary>
        public Vector2 GetMuzzlePosition()
        {
            return muzzlePoint != null
                ? (Vector2)muzzlePoint.position
                : (Vector2)transform.position;
        }

        /// <summary>
        /// Check if the turret is currently aimed at the target (within tolerance).
        /// Always returns true for fixed weapons.
        /// </summary>
        public bool IsAimedAtTarget()
        {
            if (!_isTurret) return true;
            if (_targetDirection.sqrMagnitude < 0.001f) return true;

            var currentDir = GetMuzzleDirection();
            float angle = Vector2.Angle(currentDir, _targetDirection);
            return angle <= _turretSettings.firingTolerance;
        }

        private void Update()
        {
            if (!_isTurret) return;

            UpdateTurretRotation();
        }

        private void UpdateTurretRotation()
        {
            if (_targetDirection.sqrMagnitude < 0.001f) return;

            // Calculate target angle (accounting for sprite orientation)
            float targetAngle = Mathf.Atan2(_targetDirection.y, _targetDirection.x) * Mathf.Rad2Deg - 90f;

            // Clamp to firing arc if limited
            if (_turretSettings.firingArc < 360f)
            {
                float parentAngle = transform.parent != null ? transform.parent.eulerAngles.z : 0f;
                float relativeTarget = Mathf.DeltaAngle(parentAngle, targetAngle);
                float halfArc = _turretSettings.firingArc * 0.5f;
                relativeTarget = Mathf.Clamp(relativeTarget, -halfArc, halfArc);
                targetAngle = parentAngle + relativeTarget;
            }

            // Smooth rotation toward target
            _currentRotation = Mathf.MoveTowardsAngle(
                _currentRotation,
                targetAngle,
                _turretSettings.rotationSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Euler(0, 0, _currentRotation);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw muzzle direction
            Gizmos.color = Color.red;
            Vector3 muzzlePos = muzzlePoint != null ? muzzlePoint.position : transform.position;
            Vector3 muzzleDir = muzzlePoint != null ? muzzlePoint.up : transform.up;
            Gizmos.DrawRay(muzzlePos, muzzleDir * 0.5f);

            // Draw target direction if in play mode
            if (Application.isPlaying && _targetDirection.sqrMagnitude > 0.001f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(muzzlePos, (Vector3)_targetDirection.normalized * 0.5f);
            }
        }
    }
}

using System;
using Starfire.Entity.Modules.Hull;
using Starfire.Entity.Modules.Shield;
using UnityEngine;

namespace Starfire.Entity.Modules.Damage
{
    /// <summary>
    /// Central damage processing component. Handles routing damage through shields to hull.
    /// Attach to any entity that can receive damage.
    /// </summary>
    public class DamageProcessor : MonoBehaviour, IDamageReceiver
    {
        private EntityControllerBase _controller;

        // Cached module references (updated when modules change)
        private IShieldModule _shield;
        private IHullModule _hull;

        [Header("Settings")]
        [SerializeField] private bool _invulnerable = false;
        [SerializeField] private float _damageMultiplier = 1f;

        public EntityControllerBase Controller => _controller;
        public bool CanReceiveDamage => !_invulnerable && _hull != null && !_hull.IsDestroyed;

        public event Action<DamageResult> OnDamageReceived;
        public event Action<DamageResult> OnDestroyed;

        private void Awake()
        {
            _controller = GetComponent<EntityControllerBase>();
        }

        private void Start()
        {
            RefreshModuleReferences();

            // Subscribe to hull destruction
            if (_hull != null)
            {
                _hull.OnHullDestroyed += HandleHullDestroyed;
            }
        }

        /// <summary>
        /// Call this when modules are changed to update cached references.
        /// </summary>
        public void RefreshModuleReferences()
        {
            // Unsubscribe from old hull if exists
            if (_hull != null)
            {
                _hull.OnHullDestroyed -= HandleHullDestroyed;
            }

            // Get references from ShipSystems
            if (_controller?.Systems is ShipSystems shipSystems)
            {
                _shield = shipSystems.PrimaryShield;
                _hull = shipSystems.PrimaryHull;
                Debug.Log($"[DamageProcessor] {gameObject.name} RefreshModuleReferences: Shield={(_shield != null ? "found" : "null")}, Hull={(_hull != null ? "found" : "null")}");
            }
            else
            {
                Debug.LogWarning($"[DamageProcessor] {gameObject.name} RefreshModuleReferences: Controller={_controller}, Systems={_controller?.Systems}");
            }

            // Subscribe to new hull
            if (_hull != null)
            {
                _hull.OnHullDestroyed += HandleHullDestroyed;
            }
        }

        public DamageResult ReceiveDamage(DamageInfo damageInfo)
        {
            // Lazy initialization - refresh if hull not found yet (handles initialization order issues)
            if (_hull == null)
            {
                RefreshModuleReferences();
            }

            if (!CanReceiveDamage)
            {
                Debug.LogWarning($"[DamageProcessor] {gameObject.name} CanReceiveDamage=false (invulnerable={_invulnerable}, hull={(_hull != null ? $"exists, IsDestroyed={_hull.IsDestroyed}" : "null")})");
                return DamageResult.None(damageInfo);
            }

            // Apply global damage multiplier
            float scaledBaseDamage = damageInfo.BaseDamage * _damageMultiplier;
            var scaledDamage = damageInfo.WithBaseDamage(scaledBaseDamage);

            float shieldDamage = 0f;
            float bleedthrough = 0f;
            float hullDamage = 0f;
            bool shieldsDestroyed = false;
            bool entityDestroyed = false;

            // Step 1: Process shield damage (if not bypassed and shields available)
            if (!scaledDamage.BypassesShield && _shield != null && _shield.CurrentShield > 0)
            {
                float damageToShield = scaledDamage.BaseDamage * scaledDamage.ShieldDamageMultiplier;
                shieldDamage = Mathf.Min(damageToShield, _shield.CurrentShield);
                bleedthrough = _shield.AbsorbDamage(scaledDamage);
                shieldsDestroyed = _shield.State == ShieldState.Destroyed;
            }
            else
            {
                // No shield or bypasses shield - all damage goes to hull
                bleedthrough = scaledDamage.BaseDamage;
            }

            // Step 2: Process hull damage (bleedthrough)
            if (_hull != null && bleedthrough > 0)
            {
                var hullDamageInfo = new DamageInfo(
                    bleedthrough,
                    scaledDamage.Type,
                    1f, // Shield multiplier doesn't apply to hull
                    scaledDamage.HullDamageMultiplier,
                    scaledDamage.Source,
                    scaledDamage.SourcePosition,
                    scaledDamage.Direction,
                    true // Already past shields
                );

                hullDamage = _hull.TakeDamage(hullDamageInfo);
                entityDestroyed = _hull.IsDestroyed;
            }

            var result = new DamageResult(
                scaledDamage,
                shieldDamage,
                hullDamage,
                bleedthrough,
                shieldsDestroyed,
                entityDestroyed
            );

            // Fire events
            OnDamageReceived?.Invoke(result);

            if (entityDestroyed)
            {
                OnDestroyed?.Invoke(result);
            }

            return result;
        }

        private void HandleHullDestroyed()
        {
            // This is called from the hull module when it's destroyed
            // The OnDestroyed event is fired from ReceiveDamage, so we don't need to do anything here
        }

        /// <summary>
        /// Set whether this entity is invulnerable to damage.
        /// </summary>
        public void SetInvulnerable(bool invulnerable)
        {
            _invulnerable = invulnerable;
        }

        /// <summary>
        /// Set the global damage multiplier (1.0 = normal, 2.0 = double damage, etc.).
        /// </summary>
        public void SetDamageMultiplier(float multiplier)
        {
            _damageMultiplier = Mathf.Max(0f, multiplier);
        }

        private void OnDestroy()
        {
            if (_hull != null)
            {
                _hull.OnHullDestroyed -= HandleHullDestroyed;
            }
        }
    }
}

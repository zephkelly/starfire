using System;
using System.Collections;
using Starfire.Entity.Modules.Damage;
using UnityEngine;

namespace Starfire.Entity
{
    /// <summary>
    /// Handles ship destruction sequence including effects, drops, and cleanup.
    /// Attach to ship GameObject alongside DamageProcessor.
    /// </summary>
    public class ShipDeathHandler : MonoBehaviour
    {
        [Header("Death Settings")]
        [Tooltip("Delay in seconds before the ship is destroyed after death")]
        [SerializeField] private float _destructionDelay = 0.1f;

        [Tooltip("If true, disables ship controller and colliders on death")]
        [SerializeField] private bool _disableOnDeath = true;

        [Header("Effects")]
        [Tooltip("Prefab spawned at ship position when destroyed")]
        [SerializeField] private GameObject _explosionPrefab;

        [Tooltip("Sound played when ship is destroyed")]
        [SerializeField] private AudioClip _explosionSound;

        [Tooltip("Volume of the explosion sound")]
        [Range(0f, 1f)]
        [SerializeField] private float _explosionVolume = 1f;

        private DamageProcessor _damageProcessor;
        private EntityControllerBase _controller;
        private bool _isDying = false;

        /// <summary>
        /// Fired when death sequence starts (before destruction).
        /// Use for spawning drops, updating score, etc.
        /// </summary>
        public event Action<EntityControllerBase, DamageResult> OnDeathStarted;

        /// <summary>
        /// Fired just before GameObject is destroyed.
        /// Use for final cleanup.
        /// </summary>
        public event Action<EntityControllerBase> OnDeathComplete;

        private void Awake()
        {
            _damageProcessor = GetComponent<DamageProcessor>();
            _controller = GetComponent<EntityControllerBase>();
        }

        private void Start()
        {
            if (_damageProcessor != null)
            {
                _damageProcessor.OnDestroyed += HandleDestroyed;
            }
            else
            {
                Debug.LogWarning($"ShipDeathHandler on '{gameObject.name}' requires a DamageProcessor component");
            }
        }

        private void HandleDestroyed(DamageResult result)
        {
            if (_isDying) return;
            _isDying = true;

            OnDeathStarted?.Invoke(_controller, result);

            if (_disableOnDeath)
            {
                DisableShip();
            }

            SpawnDeathEffects();
            StartCoroutine(DestroyAfterDelay());
        }

        private void DisableShip()
        {
            // Disable ship controller
            if (_controller != null)
            {
                _controller.enabled = false;
            }

            // Disable all colliders to prevent further interactions
            foreach (var col in GetComponentsInChildren<Collider2D>())
            {
                col.enabled = false;
            }

            // Stop any rigidbody movement
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }
        }

        private void SpawnDeathEffects()
        {
            // Spawn explosion prefab
            if (_explosionPrefab != null)
            {
                Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
            }

            // Play explosion sound
            if (_explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(_explosionSound, transform.position, _explosionVolume);
            }
        }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(_destructionDelay);

            OnDeathComplete?.Invoke(_controller);
            Destroy(gameObject);
        }

        /// <summary>
        /// Check if the ship is in the process of dying.
        /// </summary>
        public bool IsDying => _isDying;

        private void OnDestroy()
        {
            if (_damageProcessor != null)
            {
                _damageProcessor.OnDestroyed -= HandleDestroyed;
            }
        }
    }
}

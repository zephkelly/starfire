using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarfireV2.Pooling
{
    /// <summary>
    /// Central manager for all projectile pools.
    /// Singleton that manages pools keyed by projectile prefab.
    /// Automatically creates and sizes pools based on weapon usage.
    /// </summary>
    public class ProjectilePoolManager : MonoBehaviour
    {
        public static ProjectilePoolManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private ProjectilePoolConfig config;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo;

        // Pool storage keyed by prefab instance ID
        private readonly Dictionary<int, PrefabPool> _physicsPools = new();
        private readonly Dictionary<int, PrefabPool> _raycastPools = new();
        private PrefabPool _hitscanBeamPool;

        // Prefab reference lookup (InstanceID -> prefab)
        private readonly Dictionary<int, GameObject> _prefabLookup = new();

        // Track entity count per prefab for dynamic sizing
        private readonly Dictionary<int, int> _prefabEntityCount = new();

        // Pool container for organization
        private Transform _poolContainer;

        // Batch update tracking for active physics projectiles
        private readonly List<V2Projectile> _activeProjectiles = new();
        private bool _batchUpdateEnabled = true;

        // Events for monitoring
        public event Action<GameObject, int> OnPoolCreated;
        public event Action<GameObject, int, int> OnPoolStatsChanged; // prefab, pooled, active

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Create container for pooled objects
            var containerGO = new GameObject("ProjectilePoolContainer");
            containerGO.transform.SetParent(transform);
            _poolContainer = containerGO.transform;

            // Use default config if none assigned
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ProjectilePoolConfig>();
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneUnloaded += HandleSceneUnloaded;

            // Subscribe to entity registration if available
            if (EntityRegistry.Instance != null)
            {
                EntityRegistry.Instance.OnEntityRegistered += HandleEntityRegistered;
                EntityRegistry.Instance.OnEntityUnregistered += HandleEntityUnregistered;
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;

            if (EntityRegistry.Instance != null)
            {
                EntityRegistry.Instance.OnEntityRegistered -= HandleEntityRegistered;
                EntityRegistry.Instance.OnEntityUnregistered -= HandleEntityUnregistered;
            }
        }

        private void Update()
        {
            if (!_batchUpdateEnabled || _activeProjectiles.Count == 0) return;

            float dt = Time.deltaTime;
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                var proj = _activeProjectiles[i];
                if (proj == null || !proj.IsActive)
                {
                    _activeProjectiles.RemoveAt(i);
                    continue;
                }

                if (proj.TickLifetime(dt))
                {
                    _activeProjectiles.RemoveAt(i);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            DisposeAllPools();
        }

        #endregion

        #region Physics Projectile Pool

        /// <summary>
        /// Gets a physics projectile from the pool.
        /// Creates the pool if it doesn't exist.
        /// </summary>
        /// <param name="prefab">The projectile prefab.</param>
        /// <returns>An active GameObject from the pool.</returns>
        public GameObject GetPhysicsProjectile(GameObject prefab)
        {
            if (prefab == null) return null;

            int prefabId = prefab.GetInstanceID();

            if (!_physicsPools.TryGetValue(prefabId, out var pool))
            {
                pool = CreatePhysicsPool(prefab);
            }

            var instance = pool.Get();

            OnPoolStatsChanged?.Invoke(prefab, pool.PooledCount, pool.ActiveCount);

            return instance;
        }

        /// <summary>
        /// Returns a physics projectile to its pool.
        /// </summary>
        /// <param name="instance">The projectile instance.</param>
        /// <param name="prefab">The original prefab (for pool lookup).</param>
        public void ReturnPhysicsProjectile(GameObject instance, GameObject prefab)
        {
            if (instance == null || prefab == null) return;

            int prefabId = prefab.GetInstanceID();

            if (_physicsPools.TryGetValue(prefabId, out var pool))
            {
                pool.Return(instance);

                OnPoolStatsChanged?.Invoke(prefab, pool.PooledCount, pool.ActiveCount);
            }
            else
            {
                // No pool found, destroy the instance
                Destroy(instance);
            }
        }

        #endregion

        #region Raycast Projectile Pool

        /// <summary>
        /// Gets a raycast visual projectile from the pool.
        /// </summary>
        public GameObject GetRaycastProjectile(GameObject prefab)
        {
            if (prefab == null) return null;

            int prefabId = prefab.GetInstanceID();

            if (!_raycastPools.TryGetValue(prefabId, out var pool))
            {
                pool = CreateRaycastPool(prefab);
            }

            var instance = pool.Get();

            if (config.logPoolEvents)
            {
                Debug.Log($"[Pool] Get raycast projectile: {prefab.name} (pooled={pool.PooledCount}, active={pool.ActiveCount})");
            }

            return instance;
        }

        /// <summary>
        /// Returns a raycast visual projectile to its pool.
        /// </summary>
        public void ReturnRaycastProjectile(GameObject instance, GameObject prefab)
        {
            if (instance == null || prefab == null) return;

            int prefabId = prefab.GetInstanceID();

            if (_raycastPools.TryGetValue(prefabId, out var pool))
            {
                pool.Return(instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        #endregion

        #region Hitscan Beam Pool

        /// <summary>
        /// Gets a hitscan beam visual from the pool.
        /// </summary>
        public GameObject GetHitscanBeam()
        {
            if (_hitscanBeamPool == null)
            {
                CreateHitscanBeamPool();
            }

            return _hitscanBeamPool.Get();
        }

        /// <summary>
        /// Returns a hitscan beam visual to the pool.
        /// </summary>
        public void ReturnHitscanBeam(GameObject instance)
        {
            if (instance == null) return;

            if (_hitscanBeamPool != null)
            {
                _hitscanBeamPool.Return(instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        #endregion

        #region Batch Projectile Management

        /// <summary>
        /// Registers a physics projectile for batch lifetime updates.
        /// The projectile's Update() will be skipped in favour of the manager's batched tick.
        /// </summary>
        public void RegisterActiveProjectile(V2Projectile projectile)
        {
            if (projectile == null || !_batchUpdateEnabled) return;
            projectile.IsBatchManaged = true;
            _activeProjectiles.Add(projectile);
        }

        /// <summary>
        /// Removes a projectile from batch tracking (called on pool return).
        /// </summary>
        public void UnregisterActiveProjectile(V2Projectile projectile)
        {
            if (projectile == null) return;
            projectile.IsBatchManaged = false;
            // Don't search-remove here; the Update loop handles nulls/inactive entries
        }

        #endregion

        #region Pool Management

        /// <summary>
        /// Pre-warms a pool for a specific prefab.
        /// </summary>
        /// <param name="prefab">The prefab to pool.</param>
        /// <param name="count">Number of instances to pre-warm.</param>
        /// <param name="isPhysicsProjectile">True for physics projectiles, false for raycast visuals.</param>
        public void PrewarmPool(GameObject prefab, int count, bool isPhysicsProjectile = true)
        {
            if (prefab == null || count <= 0) return;

            int prefabId = prefab.GetInstanceID();
            Dictionary<int, PrefabPool> pools = isPhysicsProjectile ? _physicsPools : _raycastPools;

            if (pools.TryGetValue(prefabId, out var pool))
            {
                int needed = count - pool.TotalCreated;
                if (needed > 0)
                {
                    pool.ExpandPool(needed);
                }
            }
            else
            {
                if (isPhysicsProjectile)
                {
                    CreatePhysicsPool(prefab, count);
                }
                else
                {
                    CreateRaycastPool(prefab, count);
                }
            }

        }

        /// <summary>
        /// Registers a weapon config to size pools appropriately.
        /// Call this when an entity with weapons is spawned.
        /// </summary>
        public void RegisterWeaponConfig(
            GameObject projectilePrefab,
            float fireRate,
            float projectileLifetime,
            V2ProjectileMode mode)
        {
            if (projectilePrefab == null) return;

            int prefabId = projectilePrefab.GetInstanceID();

            // Track entity count
            if (!_prefabEntityCount.ContainsKey(prefabId))
            {
                _prefabEntityCount[prefabId] = 0;
            }
            _prefabEntityCount[prefabId]++;

            // Calculate needed pool size
            int entityCount = _prefabEntityCount[prefabId];
            int neededSize = config.CalculatePoolSize(fireRate, projectileLifetime, entityCount);

            // Get or create appropriate pool
            Dictionary<int, PrefabPool> pools = mode == V2ProjectileMode.Physics ? _physicsPools : _raycastPools;

            if (pools.TryGetValue(prefabId, out var pool))
            {
                int currentSize = pool.TotalCreated;
                if (neededSize > currentSize)
                {
                    pool.ExpandPool(neededSize - currentSize);
                }
            }
            else
            {
                if (mode == V2ProjectileMode.Physics)
                {
                    CreatePhysicsPool(projectilePrefab, neededSize);
                }
                else if (mode == V2ProjectileMode.RaycastBacked)
                {
                    CreateRaycastPool(projectilePrefab, neededSize);
                }
                // Hitscan doesn't use prefab-based pools
            }
        }

        /// <summary>
        /// Unregisters a weapon config when an entity is destroyed.
        /// </summary>
        public void UnregisterWeaponConfig(GameObject projectilePrefab)
        {
            if (projectilePrefab == null) return;

            int prefabId = projectilePrefab.GetInstanceID();

            if (_prefabEntityCount.ContainsKey(prefabId))
            {
                _prefabEntityCount[prefabId]--;
                if (_prefabEntityCount[prefabId] <= 0)
                {
                    _prefabEntityCount.Remove(prefabId);
                }
            }
        }

        /// <summary>
        /// Gets statistics for a specific pool.
        /// </summary>
        public PoolStats GetPoolStats(GameObject prefab)
        {
            if (prefab == null) return default;

            int prefabId = prefab.GetInstanceID();

            if (_physicsPools.TryGetValue(prefabId, out var pool))
            {
                return new PoolStats
                {
                    Prefab = prefab,
                    PooledCount = pool.PooledCount,
                    ActiveCount = pool.ActiveCount,
                    TotalCreated = pool.TotalCreated
                };
            }

            if (_raycastPools.TryGetValue(prefabId, out pool))
            {
                return new PoolStats
                {
                    Prefab = prefab,
                    PooledCount = pool.PooledCount,
                    ActiveCount = pool.ActiveCount,
                    TotalCreated = pool.TotalCreated
                };
            }

            return default;
        }

        /// <summary>
        /// Returns all active projectiles to their pools.
        /// </summary>
        public void ReturnAllActiveProjectiles()
        {
            foreach (var pool in _physicsPools.Values)
            {
                pool.ReturnAll();
            }

            foreach (var pool in _raycastPools.Values)
            {
                pool.ReturnAll();
            }

            _hitscanBeamPool?.ReturnAll();
        }

        #endregion

        #region Private Methods

        private PrefabPool CreatePhysicsPool(GameObject prefab, int initialSize = -1)
        {
            int size = initialSize >= 0 ? initialSize : config.defaultInitialSize;
            int prefabId = prefab.GetInstanceID();

            var pool = new PrefabPool(prefab, _poolContainer, size);
            _physicsPools[prefabId] = pool;
            _prefabLookup[prefabId] = prefab;

            OnPoolCreated?.Invoke(prefab, size);

            return pool;
        }

        private PrefabPool CreateRaycastPool(GameObject prefab, int initialSize = -1)
        {
            int size = initialSize >= 0 ? initialSize : config.defaultInitialSize;
            int prefabId = prefab.GetInstanceID();

            var pool = new PrefabPool(prefab, _poolContainer, size);
            _raycastPools[prefabId] = pool;
            _prefabLookup[prefabId] = prefab;

            OnPoolCreated?.Invoke(prefab, size);

            return pool;
        }

        private void CreateHitscanBeamPool()
        {
            // Create a simple beam prefab with LineRenderer
            var beamPrefab = new GameObject("HitscanBeam_Prefab");
            beamPrefab.SetActive(false);
            beamPrefab.hideFlags = HideFlags.HideAndDontSave;

            var lineRenderer = beamPrefab.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.allowOcclusionWhenDynamic = false;

            // Add poolable component for hitscan beams
            beamPrefab.AddComponent<HitscanBeamPoolable>();

            _hitscanBeamPool = new PrefabPool(beamPrefab, _poolContainer, config.hitscanBeamPoolSize);
        }

        private void HandleEntityRegistered(IEntityController entity)
        {
            // Try to find weapon modules and register their projectile configs
            // This enables pre-warming pools when entities spawn
            if (entity?.Entity == null) return;

            // Note: This is a best-effort registration. The spawner will create pools
            // on-demand if they don't exist, so missing registration isn't critical.

            // For now, we rely on lazy pool creation at spawn time.
            // Full entity-based registration would require interface changes to expose
            // projectile prefabs from weapon modules.
        }

        private void HandleEntityUnregistered(IEntityController entity)
        {
            // Could track entity counts per prefab here for pool shrinking
            // For now, pools persist until scene change
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            // Return all active projectiles
            ReturnAllActiveProjectiles();

            // Optionally shrink pools
            if (config.shrinkOnSceneUnload)
            {
                ShrinkAllPools();
            }
        }

        private void ShrinkAllPools()
        {
            foreach (var pool in _physicsPools.Values)
            {
                int targetSize = Mathf.CeilToInt(pool.TotalCreated * config.shrinkThreshold);
                pool.ShrinkPool(Mathf.Max(targetSize, config.defaultInitialSize));
            }

            foreach (var pool in _raycastPools.Values)
            {
                int targetSize = Mathf.CeilToInt(pool.TotalCreated * config.shrinkThreshold);
                pool.ShrinkPool(Mathf.Max(targetSize, config.defaultInitialSize));
            }
        }

        private void DisposeAllPools()
        {
            foreach (var pool in _physicsPools.Values)
            {
                pool.Dispose();
            }
            _physicsPools.Clear();

            foreach (var pool in _raycastPools.Values)
            {
                pool.Dispose();
            }
            _raycastPools.Clear();

            _hitscanBeamPool?.Dispose();
            _hitscanBeamPool = null;

            _prefabLookup.Clear();
            _prefabEntityCount.Clear();
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 500));
            GUILayout.Label("=== Projectile Pool Stats ===");

            GUILayout.Label($"Physics Pools: {_physicsPools.Count}");
            foreach (var kvp in _physicsPools)
            {
                if (_prefabLookup.TryGetValue(kvp.Key, out var prefab))
                {
                    var pool = kvp.Value;
                    GUILayout.Label($"  {prefab.name}: {pool.ActiveCount}/{pool.TotalCreated} active");
                }
            }

            GUILayout.Label($"Raycast Pools: {_raycastPools.Count}");
            foreach (var kvp in _raycastPools)
            {
                if (_prefabLookup.TryGetValue(kvp.Key, out var prefab))
                {
                    var pool = kvp.Value;
                    GUILayout.Label($"  {prefab.name}: {pool.ActiveCount}/{pool.TotalCreated} active");
                }
            }

            if (_hitscanBeamPool != null)
            {
                GUILayout.Label($"Hitscan Beams: {_hitscanBeamPool.ActiveCount}/{_hitscanBeamPool.TotalCreated} active");
            }

            GUILayout.EndArea();
        }
#endif

        #endregion
    }

    /// <summary>
    /// Statistics for a projectile pool.
    /// </summary>
    public struct PoolStats
    {
        public GameObject Prefab;
        public int PooledCount;
        public int ActiveCount;
        public int TotalCreated;
    }

    /// <summary>
    /// Simple poolable component for hitscan beam visuals.
    /// </summary>
    public class HitscanBeamPoolable : MonoBehaviour, IPoolable
    {
        private LineRenderer _lineRenderer;
        private float _duration;
        private float _elapsed;
        private Color _startColor;
        private bool _isActive;

        public bool IsActive => _isActive;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        public bool OnPoolGet()
        {
            _isActive = true;
            _elapsed = 0f;

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = true;
            }

            return _lineRenderer != null;
        }

        public void OnPoolReturn()
        {
            _isActive = false;

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Initializes the beam visual.
        /// </summary>
        public void Initialize(Vector2 start, Vector2 end, float duration, float width, Color color, Material material = null)
        {
            if (_lineRenderer == null) return;

            _duration = duration;
            _startColor = color;
            _elapsed = 0f;

            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, end);
            _lineRenderer.startWidth = width;
            _lineRenderer.endWidth = width * 0.5f;
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;

            if (material != null)
            {
                _lineRenderer.material = material;
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsed += Time.deltaTime;

            if (_elapsed >= _duration)
            {
                ProjectilePoolManager.Instance?.ReturnHitscanBeam(gameObject);
                return;
            }

            // Fade out
            float alpha = 1f - (_elapsed / _duration);
            Color fadedColor = _startColor;
            fadedColor.a *= alpha;

            if (_lineRenderer != null)
            {
                _lineRenderer.startColor = fadedColor;
                _lineRenderer.endColor = fadedColor;
            }
        }
    }
}

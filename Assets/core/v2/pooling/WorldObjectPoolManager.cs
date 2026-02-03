using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2.Pooling
{
    /// <summary>
    /// Manages PrefabPools for world objects (asteroids, celestial bodies).
    /// Lazily creates a pool per unique prefab on first request.
    /// </summary>
    public class WorldObjectPoolManager : MonoBehaviour
    {
        public static WorldObjectPoolManager Instance { get; private set; }

        [SerializeField] private int defaultInitialSize = 10;

        private Transform _poolContainer;
        private readonly Dictionary<int, PrefabPool> _pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _poolContainer = new GameObject("[WorldObjectPool]").transform;
            _poolContainer.SetParent(transform);
        }

        public GameObject Get(GameObject prefab)
        {
            var pool = GetOrCreatePool(prefab);
            return pool.Get();
        }

        public void Return(GameObject instance, GameObject prefab)
        {
            if (instance == null || prefab == null) return;

            int key = prefab.GetInstanceID();
            if (_pools.TryGetValue(key, out var pool))
            {
                pool.Return(instance);
            }
            else
            {
                Object.Destroy(instance);
            }
        }

        public void Prewarm(GameObject prefab, int count)
        {
            var pool = GetOrCreatePool(prefab);
            pool.ExpandPool(count);
        }

        private PrefabPool GetOrCreatePool(GameObject prefab)
        {
            int key = prefab.GetInstanceID();
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = new PrefabPool(prefab, _poolContainer, defaultInitialSize);
                _pools[key] = pool;
            }
            return pool;
        }

        private void OnDestroy()
        {
            foreach (var pool in _pools.Values)
                pool.Dispose();
            _pools.Clear();

            if (Instance == this)
                Instance = null;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2.Pooling
{
    /// <summary>
    /// Generic object pool for prefab-based GameObjects.
    /// Uses a Stack for efficient Get/Return operations.
    /// </summary>
    public class PrefabPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _poolContainer;
        private readonly Stack<GameObject> _available = new();
        private readonly HashSet<GameObject> _active = new();
        private readonly List<GameObject> _allInstances = new();

        /// <summary>
        /// Number of objects currently available in the pool.
        /// </summary>
        public int PooledCount => _available.Count;

        /// <summary>
        /// Number of objects currently in use.
        /// </summary>
        public int ActiveCount => _active.Count;

        /// <summary>
        /// Total number of objects created by this pool.
        /// </summary>
        public int TotalCreated => _allInstances.Count;

        /// <summary>
        /// The prefab this pool manages.
        /// </summary>
        public GameObject Prefab => _prefab;

        /// <summary>
        /// Creates a new prefab pool.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="container">Parent transform for pooled objects.</param>
        /// <param name="initialSize">Number of objects to pre-warm.</param>
        public PrefabPool(GameObject prefab, Transform container, int initialSize = 0)
        {
            _prefab = prefab;
            _poolContainer = container;

            // Pre-warm pool
            for (int i = 0; i < initialSize; i++)
            {
                var instance = CreateInstance();
                instance.SetActive(false);
                _available.Push(instance);
            }
        }

        /// <summary>
        /// Gets an object from the pool, or creates a new one if empty.
        /// </summary>
        /// <returns>An active GameObject ready for use.</returns>
        public GameObject Get()
        {
            GameObject instance;

            if (_available.Count > 0)
            {
                instance = _available.Pop();

                // Validate instance is still valid
                if (instance == null)
                {
                    // Instance was destroyed externally, create new one
                    instance = CreateInstance();
                }
            }
            else
            {
                instance = CreateInstance();
            }

            _active.Add(instance);

            // Call IPoolable.OnPoolGet if component exists
            var poolable = instance.GetComponent<IPoolable>();
            if (poolable != null)
            {
                if (!poolable.OnPoolGet())
                {
                    // Object reported itself as invalid, try again
                    _active.Remove(instance);
                    Object.Destroy(instance);
                    _allInstances.Remove(instance);
                    return Get();
                }
            }

            instance.SetActive(true);
            return instance;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="instance">The object to return.</param>
        public void Return(GameObject instance)
        {
            if (instance == null) return;
            if (!_active.Contains(instance)) return;

            _active.Remove(instance);

            // Call IPoolable.OnPoolReturn if component exists
            var poolable = instance.GetComponent<IPoolable>();
            poolable?.OnPoolReturn();

            instance.SetActive(false);

            // Re-parent to pool container
            if (_poolContainer != null)
            {
                instance.transform.SetParent(_poolContainer);
            }

            _available.Push(instance);
        }

        /// <summary>
        /// Returns all active objects to the pool.
        /// </summary>
        public void ReturnAll()
        {
            // Copy to list to avoid modification during iteration
            var activeList = new List<GameObject>(_active);

            foreach (var instance in activeList)
            {
                if (instance != null)
                {
                    Return(instance);
                }
            }

            _active.Clear();
        }

        /// <summary>
        /// Expands the pool by creating additional instances.
        /// </summary>
        /// <param name="count">Number of instances to add.</param>
        public void ExpandPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var instance = CreateInstance();
                instance.SetActive(false);
                _available.Push(instance);
            }
        }

        /// <summary>
        /// Shrinks the pool by destroying excess instances.
        /// Only destroys objects currently in the pool (not active ones).
        /// </summary>
        /// <param name="targetSize">Target pool size.</param>
        public void ShrinkPool(int targetSize)
        {
            while (_available.Count > targetSize)
            {
                var instance = _available.Pop();
                if (instance != null)
                {
                    _allInstances.Remove(instance);
                    Object.Destroy(instance);
                }
            }
        }

        /// <summary>
        /// Destroys all pooled objects and clears the pool.
        /// </summary>
        public void Dispose()
        {
            // Return all active first
            ReturnAll();

            // Destroy all instances
            foreach (var instance in _allInstances)
            {
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }

            _allInstances.Clear();
            _available.Clear();
            _active.Clear();
        }

        private GameObject CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _poolContainer);
            instance.name = $"{_prefab.name}_Pooled_{_allInstances.Count}";
            _allInstances.Add(instance);
            return instance;
        }
    }
}

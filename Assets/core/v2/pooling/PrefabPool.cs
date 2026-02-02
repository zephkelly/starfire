using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace StarfireV2.Pooling
{
    /// <summary>
    /// Prefab-based GameObject pool backed by Unity's ObjectPool.
    /// Calls IPoolable lifecycle hooks on components when getting/returning.
    /// </summary>
    public class PrefabPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _poolContainer;
        private readonly ObjectPool<GameObject> _pool;
        private readonly HashSet<GameObject> _active = new();
        private int _totalCreated;

        public int PooledCount => _pool.CountInactive;
        public int ActiveCount => _active.Count;
        public int TotalCreated => _totalCreated;
        public GameObject Prefab => _prefab;

        public PrefabPool(GameObject prefab, Transform container, int initialSize = 0)
        {
            _prefab = prefab;
            _poolContainer = container;

            _pool = new ObjectPool<GameObject>(
                createFunc: CreateInstance,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyInstance,
                collectionCheck: true,
                defaultCapacity: Mathf.Max(initialSize, 10),
                maxSize: 10000
            );

            // Pre-warm
            if (initialSize > 0)
            {
                var warmup = new List<GameObject>(initialSize);
                for (int i = 0; i < initialSize; i++)
                {
                    warmup.Add(_pool.Get());
                }
                foreach (var go in warmup)
                {
                    _pool.Release(go);
                }
            }
        }

        public GameObject Get()
        {
            return _pool.Get();
        }

        public void Return(GameObject instance)
        {
            if (instance == null) return;
            if (!_active.Contains(instance)) return;
            _pool.Release(instance);
        }

        public void ReturnAll()
        {
            var activeList = new List<GameObject>(_active);
            foreach (var instance in activeList)
            {
                if (instance != null)
                {
                    _pool.Release(instance);
                }
            }
            _active.Clear();
        }

        public void ExpandPool(int count)
        {
            var warmup = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                warmup.Add(_pool.Get());
            }
            foreach (var go in warmup)
            {
                _pool.Release(go);
            }
        }

        public void ShrinkPool(int targetSize)
        {
            // ObjectPool handles max size internally.
            // Clear and re-warm to the target size if we have excess.
            if (_pool.CountInactive > targetSize)
            {
                _pool.Clear();
                ExpandPool(targetSize);
            }
        }

        public void Dispose()
        {
            ReturnAll();
            _pool.Clear();
            _active.Clear();
        }

        private GameObject CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _poolContainer);
            instance.name = $"{_prefab.name}_Pooled_{_totalCreated}";
            _totalCreated++;
            return instance;
        }

        private void OnGet(GameObject instance)
        {
            _active.Add(instance);

            var poolable = instance.GetComponent<IPoolable>();
            if (poolable != null)
            {
                if (!poolable.OnPoolGet())
                {
                    // Invalid — release back immediately, pool will provide another
                    _active.Remove(instance);
                    Object.Destroy(instance);
                    return;
                }
            }

            instance.SetActive(true);
        }

        private void OnRelease(GameObject instance)
        {
            _active.Remove(instance);

            var poolable = instance.GetComponent<IPoolable>();
            poolable?.OnPoolReturn();

            instance.SetActive(false);

            if (_poolContainer != null)
            {
                instance.transform.SetParent(_poolContainer);
            }
        }

        private void OnDestroyInstance(GameObject instance)
        {
            if (instance != null)
            {
                Object.Destroy(instance);
            }
        }
    }
}

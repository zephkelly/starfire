using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Object pool for MinimapBlip instances.
    /// Pre-warms a pool and reuses blips to avoid runtime allocation.
    /// </summary>
    public class MinimapBlipPool
    {
        private readonly RectTransform _parent;
        private readonly Stack<MinimapBlip> _pool = new();
        private readonly List<MinimapBlip> _allBlips = new();
        private readonly GameObject _blipPrefab;

        public int PoolCount => _pool.Count;
        public int TotalCreated => _allBlips.Count;

        public MinimapBlipPool(RectTransform parent, int initialSize = 20)
        {
            _parent = parent;
            _blipPrefab = CreateBlipPrefab();

            // Pre-warm pool
            for (int i = 0; i < initialSize; i++)
            {
                var blip = CreateNewBlip();
                blip.gameObject.SetActive(false);
                _pool.Push(blip);
            }
        }

        /// <summary>
        /// Get a blip from the pool (or create a new one if empty).
        /// </summary>
        public MinimapBlip Get()
        {
            MinimapBlip blip;

            if (_pool.Count > 0)
            {
                blip = _pool.Pop();
            }
            else
            {
                blip = CreateNewBlip();
            }

            blip.gameObject.SetActive(true);
            return blip;
        }

        /// <summary>
        /// Return a blip to the pool.
        /// </summary>
        public void Return(MinimapBlip blip)
        {
            if (blip == null) return;

            blip.Reset();
            blip.gameObject.SetActive(false);
            _pool.Push(blip);
        }

        /// <summary>
        /// Return all active blips to the pool.
        /// </summary>
        public void ReturnAll()
        {
            foreach (var blip in _allBlips)
            {
                if (blip != null && blip.IsActive)
                {
                    blip.Reset();
                    blip.gameObject.SetActive(false);
                    if (!_pool.Contains(blip))
                    {
                        _pool.Push(blip);
                    }
                }
            }
        }

        /// <summary>
        /// Destroy all pooled objects.
        /// </summary>
        public void Dispose()
        {
            foreach (var blip in _allBlips)
            {
                if (blip != null)
                {
                    Object.Destroy(blip.gameObject);
                }
            }

            _allBlips.Clear();
            _pool.Clear();

            if (_blipPrefab != null)
            {
                Object.Destroy(_blipPrefab);
            }
        }

        private MinimapBlip CreateNewBlip()
        {
            var obj = Object.Instantiate(_blipPrefab, _parent);
            obj.name = $"MinimapBlip_{_allBlips.Count}";

            var blip = obj.GetComponent<MinimapBlip>();
            _allBlips.Add(blip);

            return blip;
        }

        private GameObject CreateBlipPrefab()
        {
            var obj = new GameObject("MinimapBlip_Prefab");

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(8f, 8f);

            var image = obj.AddComponent<Image>();
            image.raycastTarget = false;

            obj.AddComponent<MinimapBlip>();

            obj.SetActive(false);
            obj.hideFlags = HideFlags.HideAndDontSave;

            return obj;
        }
    }
}

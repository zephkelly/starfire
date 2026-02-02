using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;

namespace StarfireV2
{
    public class ShieldImpactEffect : MonoBehaviour
    {
        private static ObjectPool<ShieldImpactEffect> _pool;
        private static Sprite _cachedCircleSprite;

        private ShieldImpactConfig _config;
        private float _startTime;
        private float _initialIntensity;
        private float _initialRippleAlpha;

        private Rigidbody2D _rb;
        private Light2D _light;
        private GameObject _lightGO;
        private SpriteRenderer _rippleRenderer;
        private GameObject _rippleGO;
        private GameObject _particleInstance;
        private GameObject _lastParticlePrefab;

        private static ObjectPool<ShieldImpactEffect> Pool
        {
            get
            {
                _pool ??= new ObjectPool<ShieldImpactEffect>(
                    createFunc: () =>
                    {
                        var go = new GameObject("ShieldImpactEffect");
                        return go.AddComponent<ShieldImpactEffect>();
                    },
                    actionOnGet: effect =>
                    {
                        effect.gameObject.SetActive(true);
                    },
                    actionOnRelease: effect =>
                    {
                        effect._config = null;
                        if (effect._lightGO != null) effect._lightGO.SetActive(false);
                        if (effect._rippleGO != null) effect._rippleGO.SetActive(false);
                        if (effect._particleInstance != null) effect._particleInstance.SetActive(false);
                        if (effect._rb != null) effect._rb.simulated = false;
                        effect.gameObject.SetActive(false);
                    },
                    actionOnDestroy: effect =>
                    {
                        if (effect != null) Destroy(effect.gameObject);
                    },
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: 50
                );
                return _pool;
            }
        }

        public static ShieldImpactEffect Spawn(ShieldImpactConfig config, Vector2 position, Vector2 normal, Vector2 inheritedVelocity = default)
        {
            if (config == null) return null;

            var effect = Pool.Get();
            effect.Activate(config, position, normal, inheritedVelocity);
            return effect;
        }

        private static Sprite GetCircleSprite()
        {
            if (_cachedCircleSprite != null) return _cachedCircleSprite;

            int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color transparent = new Color(1f, 1f, 1f, 0f);
            float center = size / 2f;
            float radius = size / 2f - 2f;
            float edgeWidth = 4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                    if (dist > radius)
                    {
                        texture.SetPixel(x, y, transparent);
                    }
                    else if (dist > radius - edgeWidth)
                    {
                        float edgeFactor = 1f - ((radius - dist) / edgeWidth);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, edgeFactor));
                    }
                    else
                    {
                        float innerFactor = dist / (radius - edgeWidth);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, innerFactor * 0.2f));
                    }
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;

            _cachedCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _cachedCircleSprite;
        }

        private void Activate(ShieldImpactConfig config, Vector2 position, Vector2 normal, Vector2 inheritedVelocity)
        {
            _config = config;
            _startTime = Time.time;

            transform.SetParent(null);
            transform.position = position;

            if (normal != Vector2.zero)
            {
                float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                transform.rotation = Quaternion.identity;
            }

            // Rigidbody for inherited velocity
            if (inheritedVelocity.sqrMagnitude > 0.01f)
            {
                if (_rb == null)
                {
                    _rb = gameObject.AddComponent<Rigidbody2D>();
                    _rb.bodyType = RigidbodyType2D.Kinematic;
                }
                _rb.simulated = true;
                _rb.linearVelocity = inheritedVelocity;
            }
            else if (_rb != null)
            {
                _rb.simulated = false;
                _rb.linearVelocity = Vector2.zero;
            }

            // Particles
            if (config.particlePrefab != null)
            {
                if (_particleInstance != null && _lastParticlePrefab == config.particlePrefab)
                {
                    _particleInstance.SetActive(true);
                    _particleInstance.transform.localPosition = Vector3.zero;
                    _particleInstance.transform.localRotation = Quaternion.identity;
                    var ps = _particleInstance.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Clear();
                        ps.Play();
                    }
                }
                else
                {
                    if (_particleInstance != null) Destroy(_particleInstance);
                    _particleInstance = Instantiate(config.particlePrefab, transform.position, transform.rotation, transform);
                    _particleInstance.transform.localScale = Vector3.one * config.particleScale;
                    _lastParticlePrefab = config.particlePrefab;
                }
            }
            else if (_particleInstance != null)
            {
                _particleInstance.SetActive(false);
            }

            // Ripple
            if (config.enableRipple)
            {
                if (_rippleGO == null)
                {
                    _rippleGO = new GameObject("ShieldRipple");
                    _rippleGO.transform.SetParent(transform);
                    _rippleRenderer = _rippleGO.AddComponent<SpriteRenderer>();
                    _rippleRenderer.sprite = GetCircleSprite();
                    _rippleRenderer.sortingOrder = 100;
                }

                _rippleGO.SetActive(true);
                _rippleGO.transform.localPosition = Vector3.zero;
                _rippleGO.transform.localScale = Vector3.one * 0.1f * config.rippleSize;
                _rippleRenderer.color = config.rippleColor;
                _initialRippleAlpha = config.rippleColor.a;
            }
            else if (_rippleGO != null)
            {
                _rippleGO.SetActive(false);
            }

            // Light
            if (config.enableLight)
            {
                if (_lightGO == null)
                {
                    _lightGO = new GameObject("ShieldImpactLight");
                    _lightGO.transform.SetParent(transform);
                    _light = _lightGO.AddComponent<Light2D>();
                    _light.lightType = Light2D.LightType.Point;
                }

                _lightGO.SetActive(true);
                _lightGO.transform.localPosition = Vector3.zero;
                _light.color = config.lightColor;
                _light.intensity = config.lightIntensity;
                _light.pointLightOuterRadius = config.lightRadius;
                _light.pointLightInnerRadius = config.lightRadius * 0.1f;
                _initialIntensity = config.lightIntensity;
            }
            else if (_lightGO != null)
            {
                _lightGO.SetActive(false);
            }

            // Audio
            if (config.impactSound != null)
            {
                AudioSource.PlayClipAtPoint(config.impactSound, position, config.soundVolume);
            }
        }

        private void Update()
        {
            if (_config == null) return;

            float elapsed = Time.time - _startTime;

            if (_light != null && _lightGO != null && _lightGO.activeSelf)
            {
                float lightProgress = Mathf.Clamp01(elapsed / _config.lightDuration);
                _light.intensity = _initialIntensity * (1f - lightProgress);
            }

            if (_rippleRenderer != null && _rippleGO != null && _rippleGO.activeSelf)
            {
                float rippleProgress = Mathf.Clamp01(elapsed / _config.rippleDuration);

                float scale = Mathf.Lerp(0.1f, 1f, rippleProgress) * _config.rippleSize;
                _rippleGO.transform.localScale = Vector3.one * scale;

                Color color = _config.rippleColor;
                color.a = _initialRippleAlpha * (1f - rippleProgress);
                _rippleRenderer.color = color;
            }

            if (elapsed >= _config.duration)
            {
                Pool.Release(this);
            }
        }
    }
}

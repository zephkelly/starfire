using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace StarfireV2
{
    public class ShieldImpactEffect : MonoBehaviour
    {
        private ShieldImpactConfig _config;
        private Light2D _light;
        private SpriteRenderer _rippleRenderer;
        private float _startTime;
        private float _initialIntensity;
        private float _initialRippleAlpha;

        public static ShieldImpactEffect Spawn(ShieldImpactConfig config, Vector2 position, Vector2 normal, Vector2 inheritedVelocity = default)
        {
            if (config == null) return null;

            var go = new GameObject("ShieldImpactEffect");
            go.transform.position = position;

            if (normal != Vector2.zero)
            {
                float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
                go.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            if (inheritedVelocity.sqrMagnitude > 0.01f)
            {
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = inheritedVelocity;
            }

            var effect = go.AddComponent<ShieldImpactEffect>();
            effect.Initialize(config);

            return effect;
        }

        private void Initialize(ShieldImpactConfig config)
        {
            _config = config;
            _startTime = Time.time;

            if (_config.particlePrefab != null)
            {
                var particles = Instantiate(_config.particlePrefab, transform.position, transform.rotation, transform);
                particles.transform.localScale = Vector3.one * _config.particleScale;
            }

            if (_config.enableRipple)
            {
                CreateRippleEffect();
            }

            if (_config.enableLight)
            {
                var lightGO = new GameObject("ShieldImpactLight");
                lightGO.transform.SetParent(transform);
                lightGO.transform.localPosition = Vector3.zero;

                _light = lightGO.AddComponent<Light2D>();
                _light.lightType = Light2D.LightType.Point;
                _light.color = _config.lightColor;
                _light.intensity = _config.lightIntensity;
                _light.pointLightOuterRadius = _config.lightRadius;
                _light.pointLightInnerRadius = _config.lightRadius * 0.1f;

                _initialIntensity = _config.lightIntensity;
            }

            if (_config.impactSound != null)
            {
                AudioSource.PlayClipAtPoint(_config.impactSound, transform.position, _config.soundVolume);
            }
        }

        private void CreateRippleEffect()
        {
            var rippleGO = new GameObject("ShieldRipple");
            rippleGO.transform.SetParent(transform);
            rippleGO.transform.localPosition = Vector3.zero;

            _rippleRenderer = rippleGO.AddComponent<SpriteRenderer>();
            _rippleRenderer.sprite = CreateCircleSprite();
            _rippleRenderer.color = _config.rippleColor;
            _rippleRenderer.sortingOrder = 100;

            rippleGO.transform.localScale = Vector3.one * 0.1f * _config.rippleSize;

            _initialRippleAlpha = _config.rippleColor.a;
        }

        private Sprite CreateCircleSprite()
        {
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

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void Update()
        {
            float elapsed = Time.time - _startTime;
            float progress = Mathf.Clamp01(elapsed / _config.duration);

            if (_light != null)
            {
                float lightProgress = Mathf.Clamp01(elapsed / _config.lightDuration);
                _light.intensity = _initialIntensity * (1f - lightProgress);
            }

            if (_rippleRenderer != null)
            {
                float rippleProgress = Mathf.Clamp01(elapsed / _config.rippleDuration);

                float scale = Mathf.Lerp(0.1f, 1f, rippleProgress) * _config.rippleSize;
                _rippleRenderer.transform.localScale = Vector3.one * scale;

                Color color = _config.rippleColor;
                color.a = _initialRippleAlpha * (1f - rippleProgress);
                _rippleRenderer.color = color;
            }

            if (elapsed >= _config.duration)
            {
                Destroy(gameObject);
            }
        }
    }
}

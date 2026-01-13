using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Starfire.Entity.Modules.Shield
{
    /// <summary>
    /// Handles the visual and audio effects when a projectile impacts a shield boundary.
    /// Self-destructs after the configured duration.
    /// </summary>
    public class ShieldImpactEffect : MonoBehaviour
    {
        private ShieldImpactConfig _config;
        private Light2D _light;
        private SpriteRenderer _rippleRenderer;
        private float _startTime;
        private float _initialIntensity;
        private float _initialRippleAlpha;

        /// <summary>
        /// Factory method to spawn a shield impact effect at a position.
        /// </summary>
        /// <param name="config">Impact effect configuration</param>
        /// <param name="position">World position of impact</param>
        /// <param name="normal">Normal direction of impact (pointing away from shield)</param>
        /// <param name="inheritedVelocity">Velocity to inherit from the ship (so effect moves with ship)</param>
        public static ShieldImpactEffect Spawn(ShieldImpactConfig config, Vector2 position, Vector2 normal, Vector2 inheritedVelocity = default)
        {
            if (config == null) return null;

            var go = new GameObject("ShieldImpactEffect");
            go.transform.position = position;

            // Rotate to face the impact normal
            if (normal != Vector2.zero)
            {
                float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
                go.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            // Add Rigidbody2D to move the effect with the ship's velocity
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

            // Spawn particle effect
            if (_config.particlePrefab != null)
            {
                var particles = Instantiate(_config.particlePrefab, transform.position, transform.rotation, transform);
                particles.transform.localScale = Vector3.one * _config.particleScale;
            }

            // Create shield ripple visual
            if (_config.enableRipple)
            {
                CreateRippleEffect();
            }

            // Create light
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

            // Play sound
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

            // Create a simple circle sprite programmatically or use Unity's default
            _rippleRenderer.sprite = CreateCircleSprite();
            _rippleRenderer.color = _config.rippleColor;
            _rippleRenderer.sortingOrder = 100; // Render on top

            // Start small
            rippleGO.transform.localScale = Vector3.one * 0.1f * _config.rippleSize;

            _initialRippleAlpha = _config.rippleColor.a;
        }

        private Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
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
                        // Edge ring
                        float edgeFactor = 1f - ((radius - dist) / edgeWidth);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, edgeFactor));
                    }
                    else
                    {
                        // Inner area (mostly transparent with slight glow)
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
            float curveValue = _config.fadeCurve.Evaluate(progress);

            // Update light fade
            if (_light != null)
            {
                float lightProgress = Mathf.Clamp01(elapsed / _config.lightDuration);
                _light.intensity = _initialIntensity * (1f - lightProgress);
            }

            // Update ripple expansion and fade
            if (_rippleRenderer != null)
            {
                float rippleProgress = Mathf.Clamp01(elapsed / _config.rippleDuration);

                // Expand
                float scale = Mathf.Lerp(0.1f, 1f, rippleProgress) * _config.rippleSize;
                _rippleRenderer.transform.localScale = Vector3.one * scale;

                // Fade
                Color color = _config.rippleColor;
                color.a = _initialRippleAlpha * (1f - rippleProgress);
                _rippleRenderer.color = color;
            }

            // Self-destruct after duration
            if (elapsed >= _config.duration)
            {
                Destroy(gameObject);
            }
        }
    }
}

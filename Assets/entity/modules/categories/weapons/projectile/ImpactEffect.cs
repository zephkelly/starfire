using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Handles the visual and audio effects when a projectile impacts a target.
    /// Self-destructs after the configured duration.
    /// </summary>
    public class ImpactEffect : MonoBehaviour
    {
        private ImpactConfig _config;
        private Light2D _light;
        private float _startTime;
        private float _initialIntensity;

        /// <summary>
        /// Factory method to spawn an impact effect at a position.
        /// </summary>
        public static ImpactEffect Spawn(ImpactConfig config, Vector2 position, Vector2 direction)
        {
            if (config == null) return null;

            var go = new GameObject("ImpactEffect");
            go.transform.position = position;

            // Rotate to face the impact direction (opposite of projectile direction)
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                go.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            var effect = go.AddComponent<ImpactEffect>();
            effect.Initialize(config);

            return effect;
        }

        private void Initialize(ImpactConfig config)
        {
            _config = config;
            _startTime = Time.time;

            // Spawn particle effect
            if (_config.particlePrefab != null)
            {
                var particles = Instantiate(_config.particlePrefab, transform.position, transform.rotation, transform);
                particles.transform.localScale = Vector3.one * _config.particleScale;
            }

            // Create light
            if (_config.enableLight)
            {
                var lightGO = new GameObject("ImpactLight");
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

        private void Update()
        {
            float elapsed = Time.time - _startTime;

            // Update light fade
            if (_light != null && elapsed >= _config.fadeStartTime)
            {
                float fadeProgress = (elapsed - _config.fadeStartTime) / (_config.duration - _config.fadeStartTime);
                fadeProgress = Mathf.Clamp01(fadeProgress);

                float curveValue = _config.fadeCurve.Evaluate(fadeProgress);
                _light.intensity = _initialIntensity * (1f - curveValue);
            }

            // Self-destruct after duration
            if (elapsed >= _config.duration)
            {
                Destroy(gameObject);
            }
        }
    }
}

using UnityEngine;

namespace Starfire.Core.V3.Cam.Effects
{
    /// <summary>
    /// Perlin noise-based screen shake effect using trauma accumulation.
    /// Trauma decays over time and shake intensity is trauma squared for a natural feel.
    /// </summary>
    public class V3ScreenShakeEffect
    {
        private V3ScreenShakeConfig _config;
        private float _trauma;
        private readonly float _seed;

        /// <summary>
        /// Current trauma level (0-1).
        /// </summary>
        public float Trauma => _trauma;

        /// <summary>
        /// Whether the effect is currently active.
        /// </summary>
        public bool IsActive => _trauma > 0.001f;

        public V3ScreenShakeEffect()
        {
            _seed = Random.value * 1000f;
        }

        /// <summary>
        /// Sets the configuration for shake behavior.
        /// </summary>
        public void SetConfig(V3ScreenShakeConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Adds trauma to the shake effect. Trauma is clamped to 0-1.
        /// </summary>
        /// <param name="amount">Amount of trauma to add (will be multiplied by config's shakeMultiplier).</param>
        public void AddTrauma(float amount)
        {
            float multiplier = _config?.shakeMultiplier ?? 1f;
            _trauma = Mathf.Clamp01(_trauma + amount * multiplier);
        }

        /// <summary>
        /// Updates trauma decay.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_config == null) return;

            _trauma = Mathf.Max(0f, _trauma - _config.traumaDecay * deltaTime);
        }

        /// <summary>
        /// Applies shake offset to position and rotation.
        /// </summary>
        public void Apply(ref Vector3 position, ref float rotation)
        {
            if (!IsActive || _config == null) return;

            // Square trauma for a more natural feel (small trauma = barely noticeable, high trauma = intense)
            float shake = _trauma * _trauma;
            float time = Time.time * _config.frequency;

            // Use Perlin noise for smooth random movement
            float offsetX = (Mathf.PerlinNoise(_seed, time) - 0.5f) * 2f * _config.maxOffset * shake;
            float offsetY = (Mathf.PerlinNoise(_seed + 100f, time) - 0.5f) * 2f * _config.maxOffset * shake;
            float rotOffset = (Mathf.PerlinNoise(_seed + 200f, time) - 0.5f) * 2f * _config.maxRotation * shake;

            position.x += offsetX;
            position.y += offsetY;
            rotation += rotOffset;
        }

        /// <summary>
        /// Resets trauma to zero.
        /// </summary>
        public void Reset()
        {
            _trauma = 0f;
        }
    }
}

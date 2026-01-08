using Starfire.Core.Cam.Config;
using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    public class ScreenShakeEffect : ICameraEffect
    {
        public string EffectId => "screen_shake";
        public bool IsActive => _trauma > 0.001f;

        private CameraController _controller;
        private CameraPresetInstance _preset;

        private float _trauma;
        private float _seed;

        public float Trauma => _trauma;

        public ScreenShakeEffect()
        {
            _seed = Random.value * 1000f;
        }

        public void Initialize(CameraController controller)
        {
            _controller = controller;
        }

        public void SetPreset(CameraPresetInstance preset)
        {
            _preset = preset;
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount * (_preset?.ShakeMultiplier ?? 1f));
        }

        public void Update(float deltaTime)
        {
            if (_preset == null) return;
            _trauma = Mathf.Max(0, _trauma - _preset.ShakeTraumaDecay * deltaTime);
        }

        public void Apply(ref Vector3 position, ref float rotation, ref float zoom)
        {
            if (!IsActive || _preset == null) return;

            float shake = _trauma * _trauma;
            float time = Time.time * _preset.ShakeFrequency;

            float offsetX = (Mathf.PerlinNoise(_seed, time) - 0.5f) * 2f * _preset.MaxShakeOffset * shake;
            float offsetY = (Mathf.PerlinNoise(_seed + 100, time) - 0.5f) * 2f * _preset.MaxShakeOffset * shake;
            float rotOffset = (Mathf.PerlinNoise(_seed + 200, time) - 0.5f) * 2f * _preset.MaxShakeRotation * shake;

            position.x += offsetX;
            position.y += offsetY;
            rotation += rotOffset;
        }

        public void Reset()
        {
            _trauma = 0f;
        }
    }
}

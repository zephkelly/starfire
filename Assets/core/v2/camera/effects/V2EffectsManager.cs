using Starfire.Core.Cam.Effects;
using Starfire.Core.V2.Cam.Config;
using UnityEngine;

namespace Starfire.Core.V2.Cam
{
    public class V2EffectsManager
    {
        private VelocityCameraPresetInstance _preset;
        private PostProcessingBridge _postProcessing;

        // Screen shake state
        private float _trauma;
        private float _shakeSeed;

        // Directional punch state
        private Vector2 _punchDirection;
        private float _punchForce;
        private float _punchDuration;
        private float _punchTime;
        private AnimationCurve _punchCurve;

        public float Trauma => _trauma;
        public bool IsShaking => _trauma > 0.001f;
        public bool IsPunching => _punchTime < _punchDuration;

        public V2EffectsManager(VelocityCameraPresetInstance preset, PostProcessingBridge postProcessing = null)
        {
            _preset = preset;
            _postProcessing = postProcessing;
            _shakeSeed = Random.value * 1000f;
            _punchTime = 1f;
            _punchDuration = 1f;

            _punchCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 4f),
                new Keyframe(0.15f, 1f, 0f, 0f),
                new Keyframe(1f, 0f, -2f, 0f)
            );
        }

        public void SetPreset(VelocityCameraPresetInstance preset)
        {
            _preset = preset;
        }

        public void SetPostProcessingBridge(PostProcessingBridge bridge)
        {
            _postProcessing = bridge;
        }

        public void Update(float deltaTime)
        {
            // Update shake
            if (_preset != null && _trauma > 0)
            {
                _trauma = Mathf.Max(0, _trauma - _preset.ShakeTraumaDecay * deltaTime);
            }

            // Update punch
            if (IsPunching)
            {
                _punchTime += deltaTime;
            }

            // Update post-processing
            _postProcessing?.Update(deltaTime);
        }

        public void ApplyAllEffects(ref Vector3 position, ref float rotation, ref float zoom)
        {
            ApplyShake(ref position, ref rotation);
            ApplyPunch(ref position);
        }

        private void ApplyShake(ref Vector3 position, ref float rotation)
        {
            if (!IsShaking || _preset == null) return;

            float shake = _trauma * _trauma;
            float time = Time.time * _preset.ShakeFrequency;

            float offsetX = (Mathf.PerlinNoise(_shakeSeed, time) - 0.5f) * 2f * _preset.MaxShakeOffset * shake;
            float offsetY = (Mathf.PerlinNoise(_shakeSeed + 100, time) - 0.5f) * 2f * _preset.MaxShakeOffset * shake;
            float rotOffset = (Mathf.PerlinNoise(_shakeSeed + 200, time) - 0.5f) * 2f * _preset.MaxShakeRotation * shake;

            position.x += offsetX;
            position.y += offsetY;
            rotation += rotOffset;
        }

        private void ApplyPunch(ref Vector3 position)
        {
            if (!IsPunching) return;

            float t = _punchTime / _punchDuration;
            float curveValue = _punchCurve.Evaluate(t);

            Vector2 offset = _punchDirection * _punchForce * curveValue;
            position.x += offset.x;
            position.y += offset.y;
        }

        public void AddTrauma(float amount)
        {
            float multiplier = _preset?.ShakeMultiplier ?? 1f;
            _trauma = Mathf.Clamp01(_trauma + amount * multiplier);
        }

        public void Punch(Vector2 direction, float force, float duration = 0.15f)
        {
            _punchDirection = direction.normalized;
            _punchForce = force;
            _punchDuration = Mathf.Max(0.01f, duration);
            _punchTime = 0f;
        }

        public void Flash(Color color, float duration)
        {
            _postProcessing?.Flash(color, duration);
        }

        public void SetChromaticAberration(float intensity, float duration = 0f)
        {
            _postProcessing?.SetChromaticAberration(intensity, duration);
        }

        public void SetVignette(float intensity, float duration = 0f)
        {
            _postProcessing?.SetVignette(intensity, duration);
        }

        public void ResetAll()
        {
            _trauma = 0f;
            _punchTime = _punchDuration;
            _postProcessing?.ResetAll();
        }
    }
}

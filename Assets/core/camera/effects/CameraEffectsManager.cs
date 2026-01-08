using System.Collections.Generic;
using Starfire.Core.Cam.Config;
using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    public class CameraEffectsManager
    {
        private readonly CameraController _controller;
        private readonly List<ICameraEffect> _effects = new();

        private readonly ScreenShakeEffect _shakeEffect;
        private readonly DirectionalPunchEffect _punchEffect;
        private PostProcessingBridge _postProcessing;

        public ScreenShakeEffect ShakeEffect => _shakeEffect;
        public DirectionalPunchEffect PunchEffect => _punchEffect;
        public PostProcessingBridge PostProcessing => _postProcessing;

        public CameraEffectsManager(CameraController controller)
        {
            _controller = controller;

            _shakeEffect = new ScreenShakeEffect();
            _punchEffect = new DirectionalPunchEffect();

            _effects.Add(_shakeEffect);
            _effects.Add(_punchEffect);

            foreach (var effect in _effects)
            {
                effect.Initialize(controller);
            }
        }

        public void SetPreset(CameraPresetInstance preset)
        {
            _shakeEffect.SetPreset(preset);
        }

        public void SetPostProcessingBridge(PostProcessingBridge bridge)
        {
            _postProcessing = bridge;
        }

        public void Update(float deltaTime)
        {
            foreach (var effect in _effects)
            {
                effect.Update(deltaTime);
            }
            _postProcessing?.Update(deltaTime);
        }

        public void ApplyAllEffects(ref Vector3 position, ref float rotation, ref float zoom)
        {
            foreach (var effect in _effects)
            {
                if (effect.IsActive)
                {
                    effect.Apply(ref position, ref rotation, ref zoom);
                }
            }
        }

        public void AddEffect(ICameraEffect effect)
        {
            if (!_effects.Contains(effect))
            {
                effect.Initialize(_controller);
                _effects.Add(effect);
            }
        }

        public void RemoveEffect(ICameraEffect effect)
        {
            _effects.Remove(effect);
        }

        public void ResetAll()
        {
            foreach (var effect in _effects)
            {
                effect.Reset();
            }
            _postProcessing?.ResetAll();
        }

        public void AddTrauma(float trauma) => _shakeEffect.AddTrauma(trauma);

        public void Punch(Vector2 direction, float force, float duration = 0.15f)
            => _punchEffect.Trigger(direction, force, duration);

        public void Flash(Color color, float duration)
            => _postProcessing?.Flash(color, duration);

        public void SetChromaticAberration(float intensity, float duration = 0f)
            => _postProcessing?.SetChromaticAberration(intensity, duration);

        public void SetVignette(float intensity, float duration = 0f)
            => _postProcessing?.SetVignette(intensity, duration);
    }
}

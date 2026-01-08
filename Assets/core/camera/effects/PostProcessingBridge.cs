using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Starfire.Core.Cam.Effects
{
    public class PostProcessingBridge
    {
        private readonly Volume _volume;
        private readonly VolumeProfile _profile;

        private ChromaticAberration _chromaticAberration;
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;

        private float _chromaticBaseValue;
        private float _chromaticTarget;
        private float _chromaticDuration;
        private float _chromaticElapsed;
        private float _chromaticStartValue;
        private bool _chromaticTransitioning;

        private float _vignetteBaseValue;
        private float _vignetteTarget;
        private float _vignetteDuration;
        private float _vignetteElapsed;
        private float _vignetteStartValue;
        private bool _vignetteTransitioning;

        private Color _flashColor;
        private float _flashDuration;
        private float _flashElapsed;
        private bool _flashing;

        public PostProcessingBridge(Volume volume)
        {
            _volume = volume;
            _profile = volume.profile;

            _profile.TryGet(out _chromaticAberration);
            _profile.TryGet(out _vignette);
            _profile.TryGet(out _colorAdjustments);

            if (_chromaticAberration != null)
            {
                _chromaticBaseValue = _chromaticAberration.intensity.value;
            }

            if (_vignette != null)
            {
                _vignetteBaseValue = _vignette.intensity.value;
            }
        }

        public void Update(float deltaTime)
        {
            UpdateChromaticAberration(deltaTime);
            UpdateVignette(deltaTime);
            UpdateFlash(deltaTime);
        }

        public void SetChromaticAberration(float intensity, float duration = 0f)
        {
            if (_chromaticAberration == null) return;

            if (duration <= 0f)
            {
                _chromaticAberration.intensity.value = intensity;
                _chromaticTransitioning = false;
            }
            else
            {
                _chromaticStartValue = _chromaticAberration.intensity.value;
                _chromaticTarget = intensity;
                _chromaticDuration = duration;
                _chromaticElapsed = 0f;
                _chromaticTransitioning = true;
            }
        }

        private void UpdateChromaticAberration(float deltaTime)
        {
            if (!_chromaticTransitioning || _chromaticAberration == null) return;

            _chromaticElapsed += deltaTime;
            float t = Mathf.Clamp01(_chromaticElapsed / _chromaticDuration);
            t = Mathf.SmoothStep(0, 1, t);
            _chromaticAberration.intensity.value = Mathf.Lerp(_chromaticStartValue, _chromaticTarget, t);

            if (t >= 1f)
            {
                _chromaticTransitioning = false;
            }
        }

        public void SetVignette(float intensity, float duration = 0f)
        {
            if (_vignette == null) return;

            if (duration <= 0f)
            {
                _vignette.intensity.value = intensity;
                _vignetteTransitioning = false;
            }
            else
            {
                _vignetteStartValue = _vignette.intensity.value;
                _vignetteTarget = intensity;
                _vignetteDuration = duration;
                _vignetteElapsed = 0f;
                _vignetteTransitioning = true;
            }
        }

        private void UpdateVignette(float deltaTime)
        {
            if (!_vignetteTransitioning || _vignette == null) return;

            _vignetteElapsed += deltaTime;
            float t = Mathf.Clamp01(_vignetteElapsed / _vignetteDuration);
            t = Mathf.SmoothStep(0, 1, t);
            _vignette.intensity.value = Mathf.Lerp(_vignetteStartValue, _vignetteTarget, t);

            if (t >= 1f)
            {
                _vignetteTransitioning = false;
            }
        }

        public void Flash(Color color, float duration)
        {
            if (_colorAdjustments == null) return;

            _flashColor = color;
            _flashDuration = duration;
            _flashElapsed = 0f;
            _flashing = true;
        }

        private void UpdateFlash(float deltaTime)
        {
            if (!_flashing || _colorAdjustments == null) return;

            _flashElapsed += deltaTime;
            float t = Mathf.Clamp01(_flashElapsed / _flashDuration);

            float intensity = 1f - t;
            intensity = Mathf.Pow(intensity, 2f);
            _colorAdjustments.colorFilter.value = Color.Lerp(Color.white, _flashColor, intensity);

            if (t >= 1f)
            {
                _colorAdjustments.colorFilter.value = Color.white;
                _flashing = false;
            }
        }

        public void ResetAll()
        {
            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = _chromaticBaseValue;
                _chromaticTransitioning = false;
            }

            if (_vignette != null)
            {
                _vignette.intensity.value = _vignetteBaseValue;
                _vignetteTransitioning = false;
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.colorFilter.value = Color.white;
                _flashing = false;
            }
        }
    }
}

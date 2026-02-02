using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace StarfireV2
{
    /// <summary>
    /// Poolable impact light that animates a Light2D intensity over a duration then returns to pool.
    /// Managed by ImpactEffectManager — do not add manually.
    /// </summary>
    public class ImpactLight : MonoBehaviour
    {
        [HideInInspector]
        public Light2D light2D;

        private float _duration;
        private float _elapsed;
        private float _baseIntensity;
        private AnimationCurve _intensityCurve;

        public bool IsExpired => _elapsed >= _duration;

        public void Activate(Vector2 position, ImpactLightConfig config)
        {
            transform.position = new Vector3(position.x, position.y, 0f);

            _duration = config.duration;
            _elapsed = 0f;
            _baseIntensity = config.intensity;
            _intensityCurve = config.intensityCurve;

            light2D.color = config.color;
            light2D.intensity = config.intensity;
            light2D.pointLightOuterRadius = config.radius;

            gameObject.SetActive(true);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            light2D.intensity = _baseIntensity * _intensityCurve.Evaluate(t);
        }
    }
}

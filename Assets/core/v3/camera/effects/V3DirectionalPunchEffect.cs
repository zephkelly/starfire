using UnityEngine;

namespace Starfire.Core.V3.Cam.Effects
{
    /// <summary>
    /// Directional camera punch effect that displaces the camera briefly in a given direction.
    /// Uses an animation curve for smooth impulse-style movement.
    /// </summary>
    public class V3DirectionalPunchEffect
    {
        private Vector2 _direction;
        private float _force;
        private float _duration;
        private float _elapsed;
        private readonly AnimationCurve _curve;

        /// <summary>
        /// Whether the effect is currently active.
        /// </summary>
        public bool IsActive => _elapsed < _duration;

        public V3DirectionalPunchEffect()
        {
            _duration = 0.01f;
            _elapsed = _duration;

            // Default curve: quick peak at ~15% through, smooth falloff
            _curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 4f),      // Start at 0, steep rise
                new Keyframe(0.15f, 1f, 0f, 0f),   // Peak at 15%
                new Keyframe(1f, 0f, -2f, 0f)      // Smooth falloff to 0
            );
        }

        /// <summary>
        /// Triggers a directional punch effect.
        /// </summary>
        /// <param name="direction">Direction of the punch (will be normalized).</param>
        /// <param name="force">Force/magnitude of the punch displacement.</param>
        /// <param name="duration">Duration of the effect in seconds.</param>
        public void Trigger(Vector2 direction, float force, float duration = 0.15f)
        {
            _direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
            _force = force;
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
        }

        /// <summary>
        /// Updates the effect timer.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (IsActive)
            {
                _elapsed += deltaTime;
            }
        }

        /// <summary>
        /// Applies the punch displacement to position.
        /// </summary>
        public void Apply(ref Vector3 position, ref float rotation)
        {
            if (!IsActive) return;

            float t = _elapsed / _duration;
            float curveValue = _curve.Evaluate(t);
            Vector2 offset = _direction * _force * curveValue;

            position.x += offset.x;
            position.y += offset.y;
        }

        /// <summary>
        /// Resets the effect to inactive state.
        /// </summary>
        public void Reset()
        {
            _elapsed = _duration;
        }
    }
}

using UnityEngine;

namespace Starfire.Core.V3.Cam.Effects
{
    /// <summary>
    /// Lightweight effects manager for V3 camera system.
    /// Manages screen shake and directional punch effects.
    /// </summary>
    public class V3CameraEffectsManager
    {
        private readonly V3ScreenShakeEffect _shakeEffect;
        private readonly V3DirectionalPunchEffect _punchEffect;

        /// <summary>
        /// Access to the screen shake effect for direct manipulation.
        /// </summary>
        public V3ScreenShakeEffect ShakeEffect => _shakeEffect;

        /// <summary>
        /// Access to the punch effect for direct manipulation.
        /// </summary>
        public V3DirectionalPunchEffect PunchEffect => _punchEffect;

        /// <summary>
        /// Current trauma level from the shake effect.
        /// </summary>
        public float CurrentTrauma => _shakeEffect.Trauma;

        /// <summary>
        /// Whether any effect is currently active.
        /// </summary>
        public bool HasActiveEffects => _shakeEffect.IsActive || _punchEffect.IsActive;

        public V3CameraEffectsManager()
        {
            _shakeEffect = new V3ScreenShakeEffect();
            _punchEffect = new V3DirectionalPunchEffect();
        }

        /// <summary>
        /// Sets the configuration for the shake effect.
        /// </summary>
        public void SetConfig(V3ScreenShakeConfig config)
        {
            _shakeEffect.SetConfig(config);
        }

        /// <summary>
        /// Updates all effects. Call this each frame.
        /// </summary>
        public void Update(float deltaTime)
        {
            _shakeEffect.Update(deltaTime);
            _punchEffect.Update(deltaTime);
        }

        /// <summary>
        /// Applies all active effects to position and rotation.
        /// </summary>
        /// <param name="position">Camera position to modify.</param>
        /// <param name="rotation">Camera Z rotation to modify (degrees).</param>
        public void ApplyEffects(ref Vector3 position, ref float rotation)
        {
            _shakeEffect.Apply(ref position, ref rotation);
            _punchEffect.Apply(ref position, ref rotation);
        }

        /// <summary>
        /// Adds trauma to the screen shake effect.
        /// </summary>
        /// <param name="amount">Trauma amount (0-1 range recommended).</param>
        public void AddTrauma(float amount)
        {
            _shakeEffect.AddTrauma(amount);
        }

        /// <summary>
        /// Triggers a directional punch effect.
        /// </summary>
        /// <param name="direction">Direction of the punch.</param>
        /// <param name="force">Force/displacement of the punch.</param>
        /// <param name="duration">Duration in seconds.</param>
        public void Punch(Vector2 direction, float force, float duration = 0.15f)
        {
            _punchEffect.Trigger(direction, force, duration);
        }

        /// <summary>
        /// Resets all effects to inactive state.
        /// </summary>
        public void Reset()
        {
            _shakeEffect.Reset();
            _punchEffect.Reset();
        }
    }
}

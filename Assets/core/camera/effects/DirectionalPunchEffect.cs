using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    public class DirectionalPunchEffect : ICameraEffect
    {
        public string EffectId => "directional_punch";
        public bool IsActive => _punchTime < _punchDuration;

        private CameraController _controller;

        private Vector2 _punchDirection;
        private float _punchForce;
        private float _punchDuration = 0.15f;
        private float _punchTime;
        private AnimationCurve _punchCurve;

        public DirectionalPunchEffect()
        {
            _punchTime = _punchDuration;
            _punchCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 4f),
                new Keyframe(0.15f, 1f, 0f, 0f),
                new Keyframe(1f, 0f, -2f, 0f)
            );
        }

        public void Initialize(CameraController controller)
        {
            _controller = controller;
        }

        public void Trigger(Vector2 direction, float force, float duration = 0.15f)
        {
            _punchDirection = direction.normalized;
            _punchForce = force;
            _punchDuration = Mathf.Max(0.01f, duration);
            _punchTime = 0f;
        }

        public void Update(float deltaTime)
        {
            if (IsActive)
            {
                _punchTime += deltaTime;
            }
        }

        public void Apply(ref Vector3 position, ref float rotation, ref float zoom)
        {
            if (!IsActive) return;

            float t = _punchTime / _punchDuration;
            float curveValue = _punchCurve.Evaluate(t);

            Vector2 offset = _punchDirection * _punchForce * curveValue;
            position.x += offset.x;
            position.y += offset.y;
        }

        public void Reset()
        {
            _punchTime = _punchDuration;
        }

        public void SetCurve(AnimationCurve curve)
        {
            _punchCurve = curve;
        }
    }
}

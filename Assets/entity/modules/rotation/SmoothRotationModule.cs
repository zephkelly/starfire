using UnityEngine;

namespace Starfire.Entity.Modules.Rotation
{
    public class SmoothRotationModule : IRotationModule
    {
        private readonly SmoothRotationConfig _config;
        private EntityController _controller;
        private float _currentVelocity;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;
        public float RotationSpeed => _config.RotationSpeed * GetTierMultiplier();

        public SmoothRotationModule(SmoothRotationConfig config)
        {
            _config = config;
        }

        public void OnAttach(EntityController controller)
        {
            _controller = controller;
        }

        public void OnDetach()
        {
            _controller = null;
            _currentVelocity = 0f;
        }

        public void OnUpdate(float deltaTime) { }

        public void ProcessRotation(RotationInputData input, float deltaTime)
        {
            if (!IsEnabled || _controller?.Rigidbody == null) return;

            float targetAngle = input.GetTargetAngle(_config.SpriteOffset);

            float angleDelta = Mathf.Abs(Mathf.DeltaAngle(input.CurrentRotation, targetAngle));
            if (angleDelta < _config.Deadzone) return;

            float newAngle;
            float smoothingSpeed = _config.SmoothingFactor * GetTierMultiplier();

            if (_config.UseSmoothDamp)
            {
                newAngle = Mathf.SmoothDampAngle(
                    input.CurrentRotation,
                    targetAngle,
                    ref _currentVelocity,
                    1f / smoothingSpeed,
                    RotationSpeed,
                    deltaTime
                );
            }
            else
            {
                float t = smoothingSpeed * deltaTime;
                newAngle = Mathf.LerpAngle(input.CurrentRotation, targetAngle, t);
            }

            _controller.Rigidbody.MoveRotation(newAngle);
        }

        private float GetTierMultiplier()
        {
            return Tier switch
            {
                ModuleTier.Basic => 0.75f,
                ModuleTier.Standard => 1.0f,
                ModuleTier.Advanced => 1.25f,
                _ => 1.0f
            };
        }
    }
}

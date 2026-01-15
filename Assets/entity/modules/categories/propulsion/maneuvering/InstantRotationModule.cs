using UnityEngine;

namespace Starfire.Entity.Modules.Rotation
{
    public class InstantRotationModule : IRotationShipModule
    {
        private readonly InstantRotationConfig _config;
        private EntityControllerBase _controller;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;
        public float RotationSpeed => _config.RotationSpeed * GetTierMultiplier();

        public InstantRotationModule(InstantRotationConfig config)
        {
            _config = config;
        }

        public void OnAttach(EntityControllerBase controller)
        {
            _controller = controller;
        }

        public void OnDetach()
        {
            _controller = null;
        }

        public void OnUpdate(float deltaTime) { }

        public void ProcessRotation(RotationInputData input, float deltaTime)
        {
            if (!IsEnabled || _controller?.Rigidbody == null) return;

            float targetAngle = input.GetTargetAngle(_config.SpriteOffset);

            if (_config.RespectMaxSpeed)
            {
                float maxDelta = RotationSpeed * deltaTime;
                float angleDelta = Mathf.DeltaAngle(input.CurrentRotation, targetAngle);
                angleDelta = Mathf.Clamp(angleDelta, -maxDelta, maxDelta);
                targetAngle = input.CurrentRotation + angleDelta;
            }

            _controller.Rigidbody.MoveRotation(targetAngle);
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

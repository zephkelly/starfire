using UnityEngine;

namespace Starfire.Entity.Modules.Rotation
{
    [CreateAssetMenu(fileName = "PhysicsRotation", menuName = "Starfire/Modules/Rotation/Physics")]
    public class PhysicsRotationConfig : RotationModuleConfig
    {
        [Header("Physics Rotation Settings")]
        [Tooltip("Maximum torque force applied")]
        [SerializeField] private float maxTorque = 50f;

        [Tooltip("Maximum angular velocity in degrees per second")]
        [SerializeField] private float maxAngularVelocity = 360f;

        [Tooltip("Angular drag when not applying input")]
        [Range(0f, 10f)]
        [SerializeField] private float angularDrag = 2f;

        [Tooltip("Angle threshold in degrees below which no torque is applied")]
        [SerializeField] private float deadzone = 2f;

        [Header("PID Controller")]
        [SerializeField] private float proportionalGain = 10f;
        [SerializeField] private float derivativeGain = 5f;

        public float MaxTorque => maxTorque;
        public float MaxAngularVelocity => maxAngularVelocity;
        public float AngularDrag => angularDrag;
        public float Deadzone => deadzone;
        public float ProportionalGain => proportionalGain;
        public float DerivativeGain => derivativeGain;

        public override IRotationModule CreateModule()
        {
            return new PhysicsRotationModule(this);
        }
    }

    public class PhysicsRotationModule : IRotationModule
    {
        private readonly PhysicsRotationConfig _config;
        private EntityController _controller;
        private float _previousError;
        private float _originalAngularDrag;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;
        public float RotationSpeed => _config.RotationSpeed * GetTierMultiplier();

        public PhysicsRotationModule(PhysicsRotationConfig config)
        {
            _config = config;
        }

        public void OnAttach(EntityController controller)
        {
            _controller = controller;

            if (_controller?.Rigidbody != null)
            {
                _originalAngularDrag = _controller.Rigidbody.angularDamping;
                _controller.Rigidbody.angularDamping = _config.AngularDrag;
            }
        }

        public void OnDetach()
        {
            if (_controller?.Rigidbody != null)
            {
                _controller.Rigidbody.angularDamping = _originalAngularDrag;
            }

            _controller = null;
            _previousError = 0f;
        }

        public void OnUpdate(float deltaTime) { }

        public void ProcessRotation(RotationInputData input, float deltaTime)
        {
            if (!IsEnabled || _controller?.Rigidbody == null) return;

            float error = input.GetAngleDelta(_config.SpriteOffset);

            // Within deadzone - stop rotation
            if (Mathf.Abs(error) < _config.Deadzone)
            {
                _controller.Rigidbody.angularVelocity = 0f;
                _previousError = 0f;
                return;
            }

            float tierMultiplier = GetTierMultiplier();
            float maxTorque = _config.MaxTorque * tierMultiplier;

            float torque = CalculatePIDTorque(error, deltaTime, tierMultiplier);
            torque = Mathf.Clamp(torque, -maxTorque, maxTorque);

            if (Mathf.Abs(torque) > 0.01f)
            {
                _controller.Rigidbody.AddTorque(torque, ForceMode2D.Force);
            }

            float maxAngularVel = _config.MaxAngularVelocity * tierMultiplier * Mathf.Deg2Rad;
            _controller.Rigidbody.angularVelocity = Mathf.Clamp(
                _controller.Rigidbody.angularVelocity,
                -maxAngularVel,
                maxAngularVel
            );
        }

        private float CalculatePIDTorque(float error, float deltaTime, float tierMultiplier)
        {
            float pTerm = error * (_config.ProportionalGain * tierMultiplier);

            float derivative = deltaTime > 0 ? (error - _previousError) / deltaTime : 0f;
            float dTerm = derivative * _config.DerivativeGain;

            _previousError = error;

            return pTerm + dTerm;
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

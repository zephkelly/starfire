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
}

using Starfire.Entity.Modules.Rotation;
using UnityEngine;

namespace StarfireV2
{
    [CreateAssetMenu(fileName = "Rotation", menuName = "StarfireV2/Modules/Rotation")]
    public class ShipRotationModuleConfig : ScriptableObject, IShipModuleConfig
    {
        public ShipModuleTypeId TypeId => ShipModuleTypeId.RotationalThrusters;
        IShipModule IShipModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] private string moduleId = "rotation_module";
        [SerializeField] private string displayName = "Rotation Module";

        [Header("Rotation Mode")]
        [SerializeField] private RotationMode rotationMode = RotationMode.Smooth;

        [Header("Base Stats")]
        [Tooltip("Maximum rotation speed in degrees per second")]
        [SerializeField] private float rotationSpeed = 180f;

        [Tooltip("Offset in degrees to align sprite's forward direction. -90 = sprite faces up, 0 = sprite faces right")]
        [SerializeField] private float spriteOffset = -90f;

        [Header("Instant Mode Settings")]
        [Tooltip("If true, still respects max rotation speed. If false, rotation is truly instant.")]
        [SerializeField] private bool respectMaxSpeed = false;

        [Header("Smooth Mode Settings")]
        [Tooltip("Smoothing factor (higher = faster response)")]
        [Range(1f, 20f)]
        [SerializeField] private float smoothingFactor = 8f;

        [Tooltip("Minimum angle change to apply rotation (deadzone)")]
        [SerializeField] private float smoothDeadzone = 0.5f;

        [Tooltip("Use SmoothDamp instead of Lerp for more natural feel")]
        [SerializeField] private bool useSmoothDamp = true;

        [Header("Physics Mode Settings")]
        [Tooltip("Maximum torque force applied")]
        [SerializeField] private float maxTorque = 50f;

        [Tooltip("Maximum angular velocity in degrees per second")]
        [SerializeField] private float maxAngularVelocity = 360f;

        [Tooltip("Angular drag applied to Rigidbody2D when module is attached")]
        [Range(0f, 10f)]
        [SerializeField] private float angularDrag = 2f;

        [Tooltip("Angle threshold in degrees below which no torque is applied")]
        [SerializeField] private float physicsDeadzone = 2f;

        [Header("Physics Mode - PID Controller")]
        [Tooltip("Proportional gain for PID controller (response strength)")]
        [SerializeField] private float proportionalGain = 10f;

        [Tooltip("Derivative gain for PID controller (dampens oscillation)")]
        [SerializeField] private float derivativeGain = 5f;

        [Header("Thruster-Based Mode Settings")]
        [Tooltip("Individual thruster definitions for physics-accurate rotation")]
        [SerializeField] private ThrusterDefinition[] thrusters;

        [Tooltip("Visual configuration for thruster effects (optional)")]
        [SerializeField] private ThrusterVisualConfig thrusterVisualConfig;

        [Tooltip("Auto-discover ThrusterMarker components on ship prefab")]
        [SerializeField] private bool autoDiscoverThrusters = true;

        [Tooltip("Default thrust for auto-discovered thrusters (N)")]
        [SerializeField] private float defaultThrusterThrust = 100f;

        [Tooltip("Default response time for auto-discovered thrusters (s)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float defaultThrusterResponseTime = 0.05f;

        [Header("Thruster Control Tuning")]
        [Tooltip("Hysteresis to prevent rapid state switching (degrees)")]
        [Range(0f, 10f)]
        [SerializeField] private float stateTransitionHysteresis = 2f;

        [Tooltip("Minimum velocity before considering 'moving toward target' (deg/s)")]
        [Range(0f, 20f)]
        [SerializeField] private float minimumCoastVelocity = 5f;

        [Tooltip("Safety margin for stopping distance (1.0 = exact, 1.2 = 20% early)")]
        [Range(1f, 2f)]
        [SerializeField] private float brakingSafetyMargin = 1.1f;

        [Header("Proportional Control Settings")]
        [Tooltip("Angular velocity below which ship is considered stopped (deg/s)")]
        [Range(0.1f, 5f)]
        [SerializeField] private float velocityDeadzone = 1f;

        [Tooltip("Reference velocity for full braking thrust (deg/s)")]
        [Range(30f, 180f)]
        [SerializeField] private float referenceVelocity = 90f;

        [Tooltip("Reference angle for full acceleration thrust (degrees)")]
        [Range(15f, 90f)]
        [SerializeField] private float referenceAngle = 45f;

        [Tooltip("Angle threshold to enter settling mode (degrees)")]
        [Range(5f, 30f)]
        [SerializeField] private float settlingAngleThreshold = 15f;

        [Tooltip("Max angular velocity to enter settling mode (deg/s). Must slow down below this before settling.")]
        [Range(5f, 30f)]
        [SerializeField] private float settlingVelocityThreshold = 15f;

        [Tooltip("Proportional gain for settling phase")]
        [Range(0.5f, 5f)]
        [SerializeField] private float settlingProportionalGain = 2f;

        [Tooltip("Derivative gain for settling phase (damps oscillation)")]
        [Range(0.5f, 5f)]
        [SerializeField] private float settlingDerivativeGain = 3f;

        [Tooltip("Angle threshold below which settling accepts position as 'good enough' (degrees)")]
        [Range(1f, 10f)]
        [SerializeField] private float settlingAcceptanceThreshold = 3f;

        [Tooltip("Minimum thrust output fraction (prevents thruster stutter)")]
        [Range(0f, 0.1f)]
        [SerializeField] private float minimumThrustFraction = 0.02f;

        [Header("Direction Optimization")]
        [Tooltip("Enable smart direction selection - continues spinning instead of reversing when faster")]
        [SerializeField] private bool enableDirectionOptimization = true;

        [Tooltip("Minimum angular velocity to consider direction optimization (deg/s)")]
        [Range(30f, 120f)]
        [SerializeField] private float directionOptimizationVelocityThreshold = 60f;

        // Identity
        public string ModuleId => moduleId;
        public string DisplayName => displayName;

        // Mode selection
        public RotationMode RotationMode => rotationMode;

        // Base stats
        public float RotationSpeed => rotationSpeed;
        public float SpriteOffset => spriteOffset;

        // Instant mode
        public bool RespectMaxSpeed => respectMaxSpeed;

        // Smooth mode
        public float SmoothingFactor => smoothingFactor;
        public float SmoothDeadzone => smoothDeadzone;
        public bool UseSmoothDamp => useSmoothDamp;

        // Physics mode
        public float MaxTorque => maxTorque;
        public float MaxAngularVelocity => maxAngularVelocity;
        public float AngularDrag => angularDrag;
        public float PhysicsDeadzone => physicsDeadzone;
        public float ProportionalGain => proportionalGain;
        public float DerivativeGain => derivativeGain;

        // Thruster-based mode
        public ThrusterDefinition[] Thrusters => thrusters;
        public ThrusterVisualConfig ThrusterVisualConfig => thrusterVisualConfig;
        public bool AutoDiscoverThrusters => autoDiscoverThrusters;
        public float DefaultThrusterThrust => defaultThrusterThrust;
        public float DefaultThrusterResponseTime => defaultThrusterResponseTime;

        // Thruster control tuning
        public float StateTransitionHysteresis => stateTransitionHysteresis;
        public float MinimumCoastVelocity => minimumCoastVelocity;
        public float BrakingSafetyMargin => brakingSafetyMargin;

        // Proportional control
        public float VelocityDeadzone => velocityDeadzone;
        public float ReferenceVelocity => referenceVelocity;
        public float ReferenceAngle => referenceAngle;
        public float SettlingAngleThreshold => settlingAngleThreshold;
        public float SettlingVelocityThreshold => settlingVelocityThreshold;
        public float SettlingProportionalGain => settlingProportionalGain;
        public float SettlingDerivativeGain => settlingDerivativeGain;
        public float SettlingAcceptanceThreshold => settlingAcceptanceThreshold;
        public float MinimumThrustFraction => minimumThrustFraction;

        // Direction optimization
        public bool EnableDirectionOptimization => enableDirectionOptimization;
        public float DirectionOptimizationVelocityThreshold => directionOptimizationVelocityThreshold;

        public IShipRotationModule CreateModule()
        {
            return new RotationModule(this);
        }
    }
}

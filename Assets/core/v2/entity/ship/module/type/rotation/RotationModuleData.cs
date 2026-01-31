using System;
using Starfire.Entity.Modules.Rotation;
using UnityEngine;

namespace StarfireV2
{
    [Serializable]
    public class RotationModuleData : IModuleRuntimeData
    {
        public string moduleId = "rotation_module";
        public string displayName = "Rotation Module";
        public RotationMode rotationMode = RotationMode.Smooth;

        [Header("Base Stats")]
        public float rotationSpeed = 180f;
        public float spriteOffset = -90f;

        [Header("Instant Mode")]
        public bool respectMaxSpeed;

        [Header("Smooth Mode")]
        [Range(1f, 20f)]
        public float smoothingFactor = 8f;
        public float smoothDeadzone = 0.5f;
        public bool useSmoothDamp = true;

        [Header("Physics Mode")]
        public float maxTorque = 50f;
        public float maxAngularVelocity = 360f;
        [Range(0f, 10f)]
        public float angularDrag = 2f;
        public float physicsDeadzone = 2f;
        public float proportionalGain = 10f;
        public float derivativeGain = 5f;

        [Header("Thruster-Based Mode")]
        public ThrusterDefinition[] thrusters;
        public ThrusterVisualConfig thrusterVisualConfig;
        public bool autoDiscoverThrusters = true;
        public float defaultThrusterThrust = 100f;
        [Range(0f, 0.5f)]
        public float defaultThrusterResponseTime = 0.05f;

        [Header("Thruster Control Tuning")]
        [Range(0f, 10f)]
        public float stateTransitionHysteresis = 2f;
        [Range(0f, 20f)]
        public float minimumCoastVelocity = 5f;
        [Range(1f, 2f)]
        public float brakingSafetyMargin = 1.1f;

        [Header("Proportional Control")]
        [Range(0.1f, 5f)]
        public float velocityDeadzone = 1f;
        [Range(30f, 180f)]
        public float referenceVelocity = 90f;
        [Range(15f, 90f)]
        public float referenceAngle = 45f;
        [Range(5f, 30f)]
        public float settlingAngleThreshold = 15f;
        [Range(5f, 30f)]
        public float settlingVelocityThreshold = 15f;
        [Range(0.5f, 5f)]
        public float settlingProportionalGain = 2f;
        [Range(0.5f, 5f)]
        public float settlingDerivativeGain = 3f;
        [Range(1f, 10f)]
        public float settlingAcceptanceThreshold = 3f;
        [Range(0f, 0.1f)]
        public float minimumThrustFraction = 0.02f;

        [Header("Direction Optimization")]
        public bool enableDirectionOptimization = true;
        [Range(30f, 120f)]
        public float directionOptimizationVelocityThreshold = 60f;

        public string ModuleId => moduleId;
        public ShipModuleTypeId TypeId => ShipModuleTypeId.RotationalThrusters;
    }
}

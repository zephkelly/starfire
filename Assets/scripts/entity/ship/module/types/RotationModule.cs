using System;
using UnityEngine;

namespace Starfire.Entity
{
    public enum RotationMode : byte
    {
        Manual = 0,
        Auto = 1,
    }

    public enum RotationState : byte
    {
        Idle = 0,
        Accelerate = 1,
        Coast = 2,
        Decelerate = 3,
        Settle = 4,
    }

    public class ShipRotationModule : IShipModule
    {
        public string ModuleId { get; private set; }
        public ShipModuleCategory Category => ShipModuleCategory.Rotation;

        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; set; }
        public float HealthCoefficient => CurrentHealth / MaxHealth;
        public float EfficiencyMultiplier => HealthCoefficient;
        public bool IsEnabled { get; set; } = true;

        public RotationMode Mode { get; set; } = RotationMode.Auto;
        public float TurnRate { get; set; } // Degrees per second
        public float TargetHeading { get; set; } // Radians
        public float CurrentHeading { get; set; } // Radians
        public float AngularVelocity { get; set; } // Radians per second
        public float EffectiveTurnRate => TurnRate * EfficiencyMultiplier;


        public RotationState State { get; private set; } = RotationState.Idle;

        private const float SETTLING_THRESHOLD_RADIANS = 0.01f;
        private const float BRAKE_THRESHOLD_RADIANS = 0.1f;

        public void Update(float deltaTime)
        {
            if (!IsEnabled || Mode != RotationMode.Auto) return;
            
            float angleDiff = Mathf.DeltaAngle(CurrentHeading * Mathf.Rad2Deg, TargetHeading * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float absAngleDiff = Mathf.Abs(angleDiff);
            
            switch (State)
            {
                case RotationState.Idle:
                    if (absAngleDiff > SETTLING_THRESHOLD_RADIANS)
                        State = RotationState.Accelerate;
                    break;
                    
                case RotationState.Accelerate:
                    float turnAccel = EffectiveTurnRate * Mathf.Deg2Rad * Mathf.Sign(angleDiff);
                    AngularVelocity = turnAccel;
                    
                    if (absAngleDiff < BRAKE_THRESHOLD_RADIANS)
                        State = RotationState.Decelerate;
                    else
                        State = RotationState.Coast;
                    break;
                    
                case RotationState.Coast:
                    if (absAngleDiff < BRAKE_THRESHOLD_RADIANS)
                        State = RotationState.Decelerate;
                    break;
                    
                case RotationState.Decelerate:
                    AngularVelocity *= 0.5f;
                    
                    if (absAngleDiff < SETTLING_THRESHOLD_RADIANS)
                        State = RotationState.Settle;
                    break;
                    
                case RotationState.Settle:
                    AngularVelocity *= 0.1f;
                    CurrentHeading = TargetHeading;
                    AngularVelocity = 0;
                    State = RotationState.Idle;
                    break;
            }
            
            CurrentHeading += AngularVelocity * deltaTime;
            
            while (CurrentHeading > Mathf.PI * 2) CurrentHeading -= Mathf.PI * 2;
            while (CurrentHeading < 0) CurrentHeading += Mathf.PI * 2;
        }

        public void SetTargetHeading(float headingRadians)
        {
            TargetHeading = headingRadians;
        }

        public void Damage(float amount)
        {
            if (!IsEnabled) return;
            
            CurrentHealth -= amount;
            if (CurrentHealth < 0) CurrentHealth = 0;
        }
        
        public void Repair(float amount)
        {
            CurrentHealth += amount;
            if (CurrentHealth > MaxHealth)
                CurrentHealth = MaxHealth;
        }
    }
}
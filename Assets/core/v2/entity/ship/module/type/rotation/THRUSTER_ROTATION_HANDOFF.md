# Thruster Rotation System - Handoff Document

## System Overview

A physics-based rotational thruster system for a 2D top-down space game in Unity. Ships have multiple thrusters at fixed positions that coordinate to produce rotation using `Rigidbody2D.AddForceAtPosition()` for physics-accurate torque.

## Key Files

| File | Purpose |
|------|---------|
| `RotationModule.cs` | Main module with `ProcessThrusterRotation()` and state machine |
| `ThrusterCoordinator.cs` | Distributes torque across thrusters, applies forces |
| `ThrusterState.cs` | Runtime state per thruster |
| `ThrusterDefinition.cs` | Config data structure for each thruster |
| `ShipRotationModuleConfig.cs` | ScriptableObject with all tuning parameters |

---

## ✅ ISSUE RESOLVED

### Root Cause (CONFIRMED)
The state machine entered SETTLING mode too early while ship still had high angular velocity. The settling check at line 266 happened BEFORE velocity-based coast/brake logic.

### Fixes Applied

#### 1. Added `settlingVelocityThreshold` Config Parameter
New parameter in `ShipRotationModuleConfig.cs` (default: 15 deg/s) that gates entry into settling mode.

#### 2. Restructured State Machine Priority
`DetermineControlState()` now checks in correct order:
1. **IDLE** - Both error < 2° AND velocity < 1°/s
2. **HIGH VELOCITY** - If velocity > settlingVelocityThreshold, use coast/brake logic
3. **SETTLING** - Low velocity AND close to target
4. **ACCELERATE** - Low velocity but far from target

#### 3. Normalized PD Controller with Anti-Reversal
`CalculateSettlingTorque()` now normalizes error and velocity to 0-1 range AND prevents the D term from reversing thrust direction when moving toward target:
```csharp
float normalizedError = absError / settlingAngleThreshold;      // 0-1
float normalizedVelocity = absVelocity / settlingVelocityThreshold;  // 0-1

float pTerm = normalizedError * Mathf.Sign(error) * Kp;
float dTerm = normalizedVelocity * Mathf.Sign(angularVelocity) * Kd;
float rawFactor = pTerm - dTerm;

// KEY FIX: When moving toward target, D term can only REDUCE thrust, not REVERSE it
if (movingTowardTarget)
{
    if (error > 0) rawFactor = Mathf.Max(0f, rawFactor);  // Coast, don't reverse
    else rawFactor = Mathf.Min(0f, rawFactor);
}

float settlingFactor = Mathf.Clamp(rawFactor, -1f, 1f);
return settlingFactor * maxTorque;
```

---

## Current Config Values

| Parameter | Value | Purpose |
|-----------|-------|---------|
| `physicsDeadzone` | 2° | Error tolerance for IDLE |
| `velocityDeadzone` | 1°/s | Velocity tolerance for IDLE |
| `settlingAngleThreshold` | 15° | Error threshold for SETTLING |
| `settlingVelocityThreshold` | 15°/s | **NEW** - Max velocity to enter SETTLING |
| `minimumCoastVelocity` | 5°/s | Min velocity to coast |
| `referenceVelocity` | 90°/s | Full braking reference |
| `referenceAngle` | 45° | Full acceleration reference |
| `settlingProportionalGain` | 2.0 | Normalized PD P-term |
| `settlingDerivativeGain` | 3.0 | Normalized PD D-term |

---

## Testing Checklist

- [ ] Ship at 100°/s velocity, 14° from target → Should BRAKE (not SETTLING)
- [ ] Ship at 10°/s velocity, 14° from target → Should SETTLING
- [ ] Ship at 0.5°/s velocity, 1° from target → Should IDLE
- [ ] Aim mouse 180° from ship → Should: ACCELERATE → COAST → BRAKE → SETTLING → IDLE
- [ ] Final state should have zero angular velocity AND face target
- [ ] All thrusters should stop firing when IDLE

---

## Tuning Tips

If oscillation still occurs:
- Increase `settlingDerivativeGain` (more damping)
- Decrease `settlingProportionalGain` (less aggressive)
- Increase `settlingVelocityThreshold` to enter settling later

If settling is too slow:
- Increase `settlingProportionalGain`
- Decrease `settlingDerivativeGain`

If thrusters stutter at low thrust:
- Increase `minimumThrustFraction` (e.g., 0.05 = 5%)

---

## Architecture Reference

### State Machine Flow
```
ProcessThrusterRotation()
    ↓
DetermineControlState(error, angularVelocity)
    ├─ IDLE: error < 2° && velocity < 1°/s
    ├─ HIGH VELOCITY (> 15°/s): DetermineHighVelocityState()
    │   ├─ Not rotating toward target → BRAKE
    │   ├─ Within stopping distance → BRAKE
    │   ├─ Below min coast velocity → ACCELERATE
    │   └─ Otherwise → COAST
    ├─ LOW VELOCITY + Close to target (< 15°) → SETTLING
    └─ LOW VELOCITY + Far from target → ACCELERATE
    ↓
Calculate*Torque() based on state
    ↓
ThrusterCoordinator.DistributeTorque() → UpdateThrusters() → ApplyForces()
```

### Torque Calculation Methods
- **CalculateAccelerationTorque()** - Proportional to error / referenceAngle
- **CalculateBrakingTorque()** - Proportional to velocity / referenceVelocity
- **CalculateSettlingTorque()** - Normalized PD controller for fine adjustments

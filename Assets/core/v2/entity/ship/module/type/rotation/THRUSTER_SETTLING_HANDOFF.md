# Thruster Rotation System - Settling Behavior Handoff

## Overview

The ship rotation system uses physics-based thrusters to rotate toward a target direction (cursor position). The system has a state machine with states: **Idle**, **Accelerate**, **Coast**, **Brake**, and **Settling**.

## Key Files

| File | Purpose |
|------|---------|
| [RotationModule.cs](RotationModule.cs) | Main rotation logic, state machine, torque calculations |
| [ThrusterCoordinator.cs](thruster/ThrusterCoordinator.cs) | Distributes torque demand across multiple thrusters |
| [ThrusterState.cs](thruster/ThrusterState.cs) | Runtime state for individual thrusters (thrust, damage, efficiency) |
| [ThrusterMarker.cs](visual/ThrusterMarker.cs) | MonoBehaviour placed on ship prefab to mark thruster positions |
| [ShipRotationModuleConfig.cs](ShipRotationModuleConfig.cs) | ScriptableObject config with all tuning parameters |

## Recent Changes (This Session)

### Problem Addressed
When moving the cursor a few pixels after the ship settled, it took **5-6 bursts** to complete small rotations. The settling behavior was too conservative.

### Root Cause
In `CalculateSettlingTorque()`:
1. Overshoot detection (`timeToTarget < 0.1f`) triggered too early
2. Proportional control produced weak thrust for small errors (5° → only 33% thrust)
3. Cycle: burst → coast → brake prematurely → repeat

### Changes Made

#### 1. Increased Settling Burst Multiplier
**File:** `RotationModule.cs:450`
```csharp
// Changed from 0.5f to 0.8f
settlingFactor = Mathf.Sign(error) * normalizedError * _config.SettlingProportionalGain * 0.8f;
```

#### 2. Reduced Overshoot Detection Threshold
**File:** `RotationModule.cs:429`
```csharp
// Changed from 0.1f to 0.05f - allows longer coasting before braking
bool willOvershoot = movingTowardTarget && timeToTarget < 0.05f;
```

#### 3. Added Minimum Thrust Floor for Small Angles
**File:** `RotationModule.cs:449`
```csharp
// Added minimum floor of 0.5 so small corrections get meaningful thrust
float normalizedError = Mathf.Clamp01(absError / _config.SettlingAngleThreshold);
normalizedError = Mathf.Max(normalizedError, 0.5f);
```

**Result:** Small corrections now use at least 80% thrust capacity (`0.5 * 2.0 * 0.8 = 0.8`) instead of weak proportional values.

## Settling Logic Flow

```
CalculateSettlingTorque(error, angularVelocity):
│
├─ If error < 3° AND velocity < 1°/s → ACCEPT (return 0)
│
├─ Check if moving toward target
│
├─ Calculate timeToTarget = error / velocity
│
├─ If moving toward target AND timeToTarget >= 0.05s → COAST (factor = 0)
│
├─ If moving toward target AND timeToTarget < 0.05s → BRAKE (gentle, 0.5x)
│
└─ Else (stopped or moving away) → BURST (0.8x with 0.5 floor)
```

## Key Config Parameters

| Parameter | Default | Purpose |
|-----------|---------|---------|
| `SettlingAngleThreshold` | 15° | Angle below which settling mode activates |
| `SettlingVelocityThreshold` | 15°/s | Velocity below which settling mode activates |
| `SettlingAcceptanceThreshold` | 3° | Error below which position is "good enough" |
| `SettlingProportionalGain` | 2.0 | Multiplier for proportional correction |
| `VelocityDeadzone` | 1°/s | Velocity considered "stopped" |
| `MinimumThrustFraction` | 0.02 | Prevents thruster stutter at low values |

## Other Features Implemented (Previous Sessions)

### Auto-Discovery of ThrusterMarkers
- Place `ThrusterMarker` components on ship prefab children
- `RotationModule.InitializeThrusters()` auto-discovers them
- No need to define `ThrusterDefinition[]` in config

### Per-Thruster Max Thrust
- `ThrusterMarker.maxThrust` field (0 = use config default)
- Allows different thrust values per thruster position

### Thruster Damage System
- `ThrusterState.Efficiency` (0-1) multiplier
- `ThrusterState.ApplyDamage(float percent)` reduces efficiency
- `ThrusterState.Repair(float percent)` restores efficiency
- `EffectiveMaxThrust = BaseMaxThrust * Efficiency`

### Visual Direction Fix
- Exhaust particles now fire opposite to thrust direction (Newton's third law)
- `exhaustDir = -thrustDir` in `InitializeThrusterVisuals()`

## Potential Future Improvements

1. **Impulse-based settling** - Calculate exact impulse needed for small rotations instead of proportional control
2. **Configurable overshoot threshold** - Move `0.05f` magic number to config
3. **Configurable minimum thrust floor** - Move `0.5f` to config
4. **Debug visualization** - Show settling state, predicted trajectory in Scene view

## Testing Checklist

- [ ] Large rotation (90°+): Should accelerate → coast → brake → settle smoothly
- [ ] Small rotation (5-10°): Should complete in 1-2 bursts, not 5-6
- [ ] Micro adjustment (< 3°): Should accept without firing thrusters
- [ ] Continuous cursor movement: Should track smoothly without oscillation
- [ ] Damaged thruster: Should still rotate but with reduced torque capacity

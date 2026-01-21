# Warp Effect System - Handoff Document

## Overview

The warp effect system creates visual feedback when the player travels at high speeds, simulating a "warp/hyperspace" feel through star streaking, nebula expansion, chromatic aberration, lens distortion, and camera zoom.

## Architecture

```
WarpEffectController (MonoBehaviour)
    ├── Reads velocity from tracked Rigidbody2D or CameraController target
    ├── Calculates warp intensity based on speed
    ├── Sets global shader properties
    └── Controls post-processing effects

WarpEffectConfig (ScriptableObject)
    └── Stores all tunable parameters and curves

Shaders (consume global properties)
    ├── Starfield.shader / StarfieldMultiLayer.shader - star streaking
    ├── Nebula.shader - procedural nebula with warp expansion
    └── StylizedNebula.shader - stylized nebula with warp expansion
```

## Key Files

| File | Purpose |
|------|---------|
| `WarpEffectController.cs` | Main controller - tracks velocity, calculates intensity, sets shader globals |
| `WarpEffectConfig.cs` | ScriptableObject with all configurable parameters |
| `WarpDebugPanel.cs` | Editor-only debug panel (F7 to toggle) showing real-time warp values |
| `Nebula.shader` | Procedural nebula shader with warp support |
| `StylizedNebula.shader` | Stylized nebula variant with warp support |

## Shader Global Properties

The `WarpEffectController` sets these globals every frame:

### Core Warp Properties
| Property | Type | Description |
|----------|------|-------------|
| `_WarpIntensity` | float | Current warp intensity (0-1) |
| `_WarpStretch` | float | Star stretch multiplier (1-50) |
| `_WarpDirection` | float4 | Normalized velocity direction (xy) |
| `_WarpBrightnessBoost` | float | Star brightness multiplier |
| `_WarpNebulaStretch` | float | Nebula expansion factor (>=1) |
| `_WarpNebulaFade` | float | Nebula fade amount at warp |

### Streak Shape Properties
| Property | Type | Description |
|----------|------|-------------|
| `_WarpStreakWidth` | float | How thin streaks become |
| `_WarpEdgeSoftnessMin` | float | Edge softness at low warp |
| `_WarpEdgeSoftnessMax` | float | Edge softness at full warp |
| `_WarpLeadingEdgeRatio` | float | Leading edge sharpness |
| `_WarpTrailingFadeStart` | float | Where trailing fade begins |
| `_WarpBlendTransition` | float | Star-to-streak blend range |
| `_WarpTailTaperPower` | float | Tail taper curve power |

### Parallax & Wobble
| Property | Type | Description |
|----------|------|-------------|
| `_WarpDistantMinEffect` | float | Min effect for distant layers |
| `_WarpParallaxMultiplier` | float | Depth scaling multiplier |
| `_WobbleIntensity` | float | Streak wobble amount |
| `_WobbleFrequency` | float | Wobble wave frequency |
| `_WobbleSpeed` | float | Wobble animation speed |

## Configuration Guide

### Speed-Based Mode (Default)
The controller automatically calculates warp intensity from velocity:

```
WarpEffectConfig:
  minSpeedForEffect: 5     // Speed where effect starts
  maxSpeedForEffect: 50    // Speed where effect reaches 100%
  intensitySmoothing: 0.15 // Smoothing time for transitions
```

### Nebula Expansion
Controls how nebulae visually expand during warp:

```
WarpEffectConfig:
  nebulaStretchRatio: 0.3  // 0.1 = 10% expansion, 0.5 = 50% expansion
  nebulaFadeAtFullWarp: 0.3 // How much nebulae dim at full warp
```

## Recent Changes (January 2026)

### Nebula Warp Behavior: Squish → Expand

**Problem:** When warping, nebula layers were visually compressing/squishing, which felt wrong. The desired behavior was expansion/stretching outward.

**Root Cause:** The shader was stretching UV coordinates along the warp direction:
```hlsl
parallel *= _WarpNebulaStretch;  // Stretched UV = compressed visual
```

**Solution:** Inverted the operation to compress UV coordinates instead:
```hlsl
parallel /= _WarpNebulaStretch;  // Compressed UV = expanded visual
```

**Files Modified:**
- `Assets/Shaders/Nebula.shader` (line 380)
- `Assets/Shaders/StylizedNebula.shader` (line 693)

### Nebula Stretch Formula Simplification

**Problem:** The `nebulaStretchRatio` was coupled to star stretch, making small values (like 0.01) still produce large effects because they were multiplied by `(starStretch - 1)` which could be 19+.

**Old Formula:**
```csharp
return Mathf.Lerp(1f, 1f + (starStretch - 1f) * nebulaStretchRatio, intensity);
// With ratio=0.01 and starStretch=20: result = 1.19 at full warp
```

**New Formula:**
```csharp
return 1f + nebulaStretchRatio * intensity;
// With ratio=0.01: result = 1.01 at full warp (direct control)
```

**Files Modified:**
- `Assets/core/camera/effects/WarpEffectConfig.cs` (GetNebulaStretchMultiplier method)

## Debug Panel

Press **F7** in Play mode to toggle the debug panel, which shows:
- System status (controller found, camera found, config assigned)
- Velocity data (raw velocity, speed, warp direction)
- Speed thresholds with visual bar
- Warp state and intensity
- All shader global values

**Test Controls:**
- Hold **Left Shift**: Force warp to 100%
- Press **Space**: Reset warp

## Usage Examples

### Accessing Warp State from Other Scripts
```csharp
var warpController = FindFirstObjectByType<WarpEffectController>();

// Check if warping
if (warpController.IsWarping) { }

// Get current intensity (0-1)
float intensity = warpController.WarpIntensity;

// Subscribe to events
warpController.OnWarpStarted += HandleWarpStart;
warpController.OnWarpEnded += HandleWarpEnd;
warpController.OnWarpIntensityChanged += HandleIntensityChange;
```

### Manual Warp Control (Non-Speed-Based)
```csharp
// Disable speed-based mode in inspector, then:
warpController.StartWarp(duration: 1.5f);
warpController.SetWarpDirection(ship.velocity.normalized);
warpController.StopWarp(duration: 1.0f);

// Or direct control:
warpController.SetWarpIntensity(0.5f);
```

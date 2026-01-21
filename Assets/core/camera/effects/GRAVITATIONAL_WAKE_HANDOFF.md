# Gravitational Wake Effect - Handoff Document

## Overview

A screen-space gravitational distortion effect that creates a "wake" behind the ship at high velocities, simulating warped spacetime. The effect shows a bubble of distortion around the ship that warps objects behind it, similar to gravitational lensing.

## Visual Effect

```
        [Direction of Travel]
               ^
               |
    +----------+----------+
    |   Bow Wave Zone     |
    | (front compression) |
    +------+------+-------+
    |      |      |       |
    |   Bubble Zone       |
    |  (undistorted)      |
    |      |      |       |
    +------+------+-------+
    |  Strong Wake Zone   |
    | (trailing distortion)|
    +---------------------+
```

- **Bow Wave Zone**: Compression effect at the front - "piercing through space"
- **Bubble Zone**: Undistorted area around the ship (breathing animation)
- **Ring Zone**: Gravitational lensing with ripples and noise wobble
- **Wake Zone**: Stronger distortion trailing behind the ship

## Files Created

| File | Purpose |
|------|---------|
| `Assets/Shaders/GravitationalWake.shader` | Fullscreen distortion shader |
| `Assets/core/camera/effects/GravitationalWakeFeature.cs` | URP ScriptableRendererFeature |
| `Assets/core/camera/effects/GravitationalWakePass.cs` | RenderGraph-based render pass |
| `Assets/core/camera/effects/GravitationalWakeConfig.cs` | ScriptableObject configuration |

## Files Modified

| File | Changes |
|------|---------|
| `Assets/core/v3/camera/CameraController.cs` | Added `UpdateWakePosition()` to set ship screen position |
| `Assets/core/camera/effects/WarpEffectConfig.cs` | Added wake configuration parameters (optional, can use dedicated config instead) |
| `Assets/core/camera/effects/WarpEffectController.cs` | Added wake global shader property updates |

## Setup Instructions

### 1. Create Configuration Asset
- Right-click in Project → **Create → Starfire → Effects → Gravitational Wake Config**
- Name it `GravitationalWakeConfig`

### 2. Create Material
- Right-click in Project → **Create → Material**
- Set shader to **Starfire/GravitationalWake**
- Name it `GravitationalWakeMaterial`

### 3. Add Renderer Feature
- Open `Assets/Settings/Renderer2D.asset`
- Click **Add Renderer Feature → Gravitational Wake Feature**
- Assign:
  - **Config**: Your `GravitationalWakeConfig` asset
  - **Wake Material**: Your `GravitationalWakeMaterial`

## Configuration Parameters

### GravitationalWakeConfig

| Parameter | Range | Default | Description |
|-----------|-------|---------|-------------|
| `bubbleRadius` | 0-0.3 | 0.08 | Radius of undistorted zone around ship (screen space) |
| `ringWidth` | 0.05-0.3 | 0.15 | Width of the distortion ring |
| `trailLength` | 0.1-1.0 | 0.5 | How far wake extends behind ship |
| `distortionStrength` | 0-0.15 | 0.03 | Maximum UV distortion amount |
| `trailFalloff` | 0.5-3.0 | 1.5 | How quickly wake fades with distance |
| `directionalBias` | 0-1.0 | 0.7 | 1 = only behind ship, 0 = symmetric |
| `intensityCurve` | Curve | EaseInOut | Maps warp intensity to effect strength |
| `chromaEnabled` | bool | true | Enable chromatic aberration in wake |
| `chromaStrength` | 0-0.02 | 0.003 | Chromatic aberration intensity |
| `activationThreshold` | 0-0.5 | 0.01 | Minimum warp intensity to activate |

### Animation Parameters

| Parameter | Range | Default | Description |
|-----------|-------|---------|-------------|
| `pulseSpeed` | 0.5-4 | 1.5 | Speed of bubble breathing animation |
| `pulseAmount` | 0-0.3 | 0.15 | How much bubble radius breathes (30% = 0.3) |
| `rippleCount` | 1-5 | 2 | Number of ripple waves emanating from bubble |
| `rippleSpeed` | 0.1-1 | 0.3 | Speed of ripples flowing outward |
| `rippleStrength` | 0-0.5 | 0.25 | Strength of ripple distortion |
| `noiseScale` | 5-30 | 12 | Scale of noise pattern for wobble effect |
| `noiseSpeed` | 0.1-2 | 0.5 | Speed of noise animation |
| `noiseStrength` | 0-0.5 | 0.2 | Strength of noise-based wobble |
| `bowWaveStrength` | 0-0.5 | 0.35 | Bow wave strength (piercing effect at front) |

## Architecture

### Data Flow

```
WarpEffectController                    V3 CameraController
       │                                       │
       │ Sets:                                 │ Sets:
       │ - _WarpIntensity                      │ - _WakeCenterPosition
       │ - _WarpDirection                      │   (ship screen position)
       │                                       │
       └───────────────┬───────────────────────┘
                       │
                       ▼
            GravitationalWakeFeature
                       │
                       │ Checks _WarpIntensity > threshold
                       │ Sets wake config globals
                       │
                       ▼
             GravitationalWakePass
                       │
                       │ RenderGraph: Copy → Apply Effect
                       │
                       ▼
             GravitationalWake.shader
                       │
                       │ Reads all globals
                       │ Applies distortion at _WakeCenterPosition
                       │
                       ▼
                 Final Frame
```

### Global Shader Properties

Set by **WarpEffectController**:
- `_WarpIntensity` (float): 0-1 warp effect intensity
- `_WarpDirection` (Vector2): Normalized velocity direction

Set by **V3 CameraController**:
- `_WakeCenterPosition` (Vector2): Ship's position in screen space (0-1)

Set by **GravitationalWakeFeature**:
- `_WakeBubbleRadius`, `_WakeRingWidth`, `_WakeTrailLength`
- `_WakeDistortionStrength`, `_WakeTrailFalloff`, `_WakeDirectionalBias`
- `_WakeChromaStrength`
- Animation: `_WakePulseSpeed`, `_WakePulseAmount`
- Animation: `_WakeRippleCount`, `_WakeRippleSpeed`, `_WakeRippleStrength`
- Animation: `_WakeNoiseScale`, `_WakeNoiseSpeed`, `_WakeNoiseStrength`
- Animation: `_WakeBowWaveStrength`

## Dynamic Ship Position

The ship is not always at screen center due to:
1. **Mouse look-ahead**: Camera offset based on mouse position
2. **Zoom changes**: Affect the offset scaling

The V3 CameraController calculates the ship's screen position:

```csharp
private void UpdateWakePosition()
{
    Vector2 aimOffsetScreen = _currentAimOffset / (_camera.orthographicSize * 2f);
    aimOffsetScreen.x /= _camera.aspect;
    Vector2 shipScreenPos = new Vector2(0.5f, 0.5f) - aimOffsetScreen;
    Shader.SetGlobalVector(WakeCenterPositionId, shipScreenPos);
}
```

## Performance

- **Zero overhead when not warping**: Feature skips entirely when `_WarpIntensity < threshold`
- **Single fullscreen pass**: Two render graph passes (copy + effect)
- **3 texture samples**: Main + 2 for chromatic aberration (can disable)
- **Target**: < 0.5ms at 1080p

## Shader Debug Mode

The shader has debug properties for testing without warp active:

```
[Toggle] _UseDebugValues = 1
_DebugIntensity = 0.5
_DebugDirectionX = 0
_DebugDirectionY = 1
```

Enable on the material to test the effect without requiring actual ship velocity.

## Troubleshooting

### Effect not appearing
1. Check `_WarpIntensity` is being set (use WarpDebugPanel, F7)
2. Verify config and material are assigned in renderer feature
3. Check activation threshold isn't too high

### Effect not following ship
1. Verify V3 CameraController has `UpdateWakePosition()` call
2. Check `_WakeCenterPosition` is being set (add debug log)

### Distortion looks wrong
1. Adjust `distortionStrength` (start low: 0.01-0.03)
2. Check `bubbleRadius` and `ringWidth` ratios
3. Verify `_WarpDirection` is correct (normalized velocity)

### Performance issues
1. Disable chromatic aberration (`chromaEnabled = false`)
2. Increase `activationThreshold` to skip at low intensities
3. Check Profiler for "Gravitational Wake" passes

## Integration with Existing Systems

The wake effect integrates with:
- **WarpEffectController**: Reads `_WarpIntensity` and `_WarpDirection`
- **V3 CameraController**: Sets `_WakeCenterPosition`
- **URP Renderer2D**: Renderer feature injection

No changes required to existing warp star streaking or other effects.

## Animation Effects

The wake effect includes several dynamic animation layers that scale with warp intensity:

### 1. Breathing/Pulse
The bubble radius and ring width animate with a sine wave, creating an organic "breathing" feel. The ring width pulses inversely to the bubble for a more dynamic appearance.

### 2. Ripples
Concentric waves emanate from the bubble edge and flow outward, creating the impression of spacetime being disturbed.

### 3. Noise Wobble
Procedural gradient noise creates organic turbulence in the distortion, preventing the effect from feeling too mechanical.

### 4. Bow Wave (Piercing Effect)
A compression effect at the FRONT of the ship creates the feeling of "piercing through space". Set to 35% of trailing wake strength by default for a subtle but noticeable effect.

## Future Improvements

Potential enhancements:
1. Multiple distortion centers (for multi-ship scenarios)
2. Interaction with other visual effects (nebulae, particles)
3. LOD system for different quality settings
4. Asymmetric bow wave shape for more aggressive "piercing" look

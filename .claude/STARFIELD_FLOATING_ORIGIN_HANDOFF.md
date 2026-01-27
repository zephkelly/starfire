# Starfield Floating Origin Integration - Handoff Document

## Overview

This document describes the integration between the starfield background system and the floating origin system, ensuring visual continuity when the world origin shifts.

## Problem Statement

When the floating origin system shifts the world origin (via `WorldGenerationService`), the camera's world position jumps dramatically (e.g., from 100,000 to 100). The starfield shaders use `_CameraWorldPos` for procedural noise sampling, causing:

1. **Visual Discontinuity**: Starfield/nebula patterns abruptly change because noise is sampled at completely different coordinates
2. **Precision Loss**: Large world coordinates cause floating-point precision issues in shader hash/noise functions

## Solution Architecture

### Virtual Camera Position

Instead of passing the actual camera world position to shaders, we calculate a "virtual position" that maintains continuity across origin shifts:

```
virtualPosition = actualCameraPosition + accumulatedOriginOffset
```

When an origin shift occurs:
- The camera moves from (100000, 50000) to (100, 50) - a shift of (-99900, -49950)
- We add the inverse to our accumulator: accumulator += (99900, 49950)
- Virtual position = (100, 50) + (99900, 49950) = (100000, 50000) ✓

### Coordinate Wrapping

To prevent precision loss over very long play sessions, coordinates are wrapped at 100,000 units:

```csharp
private static float WrapCoordinate(float value, float period)
{
    value = value % period;
    if (value < 0) value += period;
    return value;
}
```

This wrap period is:
- Large enough that noise tiling is imperceptible (~5000 noise features at typical scale)
- Small enough to maintain float precision in shaders

## Modified Files

### 1. StarfieldManager.cs
**Path**: `Assets/core/background/StarfieldManager.cs`

**Changes**:
- Added `using Starfire.Core.V2.World;`
- Added origin shift tracking fields:
  ```csharp
  [System.NonSerialized] private Vector2 _originShiftAccumulator = Vector2.zero;
  [System.NonSerialized] private bool _subscribedToOriginShift = false;
  private const float WRAP_PERIOD = 100000f;
  ```
- Added event subscription methods: `TrySubscribeToOriginShift()`, `UnsubscribeFromOriginShift()`
- Added `HandleOriginShift(Vector2 shiftAmount)` handler
- Modified `UpdateShaderGlobals()` to use virtual position
- Added deferred subscription retry in `LateUpdate()` (handles script execution order)

**Key Method - HandleOriginShift**:
```csharp
private void HandleOriginShift(Vector2 shiftAmount)
{
    // Add inverse of shift to maintain visual continuity
    _originShiftAccumulator -= shiftAmount;

    // Wrap to prevent precision loss
    _originShiftAccumulator.x = WrapCoordinate(_originShiftAccumulator.x, WRAP_PERIOD);
    _originShiftAccumulator.y = WrapCoordinate(_originShiftAccumulator.y, WRAP_PERIOD);

    // Notify layers with runtime state
    foreach (var layer in layers)
    {
        if (layer is ShootingStarLayer shootingLayer)
            shootingLayer.OnOriginShift(shiftAmount);
        else if (layer is CometLayer cometLayer)
            cometLayer.OnOriginShift(shiftAmount);
    }
}
```

**Key Method - UpdateShaderGlobals**:
```csharp
private void UpdateShaderGlobals()
{
    if (_camera == null) return;

    Vector3 camPos = _camera.transform.position;

    // Calculate virtual position for floating origin continuity
    Vector2 virtualPos = new Vector2(camPos.x, camPos.y) + _originShiftAccumulator;

    // Wrap to prevent precision loss
    virtualPos.x = WrapCoordinate(virtualPos.x, WRAP_PERIOD);
    virtualPos.y = WrapCoordinate(virtualPos.y, WRAP_PERIOD);

    Shader.SetGlobalVector(CameraWorldPosID, new Vector4(virtualPos.x, virtualPos.y, 0, 0));
    // ... rest unchanged
}
```

### 2. ShootingStarLayer.cs
**Path**: `Assets/core/background/layers/ShootingStarLayer.cs`

**Changes**:
- Added `OnOriginShift(Vector2 shiftAmount)` method in Runtime API region

**Why Special Handling**: ShootingStarLayer tracks active shooting stars with world-space positions (`startPosition`, `position`, `spawnCameraPosition`). These must be shifted when the origin shifts.

```csharp
public void OnOriginShift(Vector2 shiftAmount)
{
    if (_activeStars == null) return;

    for (int i = 0; i < _activeStars.Count; i++)
    {
        var star = _activeStars[i];
        star.startPosition += shiftAmount;
        star.position += shiftAmount;
        star.spawnCameraPosition += shiftAmount;
        _activeStars[i] = star;
    }
}
```

### 3. CometLayer.cs
**Path**: `Assets/core/background/layers/CometLayer.cs`

**Changes**:
- Added `OnOriginShift(Vector2 shiftAmount)` method (identical pattern to ShootingStarLayer)

```csharp
public void OnOriginShift(Vector2 shiftAmount)
{
    if (_activeComets == null) return;

    for (int i = 0; i < _activeComets.Count; i++)
    {
        var comet = _activeComets[i];
        comet.startPosition += shiftAmount;
        comet.position += shiftAmount;
        comet.spawnCameraPosition += shiftAmount;
        _activeComets[i] = comet;
    }
}
```

## Layers That Don't Need Special Handling

These layers only use global shader uniforms (`_CameraWorldPos`) and have no per-entity runtime state:

- `StarLayer`
- `ShapedStarLayer`
- `NebulaLayer`
- `StylizedNebulaLayer`
- `MultiStarLayer`

They automatically benefit from the virtual position calculation in `StarfieldManager.UpdateShaderGlobals()`.

## Related Systems

### WorldGenerationService
**Path**: `Assets/core/v2/world/WorldGenerationService.cs`

- Singleton accessible via `WorldGenerationService.Instance`
- Fires `OnOriginShift` event with shift amount when origin resets
- Event signature: `public event Action<Vector2> OnOriginShift;`

### Previous Floating Origin Fixes
See `FLOATING_ORIGIN_HANDOFF.md` (if exists) for camera controller and entity position fixes.

## Shader Flow

```
StarfieldManager.UpdateShaderGlobals()
    ↓
_CameraWorldPos = virtualPosition (wrapped)
    ↓
All starfield shaders receive this global:
    - Starfield.shader
    - ShapedStarfield.shader
    - Nebula.shader
    - StylizedNebula.shader
    - ShootingStars.shader
    - Comet.shader
    ↓
Shaders calculate: parallaxOffset = _CameraWorldPos * _ParallaxFactor
    ↓
Noise sampled at stable coordinates
```

## Testing Checklist

1. **Basic Continuity Test**
   - [ ] Move player far from origin (past floating origin threshold, typically ~10,000 units)
   - [ ] Observe origin shift occurs (check console or debug)
   - [ ] Verify starfield/nebula patterns remain visually continuous
   - [ ] No sudden changes in star positions or nebula colors

2. **Active Entity Test**
   - [ ] Have shooting stars/comets active when origin shift occurs
   - [ ] Verify they continue their paths smoothly
   - [ ] No disappearing or teleporting entities

3. **Precision Test**
   - [ ] Travel very far (1,000,000+ units with multiple origin shifts)
   - [ ] Verify noise patterns remain coherent (no flickering)
   - [ ] No degradation in visual quality

4. **Edge Cases**
   - [ ] Enter play mode at large coordinates
   - [ ] Rapid camera movement triggering multiple shifts
   - [ ] Scene reload after origin shifts

## Failure Symptoms and Causes

| Symptom | Likely Cause |
|---------|--------------|
| Abrupt nebula color/pattern change on shift | Virtual position not being calculated correctly |
| Stars jump to new positions | Origin shift accumulator not updating |
| Shooting stars disappear on shift | ShootingStarLayer.OnOriginShift not being called |
| Comets disappear on shift | CometLayer.OnOriginShift not being called |
| Console: null WorldGenerationService | Subscription timing issue - check deferred subscription |
| Noise flickering at large distances | Precision loss - verify wrapping is working |

## Design Decisions

### Why 100,000 Unit Wrap Period?
- At typical `noiseScale=0.05`, feature size is ~20 units
- 100,000 units = ~5,000 features before repeat
- Players cannot visually detect this repetition
- 100,000 fits comfortably in float precision (7 significant digits)

### Why Deferred Subscription?
- `WorldGenerationService.Instance` is set in `Awake()`/`OnEnable()`
- Script execution order may cause `StarfieldManager.OnEnable()` to run first
- Retry in `LateUpdate()` ensures eventual subscription

### Why Not Modify Shaders?
- C#-side wrapping handles precision for typical use cases
- Shader modifications would require changes to 6+ shader files
- More complex and harder to maintain

## Future Considerations

1. **If precision issues persist**: Add `preciseFrac()` function to shaders
2. **If tiling becomes noticeable**: Implement hierarchical noise with multiple wrap periods
3. **If new layer types added**: Check if they have per-entity runtime state requiring shift handling

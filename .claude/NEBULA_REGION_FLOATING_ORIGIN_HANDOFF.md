# Nebula Region Floating Origin Integration - Handoff Document

## Overview

This document describes the integration between the nebula region system and the floating origin system, ensuring nebula regions remain stable when the world origin shifts.

## Problem Statement

The user reported nebulas disappearing with a hard edge when crossing boundaries - "fullscreen toggle" behavior where the entire nebula would appear/disappear instantly rather than fading gradually.

### Root Cause Identified

The nebula shader uses two positions that were in **different coordinate systems**:

1. `_CameraWorldPos` - set by `StarfieldManager` in **virtual space** (actual + origin accumulator, wrapped to 0-100000)
2. `_RegionCenter` - was set by `NebulaRegion` in **actual Unity space** (raw transform.position)

When floating origin shifts occur (configured at 2560 units in this project), these coordinate systems would diverge catastrophically:
- Camera at (50, 50) actual, but `_CameraWorldPos` = (10050, 10050) virtual
- Region at (100, 100) actual, `_RegionCenter` = (100, 100)
- Shader calculates: `distance((10050, 10050), (100, 100))` = ~14000 units
- This exceeds any reasonable radius, so `regionMask = 0` for **entire screen**

### Additional Issue Found

The user also reported the issue occurred at "chunk boundaries" even when near origin (<1000 units). The floating origin fix addresses the coordinate mismatch, but if testing near origin where no origin shifts have occurred, there may be an additional issue that needs runtime debugging with the new logging.

## Solution Implemented

### Pattern: Virtual Position System

Following the same pattern established in `StarfieldManager.cs`, we added virtual position tracking to `NebulaRegionManager`:

```
virtualPosition = actualWorldPosition + accumulatedOriginOffset
```

When an origin shift occurs:
- Camera moves from (10000, 0) to (100, 0) - a shift of (-9900, 0)
- All GameObjects also move by (-9900, 0)
- NebulaRegionManager accumulator: `accumulator -= (-9900, 0)` = accumulator + (9900, 0)
- NebulaRegionConsumer shifts `WorldPosition += (-9900, 0)`
- Virtual position = (shifted WorldPosition) + accumulator = original position

### Coordinate Wrapping

To prevent precision loss over long play sessions, coordinates are wrapped at 100,000 units (same as StarfieldManager).

## Modified Files

### 1. NebulaRegionManager.cs
**Path**: `Assets/core/background/regions/NebulaRegionManager.cs`

**Changes**:
- Added `using Starfire.Core.V2.World;`
- Changed `showDebugGizmos` default to `true`
- Added `logRegionEvents` field for debug logging
- Added floating origin tracking:
  ```csharp
  private Vector2 _originShiftAccumulator = Vector2.zero;
  private bool _subscribedToOriginShift = false;
  private const float WRAP_PERIOD = 100000f;
  ```
- Added `TrySubscribeToOriginShift()` - subscribes to `WorldGenerationService.OnOriginShift`
- Added `UnsubscribeFromOriginShift()` - cleanup on disable
- Added `HandleOriginShift(Vector2 shiftAmount)` - updates accumulator, marks regions dirty
- Added `GetVirtualPosition(Vector2 actualWorldPos)` - converts Unity position to virtual position
- Added `WrapCoordinate(float value, float period)` - helper for coordinate wrapping
- Added debug logging in `CreateRegion()` and `DestroyRegion()`

### 2. NebulaRegion.cs
**Path**: `Assets/core/background/regions/NebulaRegion.cs`

**Changes**:
- `UpdateMaterialProperties()` now uses virtual coordinates:
  ```csharp
  var manager = NebulaRegionManager.Instance;
  Vector2 virtualCenter = manager != null
      ? manager.GetVirtualPosition(WorldPosition)
      : WorldPosition;
  Material.SetVector(RegionCenterID, new Vector4(virtualCenter.x, virtualCenter.y, 0, 0));
  ```
- `IntersectsFrustum()` now accounts for `falloffDistance` in InverseFalloff mode:
  ```csharp
  float effectiveRadius = Config.edgeBehavior == NebulaEdgeBehavior.InverseFalloff
      ? Radius + Config.falloffDistance
      : Radius;
  ```

### 3. NebulaRegionConsumer.cs
**Path**: `Assets/core/v2/world/consumers/NebulaRegionConsumer.cs`

**Changes**:
- `OnOriginShift()` updated with clarifying comments
- Still shifts `WorldPosition` to keep it in sync with Unity space (for gizmos, collision checks)
- `MarkDirty()` removed since `NebulaRegionManager.HandleOriginShift` handles it

## How It Works Now

### Origin Shift Flow
```
WorldGenerationService.OnOriginShift(shiftAmount)
    ↓
NebulaRegionConsumer.OnOriginShift:
    - Shifts WorldPosition by shiftAmount (keeps in sync with Unity space)
    ↓
NebulaRegionManager.HandleOriginShift:
    - accumulator -= shiftAmount (adds inverse)
    - Wraps accumulator to prevent precision loss
    - Marks all regions dirty
    ↓
NebulaRegionManager.LateUpdate:
    - UpdateVisibleRegions() checks IsDirty
    - Calls UpdateMaterialProperties() on dirty regions
    ↓
NebulaRegion.UpdateMaterialProperties:
    - virtualCenter = GetVirtualPosition(WorldPosition)
    - Sets _RegionCenter to virtualCenter
```

### Result
- `_RegionCenter` = (shifted WorldPosition) + accumulator = original virtual position
- `_CameraWorldPos` = (shifted camera position) + StarfieldManager.accumulator = original virtual position
- Distance calculation remains correct across origin shifts

## Testing Checklist

### Test 1: Debug Logging
- [ ] Enable `showDebugGizmos` on NebulaRegionManager (now defaults to true)
- [ ] Enable `logRegionEvents` on NebulaRegionManager (now defaults to true)
- [ ] Enter play mode
- [ ] Observe `[Nebula]` messages in console for region creation/destruction

### Test 2: Near Origin Behavior
- [ ] Start at origin
- [ ] Fly to a procedurally generated nebula
- [ ] Cross chunk boundaries
- [ ] Verify nebula doesn't toggle on/off at boundaries
- [ ] If issues persist, check console logs for unexpected region destruction

### Test 3: Floating Origin Continuity
- [ ] Fly to 2560+ units from origin (triggers floating origin shift)
- [ ] Observe `[Nebula] Origin shift:` message in console
- [ ] Verify nebula doesn't toggle when shift occurs
- [ ] Cross region boundaries and verify smooth falloff

### Test 4: Long Distance Travel
- [ ] Travel 100,000+ units (multiple origin shifts)
- [ ] Verify nebula regions remain stable
- [ ] No visual artifacts or flickering

## Outstanding Investigation

The user reported issues at chunk boundaries even when near origin (<1000 units). With the current config:
- `floatingOriginLimit = 2560f`
- `chunkSize = 500f`
- `loadRadius = 3` (1500 units)
- `unloadRadius = 5` (2500 units)

Chunks should stay loaded across boundaries. If the issue persists after this fix, investigate:
1. **Check console logs** for unexpected region destruction
2. **Timing issues** - regions created before shader globals set
3. **Material pooling** - recycled materials with stale properties
4. **Chunk system config** - verify actual values in scene

## Related Systems

### StarfieldManager
**Path**: `Assets/core/background/StarfieldManager.cs`

The original floating origin implementation for background layers. NebulaRegionManager now follows the same pattern.

### WorldGenerationService
**Path**: `Assets/core/v2/world/WorldGenerationService.cs`

- Singleton accessible via `WorldGenerationService.Instance`
- Fires `OnOriginShift` event when camera exceeds `floatingOriginLimit`
- Event signature: `public event Action<Vector2> OnOriginShift;`

### Nebula Shaders
**Paths**:
- `Assets/Shaders/Nebula.shader`
- `Assets/Shaders/StylizedNebula.shader`

Both shaders have identical `calculateRegionMask()` functions that use:
- `_RegionCenter` - region center in virtual space (now correctly set)
- `_CameraWorldPos` - camera position in virtual space (set by StarfieldManager)
- `_RegionRadius`, `_RegionFalloff`, `_RegionFalloffPower`, `_RegionEdgeMode`

## Failure Symptoms and Causes

| Symptom | Likely Cause |
|---------|--------------|
| Nebula toggles fullscreen at boundaries | Coordinate mismatch (should be fixed now) |
| Nebula disappears at chunk boundary | Region being destroyed - check logs |
| Smooth falloff not working | Check `falloffDistance` and `falloffPower` in config |
| Gizmos not showing | Enable `showDebugGizmos` on NebulaRegionManager |
| No log messages | Enable `logRegionEvents` on NebulaRegionManager |
| Console: null WorldGenerationService | Subscription timing - should retry in LateUpdate |

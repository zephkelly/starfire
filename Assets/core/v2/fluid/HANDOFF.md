# Fluid Wake Effect System - Handoff Document

## Current Status: READY FOR TESTING

The fluid wake simulation system is implemented and should now be functional. Two bugs were fixed during this session.

---

## What Was Fixed

### Bug 1: Registration Timing Race Condition (FIXED)
**Problem:** `FluidObstacleComponent` tried to register with `FluidSimulationManager` before the manager initialized, causing silent registration failures.

**Solution:**
1. Added `DiscoverExistingObstacles()` method to `FluidSimulationManager.cs` that scans for existing obstacles after initialization
2. Added `_registeredWithManager` flag and `Update()` retry logic to `FluidObstacleComponent.cs`

**Files Modified:**
- `Assets/core/v2/fluid/FluidSimulationManager.cs` (lines 220-244)
- `Assets/core/v2/fluid/FluidObstacleComponent.cs` (lines 32, 97-124)

### Bug 2: Obstacle Filtering Too Aggressive (FIXED)
**Problem:** Default config settings filtered out obstacles that weren't in nebula regions or moving fast enough.

**Solution:** Changed default values in `FluidSimulationConfig.cs`:
- `minimumSpeedThreshold`: `0.5f` → `0f`
- `requireNebulaPresence`: `true` → `false`

**File Modified:**
- `Assets/core/v2/fluid/config/FluidSimulationConfig.cs` (lines 65, 72)

**Important:** If you have an existing `FluidSimulationConfig` asset, you must manually update these values in the Inspector (ScriptableObject assets retain their serialized values).

---

## Configuration

### FluidSimulationManager (MonoBehaviour)
| Setting | Description |
|---------|-------------|
| `Log Debug Info` | Enables periodic logging of obstacle filtering stats |
| `Force Simulation Active` | Bypasses all activation checks (useful for testing) |
| `Show Simulation Bounds` | Draws gizmo showing simulation area |

### FluidSimulationConfig (ScriptableObject)
| Setting | Default | Description |
|---------|---------|-------------|
| `resolution` | 256 | Simulation grid resolution (64-512) |
| `simulationWorldSize` | 100 | World-space area covered by simulation |
| `shipPushStrength` | 30 | How strongly ships push gas |
| `influenceRadiusMultiplier` | 3 | Multiplier for ship influence zone |
| `minimumSpeedThreshold` | 0 | Minimum speed to create wake (0 = no filter) |
| `requireNebulaPresence` | false | Only simulate inside nebula regions |
| `maxObstacleDistance` | 80 | Max distance from camera for obstacles |

### FluidVisualizationConfig (ScriptableObject)
Controls colors, noise, and visual appearance of the fluid effect.

---

## How to Use

### Adding Fluid Wake to a Ship
1. Add `FluidObstacleComponent` to the ship GameObject
2. Ensure the ship has a `Rigidbody2D` component
3. Optionally add a `Collider2D` for automatic bounds detection

### Testing the System
1. Enable `Log Debug Info` and `Force Simulation Active` on FluidSimulationManager
2. Enter Play mode
3. Expected console output:
```
[FluidSim] Discovered 1 existing obstacle(s) in scene. Total registered: 1
[FluidSim] Initialized successfully. Resolution: 512, Simulation size: 105.4
[FluidSim] Force activated simulation
[FluidSim] Obstacles - Registered: 1, Active: 1, Skipped (inactive: 0, distance: 0, speed: 0, nebula: 0)
```

---

## Architecture Overview

```
FluidSimulationManager (Central Controller)
    │
    ├── Compute Shaders (FluidSimulation.compute)
    │   ├── AddForces - Ship push/wake forces
    │   ├── Advect - Velocity self-advection
    │   ├── Diffuse - Viscosity
    │   ├── ComputeDivergence
    │   ├── PressureJacobi - Pressure solve
    │   ├── ProjectVelocity - Divergence-free
    │   ├── ApplyBoundaries - Solid boundaries
    │   ├── AdvectDensity - Transport density
    │   ├── DecayDensity - Fade effect
    │   └── InjectDensitySource - Replenish gas
    │
    ├── Visualization (FluidVisualization.shader)
    │   ├── Density texture sampling
    │   ├── FBM noise overlay
    │   ├── Three-color gradient
    │   └── Nebula region masking
    │
    └── Obstacle System
        ├── IFluidObstacle (interface)
        ├── FluidObstacleComponent (MonoBehaviour adapter)
        └── ObstacleData (GPU struct)
```

### Data Flow
```
Ship Movement
    ↓
FluidObstacleComponent.Velocity (from Rigidbody2D)
    ↓
GatherActiveObstacles() - filters by speed, distance, nebula
    ↓
UploadObstacleData() - to ComputeBuffer
    ↓
AddForces kernel - applies radial push + wake
    ↓
Navier-Stokes solver (advect, diffuse, pressure, project)
    ↓
AdvectDensity - density follows velocity
    ↓
FluidVisualization.shader - renders density with noise
```

---

## Key Files

| File | Purpose |
|------|---------|
| `Assets/core/v2/fluid/FluidSimulationManager.cs` | Central controller, compute dispatch |
| `Assets/core/v2/fluid/FluidObstacleComponent.cs` | Ship-to-fluid adapter |
| `Assets/core/v2/fluid/IFluidObstacle.cs` | Interface for obstacle data |
| `Assets/core/v2/fluid/config/FluidSimulationConfig.cs` | Simulation parameters |
| `Assets/core/v2/fluid/config/FluidVisualizationConfig.cs` | Visual settings |
| `Assets/Shaders/FluidSimulation/FluidSimulation.compute` | Navier-Stokes solver |
| `Assets/Shaders/FluidSimulation/FluidVisualization.shader` | Density rendering |

---

## Troubleshooting

### No wake effect visible
1. Check console for `[FluidSim] Discovered X existing obstacle(s)` - if 0, the ship doesn't have `FluidObstacleComponent`
2. Enable `Log Debug Info` to see filtering stats
3. Check if obstacles are being skipped for speed/distance/nebula
4. Verify `Force Simulation Active` is enabled for testing
5. Check your FluidSimulationConfig asset values (may have old defaults)

### Wake appears but ship doesn't affect it
1. Verify ship has `Rigidbody2D` and is actually moving (check velocity)
2. Check `shipPushStrength` and `influenceRadiusMultiplier` values
3. Try increasing `shipPushStrength` to 50-100 for more visible effect

### Performance issues
1. Lower `resolution` (128 or 64 for testing)
2. Reduce `pressureIterations` (10-15)
3. Set `skipWhenNoObstacles = true`
4. Enable `requireNebulaPresence = true` for production

---

## Future Considerations

1. **Nebula Integration:** When ready for production, set `requireNebulaPresence = true` so wakes only appear inside nebula regions
2. **Multiple Ships:** System supports up to 16 simultaneous obstacles
3. **Performance:** Consider LOD system for distant ships or reducing resolution dynamically
4. **Visual Tuning:** Adjust `FluidVisualizationConfig` for desired nebula gas appearance

---

*Last updated: Session where registration timing and filtering bugs were fixed*

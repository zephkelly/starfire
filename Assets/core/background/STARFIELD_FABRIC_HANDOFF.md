# Starfield ↔ World Fabric Integration — Handoff Document

> **Status**: COMPLETE
> **Last Updated**: 2026-01-28

## Overview

Per-layer fabric sampling: each starfield layer samples the World Fabric at its own parallax-adjusted world position, then sets fabric values as **material properties** (not shader globals). This creates depth-aware transitions where deep background layers respond differently to zone changes than foreground layers.

## Architecture

```
WorldFabricService.SampleFabricAtWorldPosition(pos)
        ↓
WorldFabricBridge (singleton, LateUpdate)
  ├─ Per-depth smoothed sample cache (Dictionary<int, SmoothedFabricSample>)
  ├─ GetSmoothedSampleForDepth(parallaxDepth) → SpaceFabricSample
  ├─ ApplyFabricToMaterial(material, parallaxDepth) → sets 9 material props
  └─ Still pushes globals as fallback for non-layer consumers
        ↓
StarfieldLayer.ApplyFabricProperties(material)  [base class method]
  ├─ Called at end of each layer's ConfigureMaterial()
  ├─ Gets WorldFabricBridge.Instance
  └─ Calls bridge.ApplyFabricToMaterial(material, this.parallaxDepth)
        ↓
Each Shader reads from CBUFFER (material properties)
  └─ Applies void dim, nebula tint, anomaly shift, etc.
```

### Per-Layer World Position Offset

Each parallax depth gets a slightly different fabric sample point:
```
samplePos = cameraWorldPos + cameraWorldPos.normalized × parallaxDepth × fabricDepthInfluence
```
`fabricDepthInfluence` (default 10000, configurable on bridge) controls how much depth separates sample points. Deep background layers may be in a different zone than foreground layers.

### Smooth Transitions

All fabric values use exponential interpolation per-depth-bucket:
```csharp
lerpRate = 1 - exp(-dt / (transitionSpeed × 0.33))
smoothed = lerp(smoothed, raw, lerpRate)
```
Default `transitionSpeed` = 3s → ~1s 63% response time.

## Shader Fabric Response Matrix

| Shader | Void | Nebula | Anomaly | Asteroid |
|--------|------|--------|---------|----------|
| Starfield | Dim stars + darken BG | Tint stars | Color shift | — |
| ShapedStarfield | Dim stars + darken BG | Tint stars | Color shift | — |
| StarfieldMultiLayer | Dim stars + darken BG | Tint stars | Color shift | — |
| Nebula | Fade emission (×0.5) | Boost emission (×1.3) | Color shift | — |
| StylizedNebula | Fade emission (×0.5) | Boost emission (×1.3) | Color shift | — |
| ShootingStars | Dim brightness | Tint color | Color shift | — |
| Comet | Dim brightness | Tint coma/tail | Color shift | — |

## Implementation Status

### C# Infrastructure
- [x] WorldFabricBridge — per-depth sampling API + ApplyFabricToMaterial()
- [x] StarfieldLayer base — ApplyFabricProperties() protected method

### Shader Updates (globals → CBUFFER + fabric response)
- [x] Starfield.shader — fabric props moved to CBUFFER
- [x] ShapedStarfield.shader — fabric props moved to CBUFFER
- [x] StarfieldMultiLayer.shader — NEW: full fabric response added (void/nebula/anomaly)
- [x] Nebula.shader — moved to CBUFFER + added anomaly color shift
- [x] StylizedNebula.shader — moved to CBUFFER + added anomaly color shift
- [x] ShootingStars.shader — NEW: full fabric response added
- [x] Comet.shader — NEW: full fabric response added

### Layer C# Classes (call ApplyFabricProperties)
- [x] StarLayer.cs
- [x] MultiStarLayer.cs
- [x] ShapedStarLayer.cs
- [x] NebulaLayer.cs
- [x] StylizedNebulaLayer.cs
- [x] ShootingStarLayer.cs
- [x] CometLayer.cs

## Fabric Material Properties (set per-layer)

| Property | Type | Description |
|----------|------|-------------|
| `_FabricNebulaDensity` | float | Nebula density 0–1 |
| `_FabricAsteroidDensity` | float | Asteroid density 0–1 |
| `_FabricVoidFactor` | float | Void factor 0–1 |
| `_FabricAnomalyStrength` | float | Anomaly strength 0–1 |
| `_FabricVoidStarFade` | float | Max star fade in void |
| `_FabricVoidBgDarken` | float | Max bg darken in void |
| `_FabricNebulaTint` | float4 | Nebula tint color |
| `_FabricNebulaTintStrength` | float | Max nebula tint |
| `_FabricAnomalyShift` | float | Anomaly color shift amount |

## WorldFabricBridge Inspector Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `transitionSpeed` | 3.0 | Seconds for full transition (0.5–10) |
| `fabricDepthInfluence` | 10000 | How much parallax offsets sample point (0–50000) |
| `voidStarFade` | 0.85 | How much void dims stars (0–1) |
| `voidBackgroundDarken` | 0.6 | How much void darkens background (0–1) |
| `nebulaStarTint` | 0.15 | How much nebula tints stars (0–1) |
| `nebulaTintColor` | (0.6, 0.3, 0.7) | Color applied in nebula |
| `anomalyColorShift` | 0.3 | How much anomaly shifts colors (0–1) |

## Key Files

- `Assets/core/background/WorldFabricBridge.cs` — Singleton, per-depth sampling + smoothing + material application
- `Assets/core/background/StarfieldLayer.cs` — Base class with `ApplyFabricProperties()`
- `Assets/core/background/layers/*.cs` — 7 layer implementations (all call ApplyFabricProperties)
- `Assets/Shaders/*.shader` — 7 shader files (all read fabric from CBUFFER)

## Future Considerations

- Asteroid density is sampled but not yet used visually by any shader — could add subtle dust/particle effect
- Per-faction color tinting could be added as additional fabric properties
- Resource density could influence comet/shooting star spawn rates (C# layer logic)

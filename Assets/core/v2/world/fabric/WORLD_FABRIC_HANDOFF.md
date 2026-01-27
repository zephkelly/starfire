# World Fabric System - Onboarding Document

> **Status**: PHASE 1-4.3 COMPLETE - MetaballField-based faction territories + resource influence
> **Last Updated**: 2026-01-28
> **Namespace**: `StarfireV2`

## Overview

The World Fabric system is a layered procedural generation architecture that builds on the existing chunk-based world generation. It enables composable generation of diverse space content: zone types, faction territories, planets, stations, hazards, and resources.

### Metaball Architecture (v2 — replaces Perlin noise zones)

Space properties are now generated using **MetaballField** — implicit blob fields that create large, smooth, bubble-like regions. Each property (nebula, asteroids, void, anomaly) has its own independent field, allowing **overlapping states** (e.g., asteroids inside a nebula).

**Key advantages over the previous Perlin approach:**
- **No noise "scars"**: Metaballs only have energy where blobs exist — vast empty regions are perfectly clean
- **Bubble-like shapes**: Overlapping blobs merge naturally like soap bubbles
- **Overlapping states**: A point can be both nebula AND asteroid belt simultaneously (bitmask flags)
- **Continuous densities**: Each property is a 0-1 float, not a discrete enum

### Starfield Integration

A `WorldFabricBridge` component samples fabric data at the camera position and pushes it to shader globals. All starfield and nebula shaders respond:
- **Void regions**: Stars fade, background darkens
- **Nebula regions**: Stars get subtle color tint, nebula shaders boost emission
- **Anomaly regions**: Stars shift color
- Transitions are smoothly lerped to prevent visual popping

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    WorldFabricService                       │
│              (Orchestrates layers & queries)                │
└─────────────────────────────────────────────────────────────┘
                              │
     ┌────────────────────────┼────────────────────────┐
     ▼                        ▼                        ▼
┌──────────┐           ┌──────────┐            ┌──────────┐
│SpaceZone │──────────►│ Faction  │───────────►│   POI    │
│ Layer    │ depends   │ Layer    │  depends   │  Layer   │
│ Pri: 0   │           │ Pri: 10  │            │ Pri: 20  │
└──────────┘           └──────────┘            └──────────┘
     │                        │                        │
     ▼                        ▼                        ▼
┌──────────┐           ┌──────────┐            ┌──────────┐
│ Metaball │           │ Metaball │            │ Poisson  │
│ Fields   │           │ Fields   │            │  Disk    │
│(4 indep.)│           │(per fac.)│            └──────────┘
└──────────┘           └──────────┘

     ┌─────────────────────────────────────────────────┐
     │              WorldFabricBridge                   │
     │  (Samples fabric → smooth lerp → shader globals)│
     └─────────────────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
   ┌────────────┐     ┌────────────┐      ┌────────────┐
   │ Starfield  │     │  Shaped    │      │  Nebula /  │
   │  .shader   │     │ Starfield  │      │ Stylized   │
   │            │     │  .shader   │      │  .shader   │
   └────────────┘     └────────────┘      └────────────┘
```

### Key Concepts

1. **Layers**: Independent generators that produce specific data types
2. **Priority**: Lower number = runs earlier (0-9: foundation, 10-19: political, 20+: content)
3. **Dependencies**: Layers can read output from earlier layers via `WorldFabricContext`
4. **Queries**: Runtime lookups work anywhere, even outside loaded chunks

## File Structure (Current Implementation)

```
Assets/core/v2/world/fabric/
├── WorldFabricService.cs       # Main orchestrator singleton [DONE]
├── WorldFabricConfig.cs        # Master ScriptableObject config [DONE]
├── WorldFabricContext.cs       # Generation context with layer access [DONE]
├── IWorldLayer.cs              # Layer interface [DONE]
├── WorldLayerData.cs           # Base class for layer data [DONE]
├── IWorldLayerQuery.cs         # Query interface [DONE]
├── WORLD_FABRIC_HANDOFF.md     # This file
│
├── Editor/
│   └── WorldFabricServiceEditor.cs  # Custom inspector with preview [DONE]
│
├── noise/
│   ├── INoiseField.cs          # Base interface [DONE]
│   ├── MetaballField.cs        # Implicit blob field for bubble-like regions [DONE]
│   ├── PerlinNoiseField.cs     # Multi-octave Perlin (used by domain warping) [DONE]
│   ├── VoronoiNoiseField.cs    # Cellular/territory noise (legacy, not used by factions) [DONE]
│   ├── HierarchicalVoronoiField.cs # Two-tier Voronoi (legacy, not used by factions) [DONE]
│   └── WorleyNoiseField.cs     # Hazard zone noise [DONE]
│
├── layers/
│   ├── SpaceZoneConfig.cs      # Per-property metaball field config ScriptableObject [DONE]
│   ├── SpaceZoneLayer.cs       # Bitmask properties + metaball fields + data + query [DONE]
│   ├── ResourceConfig.cs       # Resource distribution config ScriptableObject [DONE]
│   ├── ResourceLayer.cs        # Resource distribution + data + query [DONE]
│   ├── FactionConfig.cs        # Faction config ScriptableObject (metaball-based) [DONE]
│   ├── FactionTerritoryLayer.cs # MetaballField faction territories + data + query [DONE]
│   ├── StarSystemLayer.cs      # Stars and planets [TODO]
│   ├── PointOfInterestLayer.cs # Stations, gates, derelicts [TODO]
│   ├── HazardLayer.cs          # Radiation, anomalies [TODO]
│   └── ResourceLayer.cs        # Mineable resources [TODO]
│
└── consumers/                   # [TODO - Future phase]
    ├── StarSystemConsumer.cs
    ├── POIConsumer.cs
    └── HazardConsumer.cs

Assets/core/background/
├── WorldFabricBridge.cs        # Samples fabric → smooth lerp → shader globals [DONE]
├── StarfieldManager.cs         # Background starfield rendering (reads fabric globals)
└── ...
```

## Layer Priority Reference

| Priority | Tier | Layers |
|----------|------|--------|
| 0-4 | Foundation | SpaceZoneLayer |
| 5-9 | Resources | ResourceLayer |
| 10-19 | Political | FactionTerritoryLayer, StarSystemLayer |
| 20-29 | Structural | PointOfInterestLayer |
| 30-39 | Environmental | HazardLayer, ResourceLayer |
| 40+ | Dynamic | SpawnerLayer (future) |

## Integration with Existing Systems

### WorldGenerationService
World Fabric hooks into existing chunk events:
```csharp
WorldGenerationService.Instance.OnChunkGenerated += HandleChunkGenerated;
```

### FabricNebulaChunkGenerator
Uses `SpaceZoneType.NebulaDense` from the legacy zone query to decide whether to generate nebula regions in a chunk. This continues to work via the backward-compatible `DerivePrimaryZone()` mapping.

### FactionTerritoryLayer
Depends on `SpaceZoneLayerData` and `ResourceLayerData` from the generation context. Each major faction has its own `MetaballField`; zone data modifies faction density (void/anomaly weaken presence) and resource data boosts it.

### Entity System
Ships/AI query world state for behavior:
```csharp
var info = WorldFabricService.Instance.GetWorldInfoAt(myPosition);
if (info.FactionInfo.ControllingFaction != myFaction) EnableCaution();

// Or use the rich fabric sample directly
var sample = info.FabricSample;
if (sample.HasProperty(SpaceProperty.Nebula))
    ReduceSensorRange(sample.NebulaDensity);
```

### Starfield / Background Shaders
`WorldFabricBridge` pushes fabric data as shader globals every frame. See the **WorldFabricBridge** section below for details.

## Runtime Query API

```csharp
// NEW: Sample all space properties at once (preferred)
SpaceFabricSample sample = WorldFabricService.Instance.SampleFabricAt(absolutePosition);
sample.NebulaDensity;                                  // 0-1 continuous
sample.AsteroidDensity;                                // 0-1 continuous
sample.VoidFactor;                                     // 0-1 continuous
sample.AnomalyStrength;                                // 0-1 continuous
sample.ActiveProperties.HasFlag(SpaceProperty.Nebula); // boolean
sample.OverallDensity;                                 // combined activity level

// From Unity world position (handles floating origin)
SpaceFabricSample sample = WorldFabricService.Instance.SampleFabricAtWorldPosition(transform.position);

// Legacy: single zone type (backward compatible)
SpaceZoneType zone = WorldFabricService.Instance.GetZoneAt(absolutePosition);

// Get faction control info
FactionTerritoryInfo faction = WorldFabricService.Instance.GetFactionAt(position);

// Get danger level (0-1)
float danger = WorldFabricService.Instance.GetDangerLevelAt(position);

// Get comprehensive info (includes FabricSample)
WorldLocationInfo info = WorldFabricService.Instance.GetWorldInfoAt(position);
```

## Space Properties (Bitmask System)

Each point in space has **multiple overlapping properties**, each as a continuous 0-1 density:

```csharp
[Flags]
public enum SpaceProperty
{
    None      = 0,
    Void      = 1 << 0,   // Deep empty space
    Nebula    = 1 << 1,   // Gas cloud
    Asteroids = 1 << 2,   // Asteroid field
    Anomaly   = 1 << 3,   // Strange physics
}

SpaceFabricSample sample = WorldFabricService.Instance.SampleFabricAt(position);
sample.NebulaDensity;     // 0-1
sample.AsteroidDensity;   // 0-1
sample.VoidFactor;        // 0-1
sample.AnomalyStrength;   // 0-1
sample.ActiveProperties;  // Bitmask of which exceed threshold
```

### Legacy Zone Types (Backward Compatible)

The old `SpaceZoneType` enum still works — it's derived from the dominant property:

| Zone | Description | Characteristics |
|------|-------------|-----------------|
| DeepVoid | Empty space | Minimal encounters, low resources |
| SparseSpace | Light debris | Rare contacts |
| OpenSpace | Standard | Normal activity |
| AsteroidBelt | Dense rocks | High mining, navigation hazard |
| NebulaDense | Heavy nebula | Sensor interference, faction control reduced |
| Anomaly | Strange physics | High danger, exotic resources |

### SpaceZoneConfig Parameters (MetaballField-based)

The SpaceZoneLayer uses MetaballField for each property. Each field scatters implicit blobs:

**Per-Property MetaballField Parameters:**

| Property | Blob Spacing | Radius Min | Radius Max | Strength | Falloff Power | Threshold |
|----------|-------------|------------|------------|----------|---------------|-----------|
| Nebula | 200,000 | 80,000 | 300,000 | 1.0 | 2.0 | 0.3 |
| Asteroids | 80,000 | 30,000 | 100,000 | 0.8 | 2.5 | 0.3 |
| Void | 300,000 | 150,000 | 400,000 | 1.0 | 1.5 | 0.3 |
| Anomaly | 500,000 | 20,000 | 60,000 | 0.9 | 3.0 | 0.4 |

- **blobSpacing**: Minimum distance between blob centers (controls region density)
- **blobRadiusMin/Max**: Size range of individual blobs
- **blobStrength**: Contribution per blob (overlapping blobs sum)
- **falloffPower**: Edge sharpness (higher = sharper blob edges)
- **threshold**: Below this accumulated density = property not active

## MetaballField Internals

`MetaballField` (`noise/MetaballField.cs`) is the core implicit field generator. It creates smooth, bubble-like regions without any Perlin noise artifacts.

### How It Works

1. **Spatial hashing grid**: The world is divided into cells of size `max(blobSpacing, blobRadiusMax)`. Each cell deterministically spawns 0 or 1 blob based on a PCG-style hash of (cellX, cellY, seed).
2. **Blob properties**: Each blob gets a jittered center position within its cell, a radius in `[radiusMin, radiusMax]`, and a strength variation (0.7–1.0 of base strength). All derived from the cell hash — fully deterministic.
3. **Sampling**: `Sample(position)` checks the current cell and `searchRadius` neighboring cells. For each blob, it computes distance and applies a quintic hermite smooth falloff: `1 - (6s^5 - 15s^4 + 10s^3)` where `s = pow(t, 1/falloffPower)`. Contributions from all nearby blobs are summed.
4. **Threshold**: A blob field value above `threshold` means the property is "active" at that point.
5. **Merging**: When two blobs overlap, their contributions sum naturally, creating smooth soap-bubble-like merged shapes.

### Performance

- **O(nearby blobs)** per sample — typically 1–4 blob checks (search radius is usually 1–2 cells)
- No pre-generation needed — any position can be queried on demand
- Each SpaceZoneLayer creates 4 MetaballFields (nebula, asteroid, void, anomaly) with unique seed offsets (0, 7777, 15555, 23333) to prevent correlation

### Key Types

```csharp
// Configuration for one metaball field
public class MetaballFieldConfig
{
    public float blobSpacing;      // Min distance between blob centers
    public float blobRadiusMin;    // Smallest blob radius
    public float blobRadiusMax;    // Largest blob radius
    public float blobStrength;     // Per-blob contribution (0.1–2.0)
    public float falloffPower;     // Edge sharpness (1–5, higher = sharper)
    public float threshold;        // Activation threshold (0–1)
}

// Detailed sample result
public struct MetaballSampleResult
{
    public float RawDensity;        // Accumulated density from all blobs
    public float NormalizedDensity;  // Clamped to 0–1
    public bool IsActive;            // RawDensity >= threshold
}
```

## WorldFabricBridge (Per-Layer Starfield Integration)

`WorldFabricBridge` (`core/background/WorldFabricBridge.cs`) is a singleton MonoBehaviour that connects the World Fabric system to all starfield layers. Each layer gets its own smoothed fabric sample based on parallax depth, creating depth-aware zone transitions.

See [STARFIELD_FABRIC_HANDOFF.md](../../background/STARFIELD_FABRIC_HANDOFF.md) for full details.

### Data Flow (Per-Layer)

```
Camera position + parallax depth offset
    → WorldFabricService.SampleFabricAtWorldPosition(offsetPos)
    → Per-depth exponential lerp smoothing
    → material.SetFloat/SetVector (9 per-material properties)
    → Each shader reads from CBUFFER (not globals)
```

Global shader properties are still pushed as fallback for non-layer consumers.

### Per-Layer Sampling

Each parallax depth bucket gets a slightly different sample point:
```
samplePos = cameraWorldPos + cameraWorldPos.normalized × parallaxDepth × fabricDepthInfluence
```
`fabricDepthInfluence` (default 10000) controls the offset. Deep background layers may see different zones than foreground layers.

### All 7 Shaders Respond to Fabric

| Shader | Void | Nebula | Anomaly |
|--------|------|--------|---------|
| Starfield | Dim stars + darken BG | Tint stars | Color shift |
| ShapedStarfield | Dim stars + darken BG | Tint stars | Color shift |
| StarfieldMultiLayer | Dim stars + darken BG | Tint stars | Color shift |
| Nebula | Fade emission | Boost emission | Color shift |
| StylizedNebula | Fade emission | Boost emission | Color shift |
| ShootingStars | Dim brightness | Tint color | Color shift |
| Comet | Dim brightness | Tint coma/tail | Color shift |

### Inspector Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `transitionSpeed` | 3.0 | Seconds for full transition (0.5–10) |
| `fabricDepthInfluence` | 10000 | How much parallax offsets sample point (0–50000) |
| `voidStarFade` | 0.85 | How much void dims stars (0–1) |
| `voidBackgroundDarken` | 0.6 | How much void darkens background (0–1) |
| `nebulaStarTint` | 0.15 | How much nebula tints stars (0–1) |
| `nebulaTintColor` | (0.6, 0.3, 0.7) | Color applied to stars in nebula |
| `anomalyColorShift` | 0.3 | How much anomaly shifts star colors (0–1) |
| `showDebugValues` | false | Show on-screen debug overlay |

### Public API

```csharp
// Read the current smoothed sample (for other scripts)
SpaceFabricSample smoothed = bridge.SmoothedSample;
```

## Resource Distribution System

Resources are generated as a separate layer (priority 5) between SpaceZoneLayer and FactionTerritoryLayer. Each resource category has its own MetaballField, and densities are boosted by correlated space zone types.

### Resource Categories

| Category | Seed Offset | Blob Spacing | Radius Range | Zone Correlation |
|----------|-------------|-------------|--------------|------------------|
| Mineral  | 30000       | 120,000     | 40k-150k     | Asteroid zones (×1.5) |
| Ore      | 37777       | 100,000     | 30k-120k     | Asteroid zones (×1.4) |
| Gas      | 45555       | 180,000     | 60k-250k     | Nebula zones (×1.5) |
| Exotic   | 53333       | 400,000     | 15k-50k      | Anomaly zones (×1.8) |
| Water    | 61111       | 150,000     | 50k-200k     | Nebula zones (×1.2) |

### Bitmask System

```csharp
[Flags]
public enum ResourceProperty
{
    None    = 0,
    Mineral = 1 << 0,
    Ore     = 1 << 1,
    Gas     = 1 << 2,
    Exotic  = 1 << 3,
    Water   = 1 << 4,
}
```

### Rarity Tiers

Derived from the weighted `OverallResourceValue` (exotic resources worth 3× minerals):

| Tier | Threshold | Description |
|------|-----------|-------------|
| None | < 0.1 | Barren space |
| Common | ≥ 0.1 | Basic resources |
| Uncommon | ≥ 0.3 | Moderate deposits |
| Rare | ≥ 0.55 | Valuable resource concentration |
| Exotic | ≥ 0.75 | Exceptionally rich area |

### Query API

```csharp
// Sample all resources with zone correlation
ResourceFabricSample sample = WorldFabricService.Instance.SampleResourcesAt(absolutePosition);
sample.MineralDensity;     // 0-1
sample.ExoticDensity;      // 0-1
sample.OverallResourceValue; // Weighted sum
sample.RarityTier;         // None/Common/Uncommon/Rare/Exotic
sample.ActiveResources.HasFlag(ResourceProperty.Gas); // boolean

// From Unity world position
ResourceFabricSample sample = WorldFabricService.Instance.SampleResourcesAtWorldPosition(transform.position);
```

### Data Flow

```
SpaceZoneLayer (pri 0) → ResourceLayer (pri 5) → FactionTerritoryLayer (pri 10)
                              ↑ reads zone data for correlation boosts
```

---

## Faction System

**Hybrid Approach**: Predefined major factions + procedural minor factions.

### Major Factions (Predefined)
- 3-5 handcrafted factions with unique names, colors, and traits
- Control organic metaball-based territories (blob spacing/radius in config)
- Have defined diplomatic relationships

### Minor Factions (Procedural)
- Generated from seed with procedural names
- Smaller territories in gaps between major factions
- Traits derived from zone type (e.g., nebula dwellers, asteroid miners)

### Unclaimed Wilderness

Most of space is **unclaimed** — faction territories are islands of civilization, not a wall-to-wall checkerboard. Territory is unclaimed wherever no faction's MetaballField density exceeds the activation threshold (`factionActivationThreshold`, default 0.3). Additionally, zone modifiers weaken faction presence in voids and anomalies.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `factionActivationThreshold` | 0.3 | Density below which territory is unclaimed (0–1) |
| `voidNeutralBonus` | 0.25 | Faction density reduction multiplier in void regions |
| `anomalyNeutralBonus` | 0.30 | Faction density reduction multiplier in anomaly regions |

Faction territories naturally form organic bubble-like regions around blob clusters, with vast unclaimed gaps between them. Void and anomaly zones further suppress faction density, making those areas almost always unclaimed.

### Territory Mechanics
Territories use a **MetaballField-based system** where each faction has its own implicit blob field. Territory ownership is determined by which faction has the highest density at any given point. This produces naturally organic, bubble-like borders with no grid artifacts.

- Each major faction has its own `MetaballField` with a unique seed offset
- Territory = highest density above threshold wins
- Unclaimed space = no faction exceeds threshold
- `DistanceToBorder` is a density gap (winner - runner-up) — higher = deeper inside territory
- `IsContestedZone` = density gap below `contestedDensityGap`
- Border areas have higher pirate activity

**FactionConfig territory settings:**
| Parameter | Default | Description |
|-----------|---------|-------------|
| `factionBlobSpacing` | 300000 | Distance between blob centers (controls region spacing) |
| `factionBlobRadiusMin` | 100000 | Minimum blob radius |
| `factionBlobRadiusMax` | 400000 | Maximum blob radius |
| `factionBlobStrength` | 1.0 | Contribution strength per blob (0.1-2) |
| `factionFalloffPower` | 2.0 | Edge sharpness (1-5, higher = sharper) |
| `factionActivationThreshold` | 0.3 | Density below which territory is unclaimed (0-1) |
| `contestedDensityGap` | 0.15 | If winner - runner-up < this, zone is contested |
| `controlFadeDensityRange` | 0.3 | Density range over which control fades (0 to 1 strength) |

### Resource Border Influence

Resource-rich areas boost all faction densities at sample time, effectively expanding faction territories toward valuable resources.

`faction_density = raw_metaball_density × zone_modifier + (OverallResourceValue × resourceDensityBoost)`

**FactionConfig resource influence settings:**
| Parameter | Default | Description |
|-----------|---------|-------------|
| `resourceDensityBoost` | 0.3 | How much resource density boosts faction density (0-0.5) |
| `resourceClaimingBoost` | 0.4 | Neutral chance reduction per unit of OverallResourceValue (0-1) |

**Data flow**: ResourceLayer (priority 5) generates `ResourceLayerData` → FactionTerritoryLayer (priority 10) reads it from `WorldFabricContext` for density boosting.

### Zone Influence on Territories

Void and anomaly regions weaken faction presence via multipliers on density:
- Void zones: `density × (1 - voidFactor × voidNeutralBonus)`
- Anomaly zones: `density × (1 - anomalyStrength × anomalyNeutralBonus)`

| Parameter | Default | Description |
|-----------|---------|-------------|
| `voidNeutralBonus` | 0.25 | Faction density reduction in void regions |
| `anomalyNeutralBonus` | 0.30 | Faction density reduction in anomaly regions |

```csharp
// MetaballField-based territories — each faction gets its own field
var fieldConfig = config.GetFactionFieldConfig(); // MetaballFieldConfig from FactionConfig
var factionFields = new MetaballField[factionCount];
for (int i = 0; i < factionCount; i++)
    factionFields[i] = new MetaballField(fieldConfig, seedOffset: (i + 1) * 10000f);

// At any point: sample all fields, highest density wins
for (int i = 0; i < factionCount; i++)
{
    float density = factionFields[i].SampleDetailed(position, worldSeed).NormalizedDensity;
    density = density * zoneMod + resourceBoost;
    // Track winner and runner-up...
}
// Winner above threshold → owns territory
// No one above threshold → unclaimed/neutral
```

## Adding New Content Types

### 1. Create Data Class
```csharp
public class MyLayerData : WorldLayerData
{
    public override string LayerId => "MyLayer";
    public List<MyDefinition> Items = new();
}
```

### 2. Create Layer
```csharp
public class MyLayer : IWorldLayer
{
    public string LayerId => "MyLayer";
    public int Priority => 25;
    public Type DataType => typeof(MyLayerData);
    public IReadOnlyList<Type> Dependencies => new[] { typeof(FactionTerritoryData) };

    public IWorldLayerData Generate(Chunk chunk, WorldFabricContext context)
    {
        var factionData = context.GetLayerData<FactionTerritoryData>();
        // Generate content...
        return data;
    }
}
```

### 3. Register with Service
```csharp
WorldFabricService.Instance.RegisterLayer(new MyLayer(config));
```

## Configuration ScriptableObjects

Create via: `Right-click → Create → Starfire → World Fabric → [Config Type]`

- **WorldFabricConfig** - Master config referencing all layer configs
- **SpaceZoneConfig** - Zone noise parameters
- **ResourceConfig** - Resource field parameters, zone correlation, rarity tiers
- **FactionConfig** - Faction list, territory sizes, border width
- **StarSystemConfig** - Star density, planet counts
- **POIConfig** - Station types, spawn rates
- **HazardConfig** - Radiation/anomaly thresholds
- **ResourceConfig** - Resource type distributions

## Testing & Debug

1. **Enable Gizmos**: Set `showDebugGizmos = true` in WorldFabricConfig
2. **Enable Logging**: Set `logLayerEvents = true`
3. **Seed Testing**: Use fixed seed for reproducible tests
4. **Query Testing**: Use debug panel to query at camera position

## Debug Preview (Inspector)

The `WorldFabricService` component includes a built-in debug preview system that renders world data to a texture in the Inspector.

### Preview Modes

| Mode | Description |
|------|-------------|
| SpaceZones | Shows legacy zone types (void, nebula, asteroid belt, etc.) as discrete colors |
| Factions | Shows faction territories with colors based on faction ID |
| FactionBorders | Highlights contested zones and border areas |
| Danger | Gradient from safe (green) to dangerous (red) |
| Combined | Overlays zones, factions, and borders together |
| NebulaDensity | Heat map of continuous nebula density (black → purple) |
| AsteroidDensity | Heat map of continuous asteroid density (black → brown) |
| VoidFactor | Heat map of continuous void factor (black → dark blue) |
| AnomalyStrength | Heat map of continuous anomaly strength (black → red) |
| MineralDensity | Heat map of mineral resource density (black → gold) |
| OreDensity | Heat map of ore resource density (black → brown) |
| GasDensity | Heat map of gas resource density (black → green) |
| ExoticDensity | Heat map of exotic resource density (black → purple) |
| WaterDensity | Heat map of water resource density (black → blue) |
| ResourceValue | Heat map of weighted overall resource value (black → green → yellow) |

### Inspector Settings

- **Enable Preview**: Toggle the preview system on/off
- **Mode**: Select which data layer to visualize
- **Resolution**: Texture resolution (64-512, higher = more detail)
- **World Size**: How much world space the preview covers
- **Follow Camera**: Auto-center preview on main camera
- **Update Interval**: Seconds between preview refreshes when following camera (0.1-2s, default 0.5s). Higher values improve FPS.
- **Center**: Manual center position when not following camera

### Usage

1. Select the GameObject with `WorldFabricService` component
2. Expand the "World Preview" foldout in the Inspector
3. Enable preview and select a mode
4. Click "Generate Preview" to render the texture
5. Use "Refresh" to regenerate after config changes

### Edit Mode Support

The preview system works both in Play Mode and Edit Mode:
- **Edit Mode**: Creates temporary query objects from the current config. Click "Generate Preview" after changing SpaceZoneConfig or FactionConfig values.
- **Play Mode**: Uses the runtime layer queries directly.

The "Refresh" button invalidates cached edit-mode queries, forcing regeneration on the next preview.

### Legend

The preview includes a color legend explaining what each color represents for the current mode.

## Related Documentation

- [WORLD_GENERATION_HANDOFF.md](WORLD_GENERATION_HANDOFF.md) - Underlying chunk system
- [../../../background/regions/NebulaRegionManager.cs] - Nebula rendering
- [../../STARFIELD_FLOATING_ORIGIN_HANDOFF.md] - Coordinate system

## TODO / Future Work

- [x] Phase 1: Noise infrastructure (INoiseField, Perlin, Voronoi, Worley)
- [x] Phase 2: SpaceZoneLayer implementation
- [x] Phase 3: FactionTerritoryLayer with Voronoi
- [x] Phase 3.5: Domain warping for natural faction borders
- [x] Phase 3.6: Hierarchical Voronoi for empire clustering (contiguous territories)
- [x] Phase 3.7: Edge noise for fine border detail
- [x] Phase 3.8: MetaballField architecture (replaces Perlin zones with bubble-like regions)
- [x] Phase 3.8.1: Bitmask property system (overlapping states: asteroids + nebula)
- [x] Phase 3.8.2: WorldFabricBridge + starfield shader integration (void fading, nebula tint)
- [x] Phase 3.9: Unclaimed wilderness (neutral cells + zone-aware claiming + avalanche hash for organic borders)
- [x] Phase 4.0: ResourceLayer (5 resource types with MetaballFields + zone correlation + rarity tiers)
- [x] Phase 4.1: Resource-aware faction borders (resource density boosts faction presence)
- [x] Phase 4.2: MetaballField-based faction territories (replaces Voronoi — organic bubble borders, per-faction fields, winner-takes-all)
- [ ] Phase 4.3: StarSystemLayer + POILayer
- [ ] Phase 5: HazardLayer + ResourceLayer
- [x] Phase 6: Debug visualization & tooling (Inspector preview with throttling + per-property heat maps)
- [ ] Entity AI integration
- [ ] Save/load galaxy state
- [ ] Multiplayer sync considerations

## Setup Instructions

1. **Create ScriptableObjects** (Right-click → Create → Starfire → World Fabric):
   - Create `SpaceZoneConfig` and configure noise thresholds
   - Create `FactionConfig` and add major factions
   - Create `WorldFabricConfig` and assign the above configs

2. **Add to Scene**:
   - Add `WorldFabricService` component to a GameObject
   - Assign the `WorldFabricConfig`
   - Ensure `WorldGenerationService` exists and is configured

3. **Add WorldFabricBridge** (for starfield integration):
   - Add `WorldFabricBridge` component to the same GameObject as `StarfieldManager`
   - Configure transition speed and visual response parameters
   - Stars will automatically fade in voids and tint in nebula regions

4. **Query the World**:
```csharp
// Rich fabric query (preferred)
var sample = WorldFabricService.Instance.SampleFabricAtWorldPosition(transform.position);
if (sample.ActiveProperties.HasFlag(SpaceProperty.Nebula))
    Debug.Log($"In nebula! Density: {sample.NebulaDensity}");

// Legacy query still works
var info = WorldFabricService.Instance.GetWorldInfoAtWorldPosition(transform.position);
Debug.Log($"Zone: {info.ZoneType}, Faction: {info.FactionInfo.ControllingFaction}");
```

# Tier System

## How Tier Transitions Work

When an entity changes simulation tier (e.g., Active to Sensor as it moves away from the player), two things happen:

1. **TierEvaluationSystem** detects the change via distance check (Burst job) and enables a `TierTransition` component on the entity (main thread, max 8/frame)
2. **TierDispatchSystem** queries entities with enabled `TierTransition`, calls every registered `ITierHandler` in order, then disables the marker

Higher-tier transitions (Strategic/Dormant) are handled by independent systems:

- **FleetFormationSystem** — T2 to T3 (groups nearby sensor-tier ships into fleets)
- **FleetDisbandSystem** — T3 to T2 (unpacks fleets back into individual ships)
- **DormantConversionSystem** — T3 to T4 (converts distant fleets to dormant records)
- **DormantRevivalSystem** — T4 to T3 (revives dormant records into fleets)

## Tier Values

| Tier | Enum | Distance | Description |
|------|------|----------|-------------|
| T0 | `Loaded` | 0-5k | Full physics + rendering |
| T1 | `Active` | 5k-variable | Full physics, no rendering |
| T2 | `Sensor` | variable-100k | No physics, lightweight sensor updates |
| T3 | `Strategic` | 100k-200k | Fleet-level aggregation |
| T4 | `Dormant` | 200k+ | Minimal dormant records |

TierDispatchSystem handles transitions between T0/T1/T2. The fleet/dormant systems handle T2-T3 and T3-T4 independently.

## Core vs Plugin Architecture

The tier system is entity-type agnostic. It provides the framework; entity types register their own behaviors.

### Core Handlers (registered in TierDispatchSystem.OnCreate)

These apply to ANY entity using the tier system:

1. **TierTagHandler** — toggles `RichTierTag`, `VisualTierTag`, `SensorTierTag`
2. **PhysicsTierHandler** — adds/removes `PhysicsWorldIndex` and `DisableRendering`

### Ship Plugin (registered externally by DemoSpawner)

Ship-specific tier behavior, registered during ship bootstrapping:

- **SnapshotTierHandler** — snapshots ship module state (hull, propulsion, rotation, velocity) when entering Sensor tier, restores physics velocity when leaving

```csharp
// In DemoSpawner.Start()
var dispatch = world.GetExistingSystemManaged<TierDispatchSystem>();
dispatch.Register(new SnapshotTierHandler());
```

This pattern means the tier system has zero knowledge of ships, ship modules, or any entity-type-specific components.

## Adding a New Entity Type

Suppose you add space stations. Stations have different components than ships (e.g., `StationPower`, `DockingBay`) and need different tier-change behavior.

### Step 1: Create a handler

```csharp
using Unity.Entities;
using Starfire.Simulation;

namespace Starfire.Systems
{
    public class StationTierHandler : ITierHandler
    {
        public void OnTierChanged(Unity.Entities.Entity entity, SimulationTier from, SimulationTier to,
                                   EntityManager em, EntityCommandBuffer ecb)
        {
            if (!em.HasComponent<StationTag>(entity))
                return;

            if (from <= SimulationTier.Active && to == SimulationTier.Sensor)
            {
                var power = em.GetComponentData<StationPower>(entity);
                var snapshot = em.GetComponentData<StationSnapshot>(entity);
                snapshot.PowerOutput = power.CurrentOutput;
                em.SetComponentData(entity, snapshot);
            }
        }
    }
}
```

### Step 2: Register it during station bootstrapping

```csharp
var dispatch = World.DefaultGameObjectInjectionWorld
    .GetExistingSystemManaged<TierDispatchSystem>();
dispatch.Register(new StationTierHandler());
```

Each entity type owns its tier-change logic. The core tier system never changes.

## Handler Parameters

| Parameter | Type | Use For |
|-----------|------|---------|
| `entity` | `Unity.Entities.Entity` | The entity changing tiers |
| `from` | `SimulationTier` | Previous tier |
| `to` | `SimulationTier` | New tier |
| `em` | `EntityManager` | Read/write components, toggle enableable components |
| `ecb` | `EntityCommandBuffer` | Structural changes (add/remove components, create/destroy entities) |

## Important Notes

- Max 8 tier changes per frame — main-thread `EntityManager` access is safe at this scale
- Use `Unity.Entities.Entity` (fully qualified) in type positions — `Starfire.Entity` namespace shadows the bare `Entity` type
- Handlers receive ALL tier-changing entities — use `em.HasComponent<T>()` to filter by entity type if your handler is type-specific
- `PhysicsWorldIndex` is `ISharedComponentData` — use `ecb.AddSharedComponent()` not `ecb.AddComponent()`
- `DisableRendering` is NOT `IEnableableComponent` — must add/remove via ECB
- Enableable tags (`RichTierTag`, etc.) can be toggled immediately with `em.SetComponentEnabled<T>()`
- Registration order = execution order. Core handlers (tags, physics) run first, then plugins in registration order.

## Execution Order

```
TierEvaluationSystem ── Burst evaluate + publish TierTransition
TierDispatchSystem ──── call handlers, disable markers
FleetFormationSystem ── T2 -> T3 (1Hz)
FleetDisbandSystem ──── T3 -> T2 (1Hz)
DormantConversionSystem T3 -> T4 (1Hz)
DormantRevivalSystem ── T4 -> T3 (1Hz)
```

All run in `SimulationSystemGroup`, before `PhysicsSystemGroup`.

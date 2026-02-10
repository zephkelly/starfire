# Tier System

## How Tier Transitions Work

When an entity changes simulation tier (e.g., Active to Sensor as it moves away from the player):

1. **TierEvaluationSystem** detects the change via distance check (Burst job) and enables a `TierTransition` component on the entity (main thread, max 8/frame)
2. Independent reactive systems each query enabled `TierTransition` and handle one concern
3. **TierTransitionCleanupSystem** disables the `TierTransition` marker after all reactive systems have processed

Higher-tier transitions (Strategic/Dormant) are handled by entity-type-specific systems in `systems/ship/` and `systems/asteroid/`.

## Tier Values

| Tier | Enum | Distance | Description |
|------|------|----------|-------------|
| T0 | `Loaded` | 0-5k | Full physics + rendering |
| T1 | `Active` | 5k-variable | Full physics, no rendering |
| T2 | `Sensor` | variable-100k | No physics, lightweight sensor updates |
| T3 | `Strategic` | 100k-200k | Fleet-level aggregation |
| T4 | `Dormant` | 200k+ | Minimal dormant records |

## Architecture: Independent Reactive Systems

The tier system uses an **event component pattern**. `TierTransition` is an enableable component that acts as a one-frame event. Multiple independent systems react to it, each handling one concern. No central dispatcher, no registration.

### Tier Core Systems (`systems/tier/`)

These apply to ANY entity using the tier system:

| System | Concern | Components |
|--------|---------|------------|
| **TierEvaluationSystem** | Distance evaluation (Burst) | SimulationTierData, TierTransition |
| **TierTagSystem** | Tier tag enablement | RichTierTag, VisualTierTag, SensorTierTag |
| **PhysicsInclusionSystem** | Physics world membership | PhysicsWorldIndex |
| **RenderingStateSystem** | Rendering visibility | DisableRendering |
| **TierTransitionCleanupSystem** | Disables TierTransition marker | TierTransition |

### Entity-Type Systems

Ship systems live in `systems/ship/`, asteroid systems in `systems/asteroid/`.

**Ship Module (`systems/ship/`):**

| System | Tier Transition | Purpose |
|--------|----------------|---------|
| **ShipTierTransitionSystem** | T0/T1 <-> T2 | Snapshot/restore ship state on tier boundary |
| **ShipFleetFormationSystem** | T2 -> T3 (1Hz) | Group sensor-tier ships into fleets |
| **ShipFleetDisbandSystem** | T3 -> T2 (1Hz) | Expand fleets back into individual ships |
| **ShipDormantConversionSystem** | T3 -> T4 (1Hz) | Compress fleets to dormant records |
| **ShipDormantRevivalSystem** | T4 -> T3 (1Hz) | Reconstruct fleets from dormant records |

**Asteroid Module (`systems/asteroid/`):**

| System | Tier Transition | Purpose |
|--------|----------------|---------|
| **AsteroidFieldFormationSystem** | T2 -> T3 (1Hz) | Group sensor-tier asteroids into fields |
| **AsteroidFieldDisbandSystem** | T3 -> T2 (1Hz) | Expand fields back into individual asteroids |
| **AsteroidDormantConversionSystem** | T3 -> T4 (1Hz) | Compress fields to dormant records |
| **AsteroidDormantRevivalSystem** | T4 -> T3 (1Hz) | Reconstruct fields from dormant records |

## Adding a New Entity Type

Suppose you add space stations with `StationPower` and `DockingBay` components.

### Step 1: Create a reactive system

```csharp
using Unity.Entities;
using Unity.Physics.Systems;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierEvaluationSystem))]
    public partial class StationTierTransitionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (transition, entity) in
                SystemAPI.Query<RefRO<TierTransition>>()
                    .WithPresent<TierTransition>()
                    .WithAll<StationTag>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<TierTransition>(entity))
                    continue;

                var from = transition.ValueRO.PreviousTier;
                var to = transition.ValueRO.NewTier;

                if (from <= SimulationTier.Active && to == SimulationTier.Sensor)
                {
                    var power = EntityManager.GetComponentData<StationPower>(entity);
                    var snapshot = EntityManager.GetComponentData<StationSnapshot>(entity);
                    snapshot.PowerOutput = power.CurrentOutput;
                    EntityManager.SetComponentData(entity, snapshot);
                }
            }
        }
    }
}
```

### Step 2: Place it in `systems/station/`

No registration needed. The system automatically queries `TierTransition` events. The `WithAll<StationTag>()` filter ensures it only processes stations.

### Step 3: Add to TierTransitionCleanupSystem ordering

Add `[UpdateAfter(typeof(StationTierTransitionSystem))]` to `TierTransitionCleanupSystem` so the cleanup waits for your system.

## Discoverability

To find all systems for a given entity type and tier:

- **Universal behavior** (tags, physics, rendering): `systems/tier/`
- **Ship-specific behavior**: `systems/ship/`
- **Asteroid-specific behavior**: `systems/asteroid/`
- **Station-specific behavior** (future): `systems/station/`

## Important Notes

- Max 8 tier changes per frame (enforced in TierEvaluationSystem)
- Use `Unity.Entities.Entity` (fully qualified) in type positions
- `PhysicsWorldIndex` is `ISharedComponentData` — use `ecb.AddSharedComponent()` not `ecb.AddComponent()`
- `DisableRendering` is NOT `IEnableableComponent` — must add/remove via ECB
- Enableable tags can be toggled with `EntityManager.SetComponentEnabled<T>()`
- Entity-type systems use `.WithAll<T>()` in queries to filter by entity type
- When adding new entity-type transition systems, add corresponding `[UpdateAfter]` to TierTransitionCleanupSystem

## Execution Order

```
TierEvaluationSystem ────── Burst evaluate + enable TierTransition
  |
  | [UpdateAfter(TierEvaluationSystem)]
  v
TierTagSystem ──────────── set RichTierTag, VisualTierTag, SensorTierTag
PhysicsInclusionSystem ──── manage PhysicsWorldIndex
RenderingStateSystem ────── manage DisableRendering
ShipTierTransitionSystem ── ship snapshot capture/restore
  |
  | [UpdateAfter all above]
  v
TierTransitionCleanupSystem  disable TierTransition markers
  |
  | [UpdateAfter(TierTransitionCleanupSystem)]
  v
ShipFleetFormationSystem ── T2 -> T3 (1Hz)
ShipFleetDisbandSystem ──── T3 -> T2 (1Hz)
ShipDormantConversionSystem T3 -> T4 (1Hz)
ShipDormantRevivalSystem ── T4 -> T3 (1Hz)
AsteroidFieldFormationSystem  T2 -> T3 (1Hz)
AsteroidFieldDisbandSystem ── T3 -> T2 (1Hz)
AsteroidDormantConversionSystem T3 -> T4 (1Hz)
AsteroidDormantRevivalSystem ── T4 -> T3 (1Hz)
```

All run in `SimulationSystemGroup`, before `PhysicsSystemGroup`.

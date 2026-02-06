# System Architecture

This document defines the ECS system execution order, responsibilities, and data flow for Starfire's DOTS architecture.

---

## System Execution Overview

```mermaid
flowchart TB
    subgraph INIT["Initialization Group"]
        direction LR
        CLS[ConfigLoadSystem] --> ESS[EntitySpawnSystem]
        ESS --> TAS[TierAssignSystem]
    end

    subgraph INPUT["Input Group"]
        direction LR
        PIS[PlayerInputSystem] --> AIS[AIInputSystem]
        AIS --> DSS[DriverStackSystem]
    end

    subgraph SIM["Simulation Group (Tier 0-1)"]
        direction LR
        MVS[MovementSystem] --> RTS[RotationSystem]
        RTS --> PHS[PhysicsIntegrationSystem]
    end

    subgraph ENV["Environment Group"]
        direction LR
        SOUS[StarOrbitUpdateSystem] --> BCS[BarycenterCalcSystem]
        BCS --> GSS[GravitySimulatorSystem]
        GSS --> HSS[HeatSimulatorSystem]
    end

    subgraph PHYSICS["Physics Group"]
        direction LR
        BPC[BroadPhaseCollision] --> NPC[NarrowPhaseCollision]
        NPC --> CRS[CollisionResponseSystem]
    end

    subgraph COMBAT["Combat Group"]
        direction LR
        WPS[WeaponSystem] --> PJS[ProjectileSystem]
        PJS --> DLS[DamageLocalizationSystem]
        DLS --> MDS[ModuleDamageSystem]
        MDS --> HDS[HullDamageSystem]
        HDS --> MRS[ModuleRepairSystem]
        MRS --> ATPS[AITargetPrioritySystem]
        ATPS --> QTS[QuestTrackingSystem]
        QTS --> DECS[DamageEventCleanupSystem]
        DECS --> DTS[DestructionSystem]
    end

    subgraph TIER["Tier Management Group"]
        direction LR
        DCS[DistanceCalcSystem] --> TTS[TierTransitionSystem]
        TTS --> CMS[ChunkMigrationSystem]
    end

    subgraph BACKGROUND["Background Simulation Group"]
        direction LR
        T2S[Tier2TacticalSystem] --> T3S[Tier3StrategicSystem]
        T3S --> FLS[FleetSystem]
    end

    subgraph RENDER["Presentation Group"]
        direction LR
        VMS[ViewManagerSystem] --> TRS[TransformSyncSystem]
        TRS --> VES[VisualEffectsSystem]
    end

    INIT --> INPUT
    INPUT --> SIM
    SIM --> ENV
    ENV --> PHYSICS
    PHYSICS --> COMBAT
    COMBAT --> TIER
    TIER --> BACKGROUND
    BACKGROUND --> RENDER
```

---

## System Groups

### 1. Initialization Group

Runs once at startup and when loading save data.

```mermaid
sequenceDiagram
    participant Game as Game Start
    participant CLS as ConfigLoadSystem
    participant Blob as BlobAssets
    participant ESS as EntitySpawnSystem
    participant TAS as TierAssignSystem

    Game->>CLS: Initialize
    CLS->>Blob: Load JSON configs
    Blob-->>CLS: BlobAssetReference<T>
    CLS->>ESS: Configs ready
    ESS->>ESS: Create entity archetypes
    ESS->>TAS: Entities created
    TAS->>TAS: Calculate initial tiers
    TAS->>TAS: Enable tier tags
```

| System | Responsibility |
|--------|---------------|
| `ConfigLoadSystem` | Load JSON → BlobAsset conversion, validate schemas |
| `EntitySpawnSystem` | Create entities from archetype definitions |
| `TierAssignSystem` | Initial tier assignment based on player distance |

---

### 2. Input Group

Collects and resolves control inputs from all sources.

```mermaid
flowchart LR
    subgraph Sources
        KB[Keyboard/Mouse]
        GP[Gamepad]
        AI[AI System]
    end

    subgraph Processing
        PIS[PlayerInputSystem]
        AIS[AIInputSystem]
        DSS[DriverStackSystem]
    end

    subgraph Output
        CI[ControlInput Component]
    end

    KB --> PIS
    GP --> PIS
    AI --> AIS
    PIS --> DSS
    AIS --> DSS
    DSS --> CI
```

| System | Responsibility |
|--------|---------------|
| `PlayerInputSystem` | Read Unity Input System, populate `PlayerControlInput` |
| `AIInputSystem` | Execute behavior trees, populate `AIControlInput` |
| `DriverStackSystem` | Resolve priority, write final `ControlInput` |

**Driver Stack Resolution:**
```csharp
// Pseudo-code for driver stack priority
[BurstCompile]
public void Execute(ref ControlInput output,
                    in PlayerControlInput player,
                    in AIControlInput ai,
                    in DriverStack stack)
{
    if (stack.PlayerPriority > stack.AIPriority && player.IsActive)
        output = player.ToControlInput();
    else if (ai.IsActive)
        output = ai.ToControlInput();
    else
        output = ControlInput.None;
}
```

---

### 3. Simulation Group (Tier 0-1 Only)

Full physics simulation for nearby entities.

```mermaid
flowchart TB
    subgraph Input
        CI[ControlInput]
        PM[PropulsionModule]
        RM[RotationModule]
    end

    subgraph Movement
        MVS[MovementSystem]
        AP[AbsolutePosition]
        VEL[Velocity]
    end

    subgraph Rotation
        RTS[RotationSystem]
        ROT[Rotation Component]
    end

    subgraph Physics
        PHS[PhysicsIntegrationSystem]
        LP[LocalPosition]
        WO[WorldOrigin]
    end

    CI --> MVS
    PM --> MVS
    MVS --> VEL
    MVS --> AP

    CI --> RTS
    RM --> RTS
    RTS --> ROT

    AP --> PHS
    WO --> PHS
    PHS --> LP
```

| System | Responsibility | Query Filter |
|--------|---------------|--------------|
| `MovementSystem` | Apply thrust, update velocity | `ActiveTag` enabled |
| `RotationSystem` | Apply rotation modes (instant/smooth/physics/thruster) | `ActiveTag` enabled |
| `PhysicsIntegrationSystem` | Integrate velocity → position, update local coords | `ActiveTag` OR `LoadedTag` enabled |

**Rotation Mode State Machine:**
```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Accelerate: Input detected
    Accelerate --> Coast: Max angular velocity reached
    Coast --> Brake: Near target angle
    Brake --> Settling: Low velocity
    Settling --> Idle: At target

    note right of Accelerate: Apply torque
    note right of Coast: Maintain velocity
    note right of Brake: Counter-torque
    note right of Settling: Damping
```

---

### 4. Environment Group

Gravity simulation, orbital mechanics, and heat systems. Runs between Simulation and Physics groups.

```mermaid
flowchart TB
    subgraph StarUpdate["Star Position Updates"]
        SOUS[StarOrbitUpdateSystem]
        PRE[Pre-computed Orbits]
        SPOS[Star Positions]
    end

    subgraph Barycenter["Barycenter Calculation"]
        BCS[BarycenterCalculationSystem]
        MULTI[Multi-star Systems]
        BC[Barycenters]
    end

    subgraph Gravity["Gravity Simulation"]
        GSS[GravitySimulatorSystem]
        SOI[SOI Detection]
        VERLET[Verlet Integration]
    end

    subgraph Heat["Heat Simulation"]
        HSS[HeatSimulatorSystem]
        RAD[Radiation Calc]
        COOL[Stefan-Boltzmann]
        DMG[Heat Damage]
    end

    SOUS --> PRE --> SPOS
    SPOS --> BCS
    BCS --> MULTI --> BC
    BC --> GSS
    GSS --> SOI --> VERLET
    VERLET --> HSS
    HSS --> RAD --> COOL --> DMG
```

| System | Responsibility | Query Filter |
|--------|---------------|--------------|
| `StarOrbitUpdateSystem` | Update star positions from pre-computed orbits | Stars with PrecomputedOrbit |
| `BarycenterCalculationSystem` | Calculate barycenters for multi-star systems | StarSystemData |
| `GravitySimulatorSystem` | Velocity Verlet integration, SOI transitions | `ActiveTag` OR `LoadedTag` enabled |
| `HeatSimulatorSystem` | Calculate radiation, cooling, apply heat damage | `ActiveTag` OR `LoadedTag` enabled |

**Gravity Simulation (Tier 0-1):**
- Update star positions from pre-computed Keplerian orbits
- Recalculate barycenters for binary/triple star systems
- Detect SOI (Sphere of Influence) transitions
- Apply Velocity Verlet integration with configurable substeps
- Periodically update orbital elements for Tier 2 prediction

**Heat Simulation (Tier 0-1):**
- Query nearby stars (reuses gravity source data)
- Calculate incoming radiation (inverse-square law)
- Calculate radiative cooling (Stefan-Boltzmann)
- Update hull temperature
- Apply heat damage if exceeding MaxTemperature

See [[10-gravity-system]] and [[11-heat-system]] for detailed documentation.

---

### 5. Physics Group

Collision detection and response.

```mermaid
flowchart TB
    subgraph BroadPhase
        BPC[BroadPhaseCollision]
        SH[Spatial Hash]
        CP[Collision Pairs]
    end

    subgraph NarrowPhase
        NPC[NarrowPhaseCollision]
        CC[Circle-Circle Test]
        CM[Collision Manifold]
    end

    subgraph Response
        CRS[CollisionResponseSystem]
        IMP[Impulse Resolution]
        DMG[Damage Events]
    end

    BPC --> SH
    SH --> CP
    CP --> NPC
    NPC --> CC
    CC --> CM
    CM --> CRS
    CRS --> IMP
    CRS --> DMG
```

| System | Responsibility | Complexity |
|--------|---------------|------------|
| `BroadPhaseCollision` | Spatial hash grid, generate potential pairs | O(n) |
| `NarrowPhaseCollision` | Circle-circle intersection tests | O(pairs) |
| `CollisionResponseSystem` | Impulse resolution, damage event generation | O(collisions) |

**Spatial Hash Configuration:**
```csharp
public struct SpatialHashConfig : IComponentData
{
    public float CellSize;      // 50 units (covers most entities)
    public int MaxEntitiesPerCell;  // 64
    public int GridDimension;   // 256 (12,800 unit coverage)
}
```

---

### 6. Combat Group

Weapons, projectiles, and damage processing including progressive destruction.

```mermaid
sequenceDiagram
    participant WPS as WeaponSystem
    participant PJS as ProjectileSystem
    participant DLS as DamageLocalizationSystem
    participant MDS as ModuleDamageSystem
    participant DAS as HullDamageSystem
    participant MRS as ModuleRepairSystem
    participant DTS as DestructionSystem

    WPS->>WPS: Check fire conditions
    WPS->>PJS: Spawn projectile entity
    PJS->>PJS: Update projectile positions
    PJS->>DLS: On hit, queue PendingDamage with position
    DLS->>DLS: Check shields, reduce damage
    DLS->>DLS: Find hitbox zone from local impact position
    DLS->>MDS: Route remaining damage to zone modules
    MDS->>MDS: Apply damage to modules, update efficiency
    MDS->>DAS: Pass module overflow to hull
    DAS->>DAS: Apply armor reduction
    DAS->>DAS: Update hull integrity
    DAS->>DTS: If hull <= 0
    DTS->>DTS: Destroy entity, spawn effects
    MRS->>MRS: Auto-repair damaged modules
```

| System | Responsibility |
|--------|---------------|
| `WeaponSystem` | Cooldown management, fire commands, projectile spawning |
| `ProjectileSystem` | Projectile movement, lifetime, hit detection, queue `PendingDamage` with world position |
| `DamageLocalizationSystem` | **Shield absorption first**, calculate `LocalImpactPosition`, detect hitbox zone, set `HitZone` in `PendingDamage`. For Tier 2+, marks `IsLocalized = false` to skip zone routing. |
| `ModuleDamageSystem` | Read `HitZone` from `PendingDamage`, distribute damage to modules via zone mappings, update `ModuleHealthElement` states, fire damage events, calculate overflow |
| `HullDamageSystem` | Apply armor reduction to overflow damage, update `HullModule.CurrentIntegrity`, trigger destruction |
| `ModuleRepairSystem` | Auto-repair modules over time, handle repair delays |
| `AITargetPrioritySystem` | Recalculate AI target priority when modules disabled (reads damage events) |
| `QuestTrackingSystem` | Track quest objectives involving module damage (reads damage events) |
| `DamageEventCleanupSystem` | **Clear all damage event buffers** (`ModuleDamagedEvent`, `ModuleDisabledEvent`, `ModuleRepairedEvent`) - runs LAST in Combat Group |
| `DestructionSystem` | Entity destruction, loot drops, explosion effects |

**Damage Event Processing Order:**

Damage events (`ModuleDamagedEvent`, `ModuleDisabledEvent`, `ModuleRepairedEvent`) are stored as buffers on damaged entities. Systems that consume these events:

1. `AITargetPrioritySystem` (Combat Group) - AI recalculates target priority when modules disabled
2. `QuestTrackingSystem` (Combat Group) - Tracks quest objectives involving damage

**Note:** `DamageEffectsSystem` does NOT consume damage events. It runs in Presentation Group and reads `ModuleHealthElement` buffer directly, tracking state changes via `EntityDamageEffects.PreviousWorstState` comparison. This avoids timing issues with event cleanup.

**Critical:** `DamageEventCleanupSystem` runs **last** in the Combat Group and clears all event buffers. All event consumers **must** run within Combat Group and use `[UpdateBefore(typeof(DamageEventCleanupSystem))]` attribute to ensure they process events before cleanup.

See [[09-progressive-destruction]] for detailed documentation on the localized damage system.

### Destruction Pipeline Detail

When an entity's hull reaches 0, the following sequence executes:

```mermaid
sequenceDiagram
    participant HDS as HullDamageSystem
    participant DTS as DestructionSystem
    participant LDS as LootDropSystem
    participant ESS as EffectsSpawnSystem
    participant AIS as AITargetInvalidationSystem
    participant QTS as QuestTrackingSystem
    participant ECS as EntityCleanupSystem

    HDS->>DTS: Hull <= 0, add PendingDestructionTag
    DTS->>LDS: Spawn loot based on entity type
    DTS->>ESS: Spawn explosion/debris effects
    DTS->>AIS: Invalidate AI target references
    DTS->>QTS: Notify quest system (kills, objectives)
    DTS->>ECS: Destroy entity
```

1. **DestructionSystem** marks entity with `PendingDestructionTag`
2. **LootDropSystem** spawns loot based on entity type and inventory
3. **EffectsSpawnSystem** creates explosion particles and debris entities
4. **AITargetInvalidationSystem** clears references from AI entities targeting this entity
5. **QuestTrackingSystem** updates quest objectives if entity was quest-relevant
6. **EntityCleanupSystem** destroys the entity on next frame

**Damage Pipeline (with Progressive Destruction):**
```mermaid
flowchart LR
    subgraph Input
        RAW[Raw Damage]
        TYPE[Damage Type]
        POS[Impact Position]
    end

    subgraph Shield
        SM[ShieldModule]
        SR[Shield Reduction]
    end

    subgraph Zone["Hitbox Zone (Tier 0-1)"]
        ZONE[Zone Detection]
        MOD[Module Damage]
        EFF[Efficiency Reduction]
    end

    subgraph Hull
        HM[HullModule]
        AR[Armor Reduction]
    end

    subgraph Output
        FD[Final Damage]
        DEST[Destruction Check]
    end

    RAW --> SM
    TYPE --> SM
    POS --> ZONE
    SM --> SR
    SR -->|"Penetration"| ZONE
    SR -->|"Shield down"| ZONE
    ZONE --> MOD
    MOD --> EFF
    MOD -->|"Module overflow"| HM
    HM --> AR
    AR --> FD
    FD --> DEST
```

**Note:** All damage routes through zone detection (Tier 0-1) regardless of shield state. Shields reduce incoming damage, then remaining damage goes to zone detection → module damage → hull overflow. For Tier 2+, damage bypasses zone detection and goes directly to hull.

---

### 7. Tier Management Group

Distance calculation and tier transitions.

```mermaid
flowchart TB
    subgraph Distance
        DCS[DistanceCalcSystem]
        PP[Player Position]
        EP[Entity Positions]
        DIST[Distance²]
    end

    subgraph Transition
        TTS[TierTransitionSystem]
        QS[Quality Settings]
        TAGS[Tier Tags]
    end

    subgraph Migration
        CMS[ChunkMigrationSystem]
        OLD[Old Chunk]
        NEW[New Chunk]
    end

    PP --> DCS
    EP --> DCS
    DCS --> DIST
    DIST --> TTS
    QS --> TTS
    TTS --> TAGS
    TTS --> CMS
    CMS --> OLD
    CMS --> NEW
```

| System | Responsibility | Update Rate |
|--------|---------------|-------------|
| `DistanceCalcSystem` | Calculate squared distance to player | Every frame |
| `TierTransitionSystem` | Enable/disable tier tags, handle persistence rules | Every frame |
| `ChunkMigrationSystem` | Update ChunkLocation when entity crosses boundary | On transition |

**Tier Transition Rules (with 15% hysteresis to prevent oscillation):**
```mermaid
stateDiagram-v2
    [*] --> Tier0: distance < 5k
    Tier0 --> Tier1: distance > 5k
    Tier1 --> Tier0: distance < 4.25k
    Tier1 --> Tier2: distance > 20k
    Tier2 --> Tier1: distance < 17k
    Tier2 --> Tier3: distance > 100k
    Tier3 --> Tier2: distance < 85k
    Tier3 --> Tier4: distance > 200k
    Tier4 --> Tier3: distance < 170k

    note right of Tier0: 15% hysteresis + 2s cooldown prevents oscillation
```

**Tier Change Cooldown:** After a tier change, entity cannot change tier again for 2 seconds. This prevents entities moving at boundary velocity from rapidly flickering between tiers.

---

### 8. Background Simulation Group

Reduced-fidelity simulation for distant entities.

```mermaid
flowchart TB
    subgraph Tier2["Tier 2: Tactical"]
        T2S[Tier2TacticalSystem]
        SM[State Machine]
        SIMP[Simplified Physics]
    end

    subgraph Tier3["Tier 3: Strategic"]
        T3S[Tier3StrategicSystem]
        FLS[FleetSystem]
        ABS[Abstract Combat]
    end

    T2S --> SM
    SM --> SIMP
    T3S --> FLS
    FLS --> ABS
```

| System | Responsibility | Update Rate |
|--------|---------------|-------------|
| `Tier2TacticalSystem` | State machine AI, simplified physics | Every 5-10 frames |
| `Tier3StrategicSystem` | Fleet-level decisions, abstract combat | Every ~1 second |
| `FleetSystem` | Fleet formation, member grouping/unpacking | On demand |

**State Machine AI (Tier 2):**
```mermaid
stateDiagram-v2
    [*] --> Patrol
    Patrol --> Pursue: Enemy detected
    Patrol --> Flee: Damaged + outmatched
    Pursue --> Orbit: In range
    Pursue --> Flee: Outmatched
    Orbit --> Pursue: Target fled
    Orbit --> Flee: Damaged
    Flee --> Patrol: Safe distance

    note right of Patrol: Random waypoints
    note right of Pursue: Direct intercept
    note right of Orbit: Engagement range
    note right of Flee: Away from threat
```

---

### 9. Presentation Group

GameObject synchronization and visual effects.

```mermaid
flowchart TB
    subgraph ViewManager
        VMS[ViewManagerSystem]
        PROMO[Promote to View]
        DEMO[Demote from View]
    end

    subgraph Transform
        TRS[TransformSyncSystem]
        LP[LocalPosition]
        GO[GameObject Transform]
    end

    subgraph Effects
        VES[VisualEffectsSystem]
        THR[Thrusters]
        SHD[Shields]
        WPN[Weapon FX]
    end

    VMS --> PROMO
    VMS --> DEMO
    PROMO --> TRS
    TRS --> LP
    LP --> GO
    TRS --> VES
    VES --> THR
    VES --> SHD
    VES --> WPN
```

| System | Responsibility |
|--------|---------------|
| `ViewManagerSystem` | Create/destroy GameObjects for Tier 0 entities |
| `TransformSyncSystem` | Copy ECS LocalPosition → GameObject Transform |
| `VisualEffectsSystem` | Update particle systems, audio, visual state |

### Audio Integration

Audio is managed per-tier with priority-based mixing:

| Tier | Audio Behavior |
|------|----------------|
| Tier 0 | Full 3D spatial audio, all sounds play |
| Tier 1 | Distant sounds only (explosions, large weapons) |
| Tier 2+ | No audio (too far to hear) |

**AudioManager responsibilities:**
- Spatial audio positioning relative to camera
- Sound priority queue (max 32 concurrent sounds)
- Volume attenuation by distance from player
- Cross-fade when entities enter/exit Tier 0
- Sound culling for off-screen entities beyond audio range

**Tier 0 Distance:** Tier 0 (LoadedTag) is enabled for entities within 5,000 units of the player, OR within camera view (whichever is larger). The camera system should clamp maximum zoom to ensure visible range never exceeds the Tier 0 boundary, ensuring all visible entities have GameObjects.

**View Lifecycle:**
```mermaid
sequenceDiagram
    participant ECS as ECS Entity
    participant VMS as ViewManagerSystem
    participant Pool as Object Pool
    participant GO as GameObject

    Note over ECS: Entity enters Tier 0
    ECS->>VMS: LoadedTag enabled
    VMS->>Pool: Request prefab
    Pool-->>VMS: GameObject instance
    VMS->>GO: Set position, rotation
    VMS->>ECS: Store Entity reference

    Note over ECS: Entity exits Tier 0
    ECS->>VMS: LoadedTag disabled
    VMS->>GO: Read final state
    VMS->>Pool: Return to pool
    VMS->>ECS: Clear reference
```

---

## System Dependencies

```mermaid
graph TD
    subgraph Core["Core Dependencies"]
        WO[WorldOrigin Singleton]
        QS[QualitySettings Singleton]
        BA[BlobAssets]
    end

    subgraph Systems
        PHS[PhysicsIntegrationSystem]
        TTS[TierTransitionSystem]
        CLS[ConfigLoadSystem]
        ESS[EntitySpawnSystem]
    end

    WO --> PHS
    QS --> TTS
    BA --> CLS
    BA --> ESS
```

---

## Job Scheduling Strategy

### Parallel Jobs

Systems that can run in parallel within their group:

```csharp
// Movement and Rotation can run in parallel
[BurstCompile]
partial struct MovementJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref Velocity vel, in PropulsionModule prop, in ControlInput input)
    {
        // Update velocity based on input
    }
}

[BurstCompile]
partial struct RotationJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref Rotation rot, in RotationModule rotMod, in ControlInput input)
    {
        // Update rotation based on input
    }
}
```

### Sequential Dependencies

Systems that must wait for previous completion:

```mermaid
flowchart LR
    MV[MovementJob] --> PI[PhysicsIntegration]
    RO[RotationJob] --> PI
    PI --> BP[BroadPhaseCollision]
    BP --> NP[NarrowPhaseCollision]
    NP --> CR[CollisionResponse]
```

---

## Performance Budgets

| Group | Target Budget | Notes |
|-------|--------------|-------|
| Input | 0.5ms | Minimal, mostly reading |
| Simulation | 2ms | Tier 0-1 entities only |
| Environment | 1ms | Gravity + Heat simulation |
| Physics | 3ms | Burst-compiled spatial hash |
| Combat | 1ms | Projectile-heavy scenarios |
| Tier Management | 0.5ms | Distance checks are cheap |
| Background | 1ms | Amortized across frames |
| Presentation | 2ms | Transform sync, effects |
| **Total** | **11ms** | 5ms headroom for 60 FPS |

---

## Warp Mode System Integration

When warp is engaged, system execution changes:

```mermaid
flowchart TB
    subgraph Normal["Normal Mode"]
        ALL[All Systems Active]
    end

    subgraph Warp["Warp Mode"]
        WIS[WarpInputSystem]
        WMS[WarpMovementSystem]
        WCS[WarpCollisionSystem]
        WTS[WarpTierSystem]
    end

    Normal -->|Warp Engage| Warp
    Warp -->|Warp Drop| Normal
```

**Warp Mode Changes:**

| System | Normal Mode | Warp Mode |
|--------|-------------|-----------|
| `MovementSystem` | Standard physics | Disabled |
| `WarpMovementSystem` | Disabled | High-speed trajectory |
| `CollisionSystems` | Full detection | Swept raycast only |
| `EntitySpawnSystem` | Active | Suspended |
| `TierTransitionSystem` | Distance-based | Corridor entities → Tier 4 (Critical stays at Tier 2 min) |

See [07-warp-system.md](07-warp-system.md) for full warp mode documentation.

---

## System Registration

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(RotationSystem))]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new MovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };
        job.ScheduleParallel();
    }
}
```

---

## Related Documentation

- [01-component-model.md](01-component-model.md) - Component definitions
- [03-tiered-simulation.md](03-tiered-simulation.md) - Tier system details
- [04-archetype-strategy.md](04-archetype-strategy.md) - Entity archetypes
- [07-warp-system.md](07-warp-system.md) - Warp mode systems
- [09-progressive-destruction.md](09-progressive-destruction.md) - Localized damage and module degradation
- [10-gravity-system.md](10-gravity-system.md) - Newtonian gravity and orbital mechanics
- [11-heat-system.md](11-heat-system.md) - Thermal radiation and heat damage

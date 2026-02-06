# Starfire ECS Architecture Overview

This document provides a high-level view of the Starfire DOTS architecture, showing how all systems work together.

---

## Architecture Layers

```mermaid
graph TB
    subgraph CONFIG["Configuration Layer"]
        JSON[JSON Configs]
        BLOB[BlobAssets]
        REG[ConfigRegistry]
    end

    subgraph DOMAIN["Domain Layer"]
        COMP[Components]
        ARCH[Archetypes]
        TAG[Tags]
    end

    subgraph SIMULATION["Simulation Layer"]
        SYS[System Groups]
        TIER[Tier Management]
        AI[AI Systems]
    end

    subgraph WORLD["World Layer"]
        CHUNK[Chunk Manager]
        FLOAT[Floating Origin]
        WARP[Warp System]
        VIEW[View Manager]
    end

    CONFIG --> DOMAIN
    DOMAIN --> SIMULATION
    SIMULATION <--> WORLD

    click JSON "[[05-configuration-layer]]"
    click COMP "[[01-component-model]]"
    click ARCH "[[04-archetype-strategy]]"
    click SYS "[[02-system-architecture]]"
    click TIER "[[03-tiered-simulation]]"
    click CHUNK "[[06-chunk-integration]]"
    click WARP "[[07-warp-system]]"
    click CTRL "[[08-control-modes]]"
```

| Layer | Documents | Responsibility |
|-------|-----------|----------------|
| **Configuration** | [[05-configuration-layer]] | JSON → BlobAsset pipeline, mod support |
| **Domain** | [[01-component-model]], [[04-archetype-strategy]] | Data structures, entity definitions |
| **Simulation** | [[02-system-architecture]], [[03-tiered-simulation]] | System execution, tier management |
| **Environment** | [[10-gravity-system]], [[11-heat-system]] | Newtonian gravity, orbital mechanics, thermal radiation |
| **Input/Control** | [[08-control-modes]] | Entity control modes (Direct, Command, Autonomous) |
| **World** | [[06-chunk-integration]], [[07-warp-system]] | Spatial management, high-speed travel |
| **Combat** | [[09-progressive-destruction]] | Localized damage, module degradation |

---

## Data Flow Pipeline

```mermaid
flowchart LR
    subgraph Config["Configuration"]
        JSON[JSON Files]
        BLOB[BlobAssets]
    end

    subgraph Spawn["Entity Creation"]
        PROC[Procedural Gen]
        ARCH[Archetype Factory]
        ENT[ECS Entity]
    end

    subgraph Sim["Simulation"]
        TIER[Tier Assignment]
        SYS[System Processing]
        AI[AI Decisions]
    end

    subgraph Present["Presentation"]
        VIEW[View Manager]
        GO[GameObjects]
        FX[Visual Effects]
    end

    JSON -->|Load| BLOB
    BLOB -->|Reference| ARCH
    PROC -->|Request| ARCH
    ARCH -->|Create| ENT
    ENT -->|Distance Check| TIER
    TIER -->|Enable Tags| SYS
    SYS -->|Control Input| AI
    AI -->|Control Input| SYS
    SYS -->|Tier 0| VIEW
    VIEW -->|Sync| GO
    GO --> FX
```

---

## System Execution Order

```mermaid
flowchart TB
    subgraph INIT["Initialization (Startup)"]
        CLS[ConfigLoadSystem]
        ESS[EntitySpawnSystem]
        TAS[TierAssignSystem]
    end

    subgraph INPUT["Input Group"]
        PIS[PlayerInputSystem]
        AIS[AIInputSystem]
        DSS[DriverStackSystem]
    end

    subgraph SIM["Simulation Group"]
        MVS[MovementSystem]
        RTS[RotationSystem]
        PHS[PhysicsIntegration]
    end

    subgraph ENV["Environment Group"]
        SOUS[StarOrbitUpdateSystem]
        BCS[BarycenterCalcSystem]
        GSS[GravitySimulatorSystem]
        HSS[HeatSimulatorSystem]
    end

    subgraph PHYSICS["Physics Group"]
        BPC[BroadPhaseCollision]
        NPC[NarrowPhaseCollision]
        CRS[CollisionResponse]
    end

    subgraph COMBAT["Combat Group"]
        WPS[WeaponSystem]
        PJS[ProjectileSystem]
        DLS[DamageLocalizationSystem]
        MDS[ModuleDamageSystem]
        HDS[HullDamageSystem]
        MRS[ModuleRepairSystem]
        ATPS[AITargetPrioritySystem]
        QTS[QuestTrackingSystem]
        DECS[DamageEventCleanupSystem]
        DTS[DestructionSystem]
    end

    subgraph TIERMGMT["Tier Management"]
        DCS[DistanceCalcSystem]
        TTS[TierTransitionSystem]
        CMS[ChunkMigrationSystem]
    end

    subgraph BG["Background Simulation"]
        T2S[Tier2TacticalSystem]
        T3S[Tier3StrategicSystem]
        FLS[FleetSystem]
    end

    subgraph RENDER["Presentation"]
        VMS[ViewManagerSystem]
        TRS[TransformSyncSystem]
        VES[VisualEffectsSystem]
    end

    INIT --> INPUT
    INPUT --> SIM
    SIM --> ENV
    ENV --> PHYSICS
    PHYSICS --> COMBAT
    COMBAT --> TIERMGMT
    TIERMGMT --> BG
    BG --> RENDER
```

**Performance Budgets:**

| Group | Target | Notes |
|-------|--------|-------|
| Input | 0.5ms | Minimal, reading only |
| Simulation | 2ms | Tier 0-1 entities |
| Environment | 1ms | Gravity + Heat simulation |
| Physics | 3ms | Burst spatial hash |
| Combat | 1ms | Projectile-heavy scenarios |
| Tier Management | 0.5ms | Distance checks |
| Background | 1ms | Amortized |
| Presentation | 2ms | Transform sync |
| **Total** | **11ms** | 5ms headroom for 60 FPS |

---

## 5-Tier Simulation System

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Tier0: Spawned in view

    Tier0: Tier 0 - LOADED
    Tier0: 0-5k units
    Tier0: Full GameObject + Unity Physics

    Tier1: Tier 1 - ACTIVE
    Tier1: 5k-20k units
    Tier1: Full DOTS + BT

    Tier2: Tier 2 - TACTICAL
    Tier2: 20k-100k units
    Tier2: State Machine AI

    Tier3: Tier 3 - STRATEGIC
    Tier3: 100k-200k units
    Tier3: Fleet Abstraction

    Tier4: Tier 4 - DORMANT
    Tier4: 200k+ units
    Tier4: Existence Only

    Tier0 --> Tier1: Exits camera view
    Tier1 --> Tier0: Enters camera view

    Tier1 --> Tier2: > 20k units
    Tier2 --> Tier1: ≤ 17k units

    Tier2 --> Tier3: > 100k (Transient)
    Tier3 --> Tier2: ≤ 85k (Unpack)

    Tier3 --> Tier4: > 200k (Transient)
    Tier4 --> Tier3: ≤ 170k

    note right of Tier2: Critical entities stay here minimum
    note right of Tier4: Critical entities NEVER reach here
```

**Persistence Behavior:**

| Persistence | Tier 0-1 | Tier 2 | Tier 3 | Tier 4 |
|-------------|----------|--------|--------|--------|
| **Transient** | Full sim | State machine | Fleet grouped | Can despawn |
| **Persistent** | Full sim | State machine | Individual, slow | Tracked, no sim |
| **Critical** | Full sim | Full BT (reduced) | **Never** | **Never** |

---

## System Boundaries

```mermaid
graph TB
    subgraph Singletons["Global Singletons"]
        WO[WorldOrigin]
        PP[PlayerPosition]
        GW[GlobalWarpState]
        SC[SpawnControl]
        FR[FactionRelationshipMatrix]
        QS[QualitySettings]
    end

    subgraph InputBoundary["INPUT BOUNDARY"]
        direction LR
        UIS[Unity Input System]
        CTRL[Control Modes]
        BTS[Behavior Trees]
        DSS[Driver Stack]
    end

    subgraph SimBoundary["SIMULATION BOUNDARY"]
        direction LR
        MOV[Movement]
        ROT[Rotation]
        PHY[Physics Integration]
    end

    subgraph CollBoundary["COLLISION BOUNDARY"]
        direction LR
        HASH[Spatial Hash]
        DET[Detection]
        RES[Response]
    end

    subgraph TierBoundary["TIER BOUNDARY"]
        direction LR
        DIST[Distance Calc]
        TRANS[Tier Transition]
        FLEET[Fleet Group/Unpack]
    end

    subgraph WorldBoundary["WORLD BOUNDARY"]
        direction LR
        CHUNK[Chunk Load/Unload]
        ORIGIN[Floating Origin]
        WARP[Warp Mode]
    end

    subgraph ViewBoundary["VIEW BOUNDARY"]
        direction LR
        POOL[Object Pool]
        SYNC[Transform Sync]
        FX[Effects]
    end

    WO --> SimBoundary
    WO --> ViewBoundary
    PP --> TierBoundary
    PP --> WorldBoundary
    GW --> WorldBoundary
    GW --> TierBoundary
    FR --> InputBoundary

    InputBoundary --> SimBoundary
    SimBoundary --> CollBoundary
    CollBoundary --> TierBoundary
    TierBoundary --> WorldBoundary
    TierBoundary --> ViewBoundary
```

---

## Chunk ↔ ECS Integration

```mermaid
sequenceDiagram
    participant Player
    participant ChunkMgr as ChunkManager
    participant Bridge as ChunkECSBridge
    participant ECS as EntityManager
    participant View as ViewManager

    Note over Player: Player moves to new area

    Player->>ChunkMgr: Position update
    ChunkMgr->>ChunkMgr: Calculate target chunks

    rect rgb(200, 255, 200)
        Note over ChunkMgr,ECS: CHUNK LOADING
        ChunkMgr->>Bridge: LoadChunk(coord)
        Bridge->>Bridge: Procedural generation
        Bridge->>ECS: Create entities
        ECS->>ECS: Assign initial tier
    end

    rect rgb(255, 200, 200)
        Note over ChunkMgr,ECS: CHUNK UNLOADING
        ChunkMgr->>Bridge: UnloadChunk(coord)
        Bridge->>ECS: Query chunk entities
        alt Modified entities
            Bridge->>Bridge: Save to persistent storage
        end
        Bridge->>ECS: Demote/Destroy
    end

    rect rgb(200, 200, 255)
        Note over ECS,View: VIEW SYNC (Tier 0 only)
        ECS->>View: LoadedTag enabled
        View->>View: Get from pool
        View->>View: Sync transforms
    end
```

---

## Warp Mode State Changes

```mermaid
flowchart TB
    subgraph Normal["NORMAL MODE"]
        N_SPAWN[Full entity spawning]
        N_COLL[Frame-by-frame collision]
        N_TIER[Distance-based tiers]
        N_AI[Full AI simulation]
    end

    subgraph Warp["WARP MODE"]
        W_SPAWN[Spawning SUSPENDED]
        W_COLL[Swept raycast only]
        W_TIER[Corridor → Tier 4]
        W_AI[AI frozen in corridor]
    end

    Normal -->|"WarpPhase.Charging"| Warp
    Warp -->|"WarpPhase.None"| Normal

    subgraph Hazards["Warp Hazards Only"]
        STA[Stations]
        PLANET[Planets]
        GRAV[Gravity Wells]
        INTER[Interdiction Fields]
    end

    W_COLL --> Hazards
```

---

## Component → Archetype Mapping

```mermaid
graph LR
    subgraph Core["Core Components"]
        POS[AbsolutePosition]
        VEL[Velocity]
        ROT[Rotation]
        PHY[PhysicsBody]
        CHUNK[ChunkLocation]
    end

    subgraph Ship["Ship Components"]
        PROP[PropulsionModule]
        ROTM[RotationModule]
        SHIELD[ShieldModule]
        HULL[HullModule]
        SENS[SensorModule]
        TRANS[TransponderModule]
        WEAP[WeaponModule Buffer]
        CTRL[ControlInput]
    end

    subgraph AI["AI Components"]
        AICORE[AICoreModule]
        BTTYPE[BehaviorTreeType]
        BTSTATE[BehaviorTreeState]
        SMSTATE[StateMachineState]
    end

    subgraph Fleet["Fleet Components"]
        FDATA[FleetData]
        FMEMBER[FleetMember Buffer]
        FSHIP[FleetMembership]
    end

    Core --> PlayerShip
    Core --> AIShip
    Core --> Asteroid
    Core --> Projectile
    Core --> Station

    Ship --> PlayerShip
    Ship --> AIShip
    Ship --> Station

    AI --> AIShip
    Fleet --> FleetEntity
    FSHIP --> AIShip
```

---

## Document Cross-Reference Map

```mermaid
graph TB
    subgraph Documents
        DOC01[01-component-model<br/>Components & Tags]
        DOC02[02-system-architecture<br/>System Execution]
        DOC03[03-tiered-simulation<br/>5-Tier System]
        DOC04[04-archetype-strategy<br/>Entity Archetypes]
        DOC05[05-configuration-layer<br/>JSON & BlobAssets]
        DOC06[06-chunk-integration<br/>World Chunks]
        DOC07[07-warp-system<br/>High-Speed Travel]
        DOC08[08-control-modes<br/>Entity Control]
        DOC09[09-progressive-destruction<br/>Localized Damage]
        DOC10[10-gravity-system<br/>Orbital Mechanics]
        DOC11[11-heat-system<br/>Thermal Radiation]
    end

    DOC01 --> DOC02
    DOC01 --> DOC03
    DOC01 --> DOC04

    DOC02 --> DOC03
    DOC02 --> DOC07

    DOC03 --> DOC06

    DOC04 --> DOC05

    DOC05 --> DOC06

    DOC06 --> DOC07

    DOC08 --> DOC01
    DOC08 --> DOC02
    DOC08 --> DOC03
    DOC08 --> DOC04

    DOC09 --> DOC01
    DOC09 --> DOC02
    DOC09 --> DOC03
    DOC09 --> DOC04
    DOC09 --> DOC05

    DOC10 --> DOC01
    DOC10 --> DOC02
    DOC10 --> DOC03
    DOC10 --> DOC11

    DOC11 --> DOC01
    DOC11 --> DOC02
    DOC11 --> DOC09
    DOC11 --> DOC10

    style DOC01 fill:#e1f5fe
    style DOC02 fill:#fff3e0
    style DOC03 fill:#f3e5f5
    style DOC04 fill:#e8f5e9
    style DOC05 fill:#fce4ec
    style DOC06 fill:#fff8e1
    style DOC07 fill:#e0f2f1
    style DOC08 fill:#e3f2fd
    style DOC09 fill:#ffebee
    style DOC10 fill:#fef3c7
    style DOC11 fill:#fee2e2
```

**Reading Order for New Developers:**
1. [[01-component-model]] - Understand the data
2. [[04-archetype-strategy]] - How components compose
3. [[02-system-architecture]] - How systems process data
4. [[08-control-modes]] - Entity control (Direct, Command, Autonomous)
5. [[03-tiered-simulation]] - Distance-based simulation
6. [[10-gravity-system]] - Newtonian orbital mechanics
7. [[11-heat-system]] - Thermal radiation and damage
8. [[09-progressive-destruction]] - Localized damage and module degradation
9. [[05-configuration-layer]] - How entities are configured
10. [[06-chunk-integration]] - World management
11. [[07-warp-system]] - Special high-speed mode

---

## Key Singletons

| Singleton | Purpose | Updated By |
|-----------|---------|------------|
| `WorldOrigin` | Floating origin offset | FloatingOriginSystem |
| `PlayerPosition` | Cached player position | PlayerMovementSystem |
| `GlobalWarpState` | Warp mode flags | WarpModeSystem |
| `SpawnControl` | Enable/disable spawning | WarpModeSystem |
| `FactionRelationshipMatrix` | Faction friend/foe | ConfigLoadSystem |
| `SimulationSettings` | Quality settings | QualityManager |
| `ConfigRegistry` | BlobAsset references | ConfigLoadSystem |
| `PrefabEntityRegistry` | Entity prefabs | InitializationSystem |

---

## Key Events

| Event | Fired When | Consumed By |
|-------|------------|-------------|
| `ChunkBoundaryEvent` | Entity crosses chunk | ChunkMigrationSystem |
| `WarpEngagedEvent` | Player starts warp | UI, AudioSystem |
| `WarpDropEvent` | Player exits warp | UI, TierSystem |
| `WarpHazardWarningEvent` | Hazard ahead in warp | UI |
| `PendingDestructionTag` | Entity hull ≤ 0 | DestructionSystem |
| `ModuleDamagedEvent` | Module state changes | DamageEffectsSystem, AI |
| `ModuleDisabledEvent` | Module becomes Disabled | AI, QuestSystem |

---

## Entity Lifecycle Summary

```mermaid
flowchart TB
    subgraph Create["CREATION"]
        PROC[Procedural Gen]
        SAVE[Load from Save]
        SPAWN[Runtime Spawn]
    end

    subgraph Live["SIMULATION"]
        T0[Tier 0 - Rendered]
        T1[Tier 1 - Full DOTS]
        T2[Tier 2 - Simplified]
        T3[Tier 3 - Abstracted]
        T4[Tier 4 - Dormant]
    end

    subgraph End["DESTRUCTION"]
        DESTROY[Combat Death]
        UNLOAD[Chunk Unload]
        DEGRADE[Tier Degrade]
    end

    Create --> Live
    Live --> End

    T0 <--> T1
    T1 <--> T2
    T2 <--> T3
    T3 <--> T4

    UNLOAD -->|Transient| DEGRADE
    UNLOAD -->|Modified| SAVE
```

---

## Quick Reference

### Tier Boundaries (Default)

| Tier | Range | Hysteresis | Demote At |
|------|-------|------------|-----------|
| 0 | 0 - 5k | 750 | 4,250 |
| 1 | 5k - 20k | 3k | 17,000 |
| 2 | 20k - 100k | 15k | 85,000 |
| 3 | 100k - 200k | 30k | 170,000 |
| 4 | 200k+ | - | - |

### Entity Counts (Target)

| Tier | Max Entities | Cost/Entity |
|------|--------------|-------------|
| 0 | ~20 | ~0.5ms |
| 1 | ~500 | ~0.01ms |
| 2 | ~2,000 | ~0.002ms |
| 3 | ~100 fleets | ~0.0005ms |
| 4 | Unlimited | ~0 |

### Warp Speeds

| Engine Class | Max Speed | Charge Time |
|--------------|-----------|-------------|
| Basic | 5,000 u/s | 3s |
| Military | 10,000 u/s | 2s |
| Advanced | 20,000 u/s | 1.5s |

---

## Related Documents

- [[01-component-model]] - ECS component definitions
- [[02-system-architecture]] - System execution order
- [[03-tiered-simulation]] - 5-tier progressive simulation
- [[04-archetype-strategy]] - Entity archetype patterns
- [[05-configuration-layer]] - JSON configuration pipeline
- [[06-chunk-integration]] - World chunk management
- [[07-warp-system]] - High-speed warp travel
- [[08-control-modes]] - Entity control modes (Direct, Command, Autonomous)
- [[09-progressive-destruction]] - Localized damage and module degradation
- [[10-gravity-system]] - Newtonian orbital mechanics and N-body approximation
- [[11-heat-system]] - Thermal radiation and hull temperature damage

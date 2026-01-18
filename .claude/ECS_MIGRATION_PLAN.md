# ECS Migration Plan: Entity/Ship/Module System

## Overview
Migrate from OOP-based Entity system to Unity DOTS ECS with Burst/Jobs support while maintaining modularity for dynamic ship modules and behavior tree integration.

## Key Decisions
- **Physics**: Hybrid - keep Rigidbody2D, sync forces from ECS
- **Priority**: Phase 1 (Perception/Sensors) first for biggest performance gains
- **Behavior Trees**: Hybrid - managed code with ECS data bridge

## Architecture Approach
- **Core simulation** (sensors, steering, combat) → Pure ECS with Burst/Jobs
- **Physics** → Hybrid bridge to Rigidbody2D
- **Behavior Trees** → Managed code reading/writing ECS components
- **Modules** → Child entities with type-specific components

---

## Phase 0: Foundation Setup
**Goal:** ECS infrastructure without breaking existing game

### Tasks
- [ ] Add Unity DOTS packages (com.unity.entities, com.unity.burst, com.unity.collections, com.unity.mathematics)
- [ ] Create custom bootstrap for Starfire ECS world
- [ ] Define all component structs (see Component Definitions below)
- [ ] Create EntityConverter to spawn shadow ECS entities from existing MonoBehaviours
- [ ] Add EntityLink component to link GameObject ↔ ECS Entity

### Files to Create
```
Assets/ECS/
├── Bootstrap/StarfireWorldBootstrap.cs
├── Components/
│   ├── EntityComponents.cs      (ShipTag, EntityIdentity, PhysicsState, ForceAccumulator)
│   ├── DriverComponents.cs      (DriverInput, PlayerControlled, AIControlled)
│   ├── ModuleComponents.cs      (ModuleSlotElement, PropulsionModule, WeaponModule, etc.)
│   └── AIComponents.cs          (HeuristicState, GoalState, SteeringState, Blackboard buffers)
├── Conversion/EntityConverter.cs
└── Systems/                     (empty initially)
```

---

## Phase 1: Perception Layer (Burst-enabled) - PRIORITY
**Goal:** Migrate sensor scanning and heuristics to ECS

### Tasks
- [ ] Create SensorScanSystem (queries entities with SensorModule, spatial queries, writes DetectedContactElement buffer)
- [ ] Create DetectionUpdateSystem (updates DetectionLevel based on range)
- [ ] Create HeuristicCalculationSystem (reads modules/contacts, writes HeuristicState)
- [ ] Create HeuristicSyncToOOPSystem (writes HeuristicState back to BTContext blackboard for existing BT)

### Key Benefit
Sensor scanning is O(n^2) - massive gains from Burst/Jobs parallelization

### Critical Files to Modify
- `Assets/entity/ai/bt/BTContext.cs` - Add method to receive ECS heuristics
- `Assets/entity/ai/heuristics/HeuristicData.cs` - Already a struct, maps to HeuristicState component

---

## Phase 2: Steering & Movement (Burst-enabled)
**Goal:** Migrate steering calculations to ECS

### Tasks
- [ ] Create WaypointNavigationSystem (manages waypoint progression, writes target position)
- [ ] Create SteeringCalculationSystem (implements Seek, Arrive, Flee - Burst compiled)
- [ ] Create ForceAggregatorSystem (combines steering forces)
- [ ] Create RotationApplySystem (Burst compiled)
- [ ] Create MovementApplySystem (Burst compiled)
- [ ] Create PhysicsSyncSystem (hybrid bridge to Rigidbody2D - NOT Burst)
- [ ] Update BT steering actions to write to ECS SteeringState

### Critical Files to Modify
- `Assets/entity/controller/ShipController.cs` - Read forces from ECS
- `Assets/entity/ai/bt/actions/steering/ApplySteeringAction.cs` - Write to ECS instead of AIDriver

---

## Phase 3: Combat Systems (Burst-enabled)
**Goal:** Migrate weapon and damage systems

### Tasks
- [ ] Create WeaponAimSystem (updates aim directions - Burst)
- [ ] Create WeaponCooldownSystem (decrements timers - Burst)
- [ ] Create WeaponFireSystem (processes FireWeaponRequest, spawns projectiles via ECB)
- [ ] Create ProjectileMovementSystem (pure ECS projectiles - Burst + Jobs)
- [ ] Create DamageSystem (processes damage requests - Burst)
- [ ] Create ShieldAbsorptionSystem (Burst)

### Critical Files to Modify
- `Assets/entity/modules/categories/weapons/` - Read aim from ECS

---

## Phase 4: Goal System (Full ECS)
**Goal:** Migrate goal evaluation to pure ECS

### Tasks
- [ ] Create GoalEvaluationSystem (utility scoring - fully Burst compatible)
- [ ] Create GoalTransitionSystem (handles goal switches)
- [ ] Create GoalParameterElement buffer (replaces GoalParameters ScriptableObjects)
- [ ] Update IsCurrentGoalCondition to read from GoalState component

### Critical Files to Modify
- `Assets/entity/ai/goals/manager/GoalManager.cs` - Port evaluation logic
- `Assets/entity/ai/bt/actions/goals/EvaluateGoalsAction.cs` - Trigger ECS evaluation

---

## Phase 5: Module System
**Goal:** Migrate module management to ECS child entities

### Tasks
- [ ] Create ModuleEntityFactory (creates child entities per module type)
- [ ] Create ModuleStatsAggregationSystem (caches PropulsionStats, etc. on parent ship)
- [ ] Create ShieldRegenSystem (Burst)
- [ ] Create module attachment/detachment API using ECB

### Module Strategy: Child Entities
```
Ship Entity
  └── ModuleSlotElement buffer (references to children)

Module Entity (Parent = Ship)
  ├── ModuleData component
  └── Type-specific component (WeaponModule, ShieldModule, etc.)
```

### Critical Files to Modify
- `Assets/entity/ship/ShipSystems.cs` - Adapt slot pattern for ECS

---

## Phase 6: BT Bridge
**Goal:** Optimize BT execution with ECS data

### Tasks
- [ ] Create BTContextECS wrapper class (typed blackboard using ECS buffers)
- [ ] Create BTExecutionSystem (managed, batches BT execution across entities)
- [ ] Migrate high-frequency actions to pure ECS systems where possible
- [ ] Profile and optimize remaining bottlenecks

### Blackboard Strategy: Typed Buffers
```csharp
DynamicBuffer<BlackboardFloat>   // keyed by hash
DynamicBuffer<BlackboardFloat2>
DynamicBuffer<BlackboardEntity>
DynamicBuffer<BlackboardInt>
```

---

## Component Definitions

### Core Ship Components
```csharp
// Tag component for ships (zero-size)
public struct ShipTag : IComponentData { }

// Core entity identification
public struct EntityIdentity : IComponentData
{
    public int EntityId;
    public EntityCapabilityFlags Capabilities;  // Bitmask
    public FixedString64Bytes DisplayName;
}

[Flags]
public enum EntityCapabilityFlags : uint
{
    None = 0,
    Propulsion = 1 << 0,
    Weapons = 1 << 1,
    Shields = 1 << 2,
    Sensors = 1 << 3,
    Warp = 1 << 4,
    Hyperdrive = 1 << 5,
    AIControlled = 1 << 6,
    PlayerControlled = 1 << 7
}

// Physics state (mirrors Rigidbody2D for ECS)
public struct PhysicsState : IComponentData
{
    public float2 Position;
    public float2 Velocity;
    public float Rotation;
    public float AngularVelocity;
    public float Mass;
}

// Accumulator for forces (reset each frame)
public struct ForceAccumulator : IComponentData
{
    public float2 LinearForce;
    public float TorqueForce;
}

// Input state from drivers
public struct DriverInput : IComponentData
{
    public float2 MovementDirection;
    public float2 DesiredAcceleration;
    public float2 AimPosition;
    public float Throttle;
    public float RotationInput;
    public bool FirePressed;
    public bool WarpPressed;
    public bool HyperdrivePressed;
    public bool IsWorldSpaceAim;
    public int ActiveDriverPriority;
}
```

### Module Components
```csharp
// Module slot reference in a dynamic buffer
[InternalBufferCapacity(16)]
public struct ModuleSlotElement : IBufferElementData
{
    public FixedString32Bytes SlotId;
    public ModuleTypeId TypeId;
    public Entity ModuleEntity;
    public bool IsEquipped;
}

// Propulsion module (on child entity)
public struct PropulsionModule : IComponentData
{
    public float MaxSpeed;
    public float Acceleration;
    public float Drag;
}

// Cached propulsion stats on ship (aggregated)
public struct PropulsionStats : IComponentData
{
    public float MaxSpeed;
    public float MaxAcceleration;
    public float EffectiveDrag;
}

// Weapon module
public struct WeaponModule : IComponentData
{
    public float Damage;
    public float FireRate;
    public float Range;
    public float CooldownTimer;
    public bool IsTurret;
    public float2 AimDirection;
    public int ProjectileArchetypeIndex;
}

// Shield module
public struct ShieldModule : IComponentData
{
    public int MaxShield;
    public int CurrentShield;
    public float RegenRate;
    public float RechargeDelay;
    public float RechargeDelayTimer;
    public ShieldState State;
}

// Sensor module
public struct SensorModule : IComponentData
{
    public float DetectionRange;
    public float SilhouetteRange;
    public float FullIdentificationRange;
    public float LastScanTime;
    public float ScanInterval;
}

// Detected contacts buffer
[InternalBufferCapacity(32)]
public struct DetectedContactElement : IBufferElementData
{
    public Entity TargetEntity;
    public DetectionLevel Level;
    public float Distance;
    public float2 LastKnownPosition;
    public float LastUpdateTime;
    public bool IsHostile;
}
```

### AI Components
```csharp
// Heuristics (mirrors existing HeuristicData struct)
public struct HeuristicState : IComponentData
{
    // Health/Defense (0-1 normalized)
    public float HullPercent;
    public float ShieldPercent;
    public float OverallDefense;

    // Derived States (0-1)
    public float Confidence;
    public float Skittishness;
    public float Vulnerability;

    // Threat Assessment
    public int NearbyHostileCount;
    public float ClosestThreatDistance;
    public float ThreatLevel;

    // Detection/Awareness
    public int UnidentifiedContactCount;
    public bool HasUnidentifiedContacts;
    public float ClosestUnidentifiedDistance;

    // Situational
    public bool HasTarget;
    public bool IsInCombat;

    // Capabilities
    public float MaxSpeed;
    public float MaxAcceleration;
    public float SensorRange;
    public bool HasWeapons;
    public bool HasShield;
}

// Goal state
public struct GoalState : IComponentData
{
    public GoalType CurrentGoal;
    public GoalType PreviousGoal;
    public float CurrentGoalScore;
    public float LastSwitchTime;
    public bool IsCommanderAssigned;
}

public enum GoalType : byte
{
    Idle = 0,
    Patrol = 1,
    Investigate = 2,
    Combat = 3,
    Flee = 4,
    Follow = 5,
    Dock = 6
}

// Steering state
public struct SteeringState : IComponentData
{
    public float2 TargetPosition;
    public float2 DesiredVelocity;
    public float2 SteeringForce;
    public float DistanceToTarget;
    public bool ShouldBrake;
    public float CruiseSpeed;
    public float CruiseAcceleration;
}

// Waypoint buffer
[InternalBufferCapacity(16)]
public struct WaypointElement : IBufferElementData
{
    public float2 Position;
    public float ArrivalRadius;
    public WaypointFlags Flags;
}

// Typed blackboard buffers (replaces Dictionary<string, object>)
[InternalBufferCapacity(16)]
public struct BlackboardFloat : IBufferElementData
{
    public int KeyHash;
    public float Value;
}

[InternalBufferCapacity(8)]
public struct BlackboardFloat2 : IBufferElementData
{
    public int KeyHash;
    public float2 Value;
}

[InternalBufferCapacity(8)]
public struct BlackboardEntity : IBufferElementData
{
    public int KeyHash;
    public Entity Value;
}
```

---

## System Pipeline (Execution Order)

```
INPUT PHASE
  └─ PlayerInputSystem / SyncFromOOPSystem → DriverInput

PERCEPTION PHASE [Burst + Jobs]
  └─ SensorScanSystem → DetectionUpdateSystem → HeuristicCalculationSystem

AI DECISION PHASE
  └─ GoalEvaluationSystem [Burst] → GoalTransitionSystem → BTExecutionSystem [Managed]

STEERING PHASE [Burst + Jobs]
  └─ WaypointNavigationSystem → SteeringCalculationSystem → ForceAggregatorSystem

PHYSICS PHASE (FixedUpdate) [Burst + Jobs]
  └─ RotationApplySystem → MovementApplySystem → PhysicsSyncSystem [Hybrid]

COMBAT PHASE [Burst + Jobs]
  └─ WeaponAimSystem → WeaponCooldownSystem → WeaponFireSystem → ProjectileSystem → DamageSystem

MODULE PHASE [Burst]
  └─ ShieldRegenSystem → ModuleStatsAggregationSystem → SyncToOOPSystem [Hybrid]
```

### System Groups Definition
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class StarfireInputSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StarfireInputSystemGroup))]
public partial class StarfirePerceptionSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StarfirePerceptionSystemGroup))]
public partial class StarfireAISystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StarfireAISystemGroup))]
public partial class StarfireSteeringSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class StarfirePhysicsSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StarfireSteeringSystemGroup))]
public partial class StarfireCombatSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StarfireCombatSystemGroup))]
public partial class StarfireModuleSystemGroup : ComponentSystemGroup { }
```

---

## Burst Compilation Summary

| System | Burst | Jobs | Notes |
|--------|-------|------|-------|
| PlayerInputSystem | NO | NO | UnityEngine.Input requires main thread |
| SensorScanSystem | YES | YES | Spatial queries, high benefit |
| DetectionUpdateSystem | YES | YES | Simple math |
| HeuristicCalculationSystem | YES | YES | Pure math, high frequency |
| GoalEvaluationSystem | YES | NO | Simple iteration |
| GoalTransitionSystem | PARTIAL | NO | ECB usage |
| BTExecutionSystem | NO | NO | Managed code (polymorphism) |
| WaypointNavigationSystem | YES | YES | Pure math |
| SteeringCalculationSystem | YES | YES | Vector math, high frequency |
| ForceAggregatorSystem | YES | YES | Simple accumulation |
| RotationApplySystem | YES | YES | Pure math |
| MovementApplySystem | YES | YES | Pure math |
| PhysicsSyncSystem | NO | NO | Rigidbody2D access |
| WeaponAimSystem | YES | YES | Vector math |
| WeaponCooldownSystem | YES | YES | Timer decrements |
| WeaponFireSystem | PARTIAL | NO | ECB for projectile spawn |
| ProjectileMovementSystem | YES | YES | High entity count |
| DamageSystem | YES | YES | Pure math |
| ShieldRegenSystem | YES | YES | Simple state machine |
| ModuleStatsAggregationSystem | YES | NO | Buffer iteration |

---

## Verification Plan

### After Each Phase
1. Run existing game - should work identically (hybrid bridge active)
2. Check Unity Profiler for ECS systems executing
3. Verify Burst compilation in Jobs → Burst Inspector
4. Compare frame times before/after

### Performance Targets
- Sensor scanning: 10x+ improvement with 100+ entities
- Steering calculation: 5x+ improvement
- Projectile simulation: 20x+ improvement (pure ECS)

### Integration Tests
- [ ] Ships spawn with ECS shadow entities
- [ ] AI ships navigate waypoints via ECS steering
- [ ] Weapons fire via ECS command pattern
- [ ] Goals switch correctly based on heuristics
- [ ] Modules attach/detach at runtime

---

## Complete Task Checklist

### Phase 0: Foundation
- [ ] Install Unity DOTS packages
- [ ] Create StarfireWorldBootstrap.cs
- [ ] Create EntityComponents.cs
- [ ] Create DriverComponents.cs
- [ ] Create ModuleComponents.cs
- [ ] Create AIComponents.cs
- [ ] Create EntityConverter.cs
- [ ] Add ConvertToECS() to EntityControllerBase

### Phase 1: Perception (PRIORITY)
- [ ] Create SensorScanSystem
- [ ] Create DetectionUpdateSystem
- [ ] Create HeuristicCalculationSystem
- [ ] Create HeuristicSyncToOOPSystem

### Phase 2: Steering
- [ ] Create WaypointNavigationSystem
- [ ] Create SteeringCalculationSystem
- [ ] Create ForceAggregatorSystem
- [ ] Create RotationApplySystem
- [ ] Create MovementApplySystem
- [ ] Create PhysicsSyncSystem

### Phase 3: Combat
- [ ] Create WeaponAimSystem
- [ ] Create WeaponCooldownSystem
- [ ] Create WeaponFireSystem
- [ ] Create ProjectileMovementSystem
- [ ] Create DamageSystem
- [ ] Create ShieldAbsorptionSystem

### Phase 4: Goals
- [ ] Create GoalEvaluationSystem
- [ ] Create GoalTransitionSystem
- [ ] Port goal scoring logic to Burst

### Phase 5: Modules
- [ ] Create ModuleEntityFactory
- [ ] Create ModuleStatsAggregationSystem
- [ ] Create ShieldRegenSystem
- [ ] Create module attach/detach API

### Phase 6: BT Bridge
- [ ] Create BTContextECS
- [ ] Create BTExecutionSystem
- [ ] Migrate high-frequency BT actions
- [ ] Profile and optimize

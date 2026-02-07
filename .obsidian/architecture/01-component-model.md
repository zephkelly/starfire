# Data Model

## Overview

This document defines all data types across the three processing layers. The Rich Entity Layer uses C# classes and interfaces. The Sensor Layer uses lightweight structs. The Mass Entity Layer uses Burst-compatible structs in NativeArrays.

---

## Design Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Position type | `double` | Sub-unit precision at 500k+ units, no jitter |
| Chunk index type | `long` | Future-proofing for massive worlds |
| Ship modules | C# classes implementing `IShipModule` | Virtual dispatch for unique behavior, hot-swap capable |
| Ship abilities | C# classes implementing `IShipAbility` | Polymorphic per-class abilities (cloak, tractor beam, etc.) |
| Sensor contacts | Plain C# struct | Lightweight, no polymorphism needed at distance |
| Mass entities | Burst-compatible structs in NativeArrays | Cache-coherent batch processing |
| Tier tags | Layer membership (not IEnableableComponent) | Tiers map to layer managers, not ECS archetype tags |
| AI (Rich) | `IBehaviorController` interface | Full BT for nearby, pluggable for player/AI |
| AI (Sensor) | `SensorAIState` enum | Simple state machine, observable on map |
| Scripting runtime | Lua via MoonSharp | Pure C# interpreter, sandboxed, battle-tested in game modding |
| Script execution | After tier management, before presentation | Main thread, direct access to Rich layer objects |
| Mod entity types | Reuse existing `EntityTypeEnum` | Mods differentiate via ShipConfigId, not new enum values |
| Per-entity script data | `Dictionary<string, float>` on ShipInstance | Lua scripts store custom state via `get_data()` / `set_data()` |
| Override model | Additive + Override (last writer wins) | Already in ConfigRegistry; extensible to patching later |
| Network replication struct | `NetworkShipState` (~51 bytes) | Compact; delta compressed with dirty flags for ongoing replication |
| Sensor network format | `SensorContactSummary` (~17 bytes) | Server computes per-client, sends at 2-4 Hz |
| Network initial state | Reuse `ShipSnapshot` | Already captures full state for tier transitions |

---

## Shared Types

Types used across multiple layers.

### Position & Transform

```csharp
public struct AbsolutePosition
{
    public double X;
    public double Y;
}

public struct Velocity
{
    public double X;
    public double Y;
}
```

**Rationale:** Single-precision floats lose accuracy beyond ~16km. At 500,000 units, `double` still provides ~10 decimal places of precision.

### Entity Identity

```csharp
public struct EntityId
{
    public int Value;
    public EntityTypeEnum Type;
}

public enum EntityTypeEnum : byte
{
    Unknown = 0,
    Ship = 1,
    Station = 2,
    Projectile = 3,
    Asteroid = 4,
    Debris = 5,
    Missile = 6
}
// Modding note: Mods cannot add new EntityTypeEnum values (byte enum, compile-time).
// Mod-created ships use EntityTypeEnum.Ship with a mod-defined ShipConfigId.
// For truly novel entity categories, use EntityTypeEnum.Unknown + config-driven behavior.
```

### Entity Persistence

```csharp
public enum EntityPersistence : byte
{
    Transient = 0,
    Persistent = 1,
    Critical = 2
}
```

- **Transient:** Generic enemies. Can be grouped into fleets at Tier 3. Can despawn at Tier 4.
- **Persistent:** Named NPCs. Always tracked individually. Slower updates at distance.
- **Critical:** Quest targets, player allies. Never below Tier 2.

### Faction System

```csharp
public struct FactionId
{
    public int Index;
    public FixedString32Bytes Name;
}

public class FactionRelationshipMatrix
{
    public const int MaxFactions = 16;
    private sbyte[] _relations; // MaxFactions * MaxFactions

    public sbyte GetRelation(int factionA, int factionB)
        => _relations[factionA * MaxFactions + factionB];

    public bool IsHostile(int a, int b) => GetRelation(a, b) < -25;
    public bool IsAllied(int a, int b) => GetRelation(a, b) > 25;
}
```

### Chunk & World

```csharp
public struct ChunkCoord
{
    public long X;
    public long Y;
}

public struct WorldOrigin
{
    public double X;
    public double Y;
}
```

### Mod System Types

Data structures for the modding system. See [[12-modding-architecture]] for the full scripting API.

```csharp
public class ModManifest
{
    public string Id;
    public string Name;
    public string Version;
    public string Author;
    public string Description;
    public string[] Dependencies;
    public string[] LoadAfter;
    public string[] LoadBefore;
    public string[] Scripts;
}

public class ModLoadEntry
{
    public int LoadOrder;
    public string ModId;
    public string BasePath;
    public bool IsBaseGame;
}
```

### Ship Snapshot (Layer Transition Currency)

The `ShipSnapshot` captures enough state to reconstruct a ship at any fidelity level. Created when demoting from Rich to Sensor layer, consumed when promoting back.

```csharp
public struct ShipSnapshot
{
    public int ShipConfigId;
    public int FactionIndex;
    public EntityPersistence Persistence;
    public FixedString32Bytes UniqueId;

    // Module states (compressed)
    public float HullPercent;
    public float ShieldPercent;
    public float PropulsionEfficiency;
    public float RotationEfficiency;
    public float SensorEfficiency;
    public bool ShieldsActive;
    public bool TransponderActive;

    // Weapon states
    public FixedList128Bytes<WeaponSnapshotEntry> WeaponStates;

    // Ability states (serialized per-ability)
    public FixedList512Bytes<AbilitySnapshotEntry> AbilityStates;

    // AI state
    public SensorAIState AIState;
    public int TargetEntityId;
    public double2 Waypoint;

    // Physics
    public double2 Position;
    public double2 Velocity;
    public float Heading;
}

public struct WeaponSnapshotEntry
{
    public int SlotIndex;
    public float CooldownRemaining;
    public bool IsEnabled;
}

public struct AbilitySnapshotEntry
{
    public int AbilityId;
    public float CooldownRemaining;
    public byte StateData;
}
```

### Network State Types

Data structures used for network replication. See [[13-networking-architecture]] for full networking detail.

**NetworkShipState** — Compact replication format for Rich layer entities (~51 bytes):

```csharp
public struct NetworkShipState
{
    public double2 Position;           // 16 bytes
    public double2 Velocity;           // 16 bytes
    public float Heading;              // 4 bytes
    public float AngularVelocity;      // 4 bytes
    public half HullPercent;           // 2 bytes
    public half ShieldPercent;         // 2 bytes
    public byte Flags;                 // 1 byte (shields active, transponder, in warp)
    public byte ModuleDamageStates;    // 1 byte (packed 4×2-bit states)
    public WarpPhase WarpPhase;        // 1 byte
    public float WarpSpeed;            // 4 bytes (only when warping)
}
```

**ShipStateDelta** — For ongoing replication, only changed fields are sent:

```csharp
public struct ShipStateDelta
{
    public byte DirtyFlags;
    // Only include fields where corresponding flag is set
    // Most ticks: Position only (~16 bytes)
    // In combat: more fields change (~30-40 bytes)
}
```

**SensorContactSummary** — Server sends per-client sensor results at 2-4 Hz:

```csharp
public struct SensorContactSummary
{
    public int EntityId;                // 4 bytes
    public half2 RelativePosition;      // 4 bytes (relative to client, half precision)
    public half Speed;                  // 2 bytes
    public half Heading;                // 2 bytes
    public byte FactionIndex;           // 1 byte
    public DetectionLevel Level;        // 1 byte
    public SensorAIState AIState;       // 1 byte (FullRead only)
    public half HullPercent;            // 2 bytes (Identified+)
}
```

`ShipSnapshot` (defined above) is reused for: initial spawn replication, reconnection recovery, and observer-gain events (when a new entity enters a client's view).

---

## Rich Entity Layer (Tier 0-1)

Ships, stations, and named NPCs that are close enough for full simulation. Uses C# classes and interfaces for polymorphic behavior.

### Core Ship Data

```csharp
public class ShipInstance
{
    // Identity
    public EntityId Id;
    public int ShipConfigId;
    public FactionId Faction;
    public EntityPersistence Persistence;
    public FixedString32Bytes UniqueId;

    // Transform (source of truth)
    public AbsolutePosition Position;
    public Velocity Velocity;
    public float Rotation;          // Degrees 0-360
    public float AngularVelocity;   // Degrees per second

    // Physics
    public float Mass;
    public float Radius;
    public float Drag;
    public float Restitution;

    // Core modules (always present)
    public HullModule Hull;
    public ShieldModule Shield;
    public PropulsionModule Propulsion;
    public RotationModule RotationMod;
    public SensorModule Sensor;
    public TransponderModule Transponder;

    // Variable modules
    public List<WeaponModule> Weapons;

    // Unique abilities (vary by ship class)
    public List<IShipAbility> Abilities;

    // Behavior controller (null for player, BT for AI)
    public IBehaviorController BehaviorController;

    // Damage system
    public DamageModel DamageModel;

    // Physics state
    public GravityState GravityState;
    public HeatState HeatState;

    // Tier state
    public int CurrentTier;
    public ChunkCoord Chunk;

    // Warp (if equipped)
    public WarpState WarpState;

    // Mod scripting: per-entity data bag for Lua scripts
    public Dictionary<string, float> ScriptData;

    // Layer transition
    public ShipSnapshot CreateSnapshot();
    public void RestoreFromSnapshot(ShipSnapshot snapshot, ShipConfig config);
}
```

### Module System

Each module type is a C# class implementing a common interface. Modules have health, efficiency, and can be damaged/disabled.

```csharp
public interface IShipModule
{
    ShipModuleCategory Category { get; }
    string ModuleId { get; }
    float MaxHealth { get; }
    float CurrentHealth { get; }
    float HealthPercent { get; }
    ModuleDamageState DamageState { get; }
    float EfficiencyMultiplier { get; }
    bool IsEnabled { get; set; }

    void Update(float deltaTime);
    void ApplyDamage(float amount);
    void Repair(float amount);
}

public enum ShipModuleCategory : byte
{
    Structure = 0,
    Defense = 1,
    Propulsion = 2,
    Rotation = 3,
    Offense = 4,
    Sensor = 5,
    Comms = 6
}

public enum ModuleDamageState : byte
{
    Operational = 0,    // 75-100% health, 100% efficiency
    Damaged = 1,        // 50-74% health, 75% efficiency
    Critical = 2,       // 25-49% health, 50% efficiency
    Disabled = 3        // 0-24% health, 0% efficiency
}
```

### Module Implementations

```csharp
public class HullModule : IShipModule
{
    public ShipModuleCategory Category => ShipModuleCategory.Structure;
    public string ModuleId { get; set; }
    public float MaxIntegrity;
    public float CurrentIntegrity;
    public float Armor;            // Damage reduction percentage
    // IShipModule implementation...
}

public class ShieldModule : IShipModule
{
    public ShipModuleCategory Category => ShipModuleCategory.Defense;
    public string ModuleId { get; set; }
    public float MaxCapacity;
    public float CurrentCapacity;
    public float RegenRate;
    public float RegenDelay;
    public float TimeSinceLastDamage;
    // IShipModule implementation + shield-specific Update logic
}

public class PropulsionModule : IShipModule
{
    public ShipModuleCategory Category => ShipModuleCategory.Propulsion;
    public string ModuleId { get; set; }
    public float MaxSpeed;
    public float Acceleration;
    public float Drag;
    // IShipModule implementation...
}

public class RotationModule : IShipModule
{
    public ShipModuleCategory Category => ShipModuleCategory.Rotation;
    public string ModuleId { get; set; }
    public RotationModeEnum Mode;
    public float TurnRate;
    public float ThrusterTorque;
    public RotationStateEnum State;
    public float CurrentAngularAccel;
    // IShipModule implementation + rotation mode state machine
}

public class SensorModule : IShipModule
{
    public ShipModuleCategory Category => ShipModuleCategory.Sensor;
    public string ModuleId { get; set; }
    public float Range;
    public float RefreshRate;
    public float LastScanTime;
    // IShipModule implementation...
}

public class TransponderModule : IShipModule
{
    public ShipModuleCategory Category => ShipModuleCategory.Comms;
    public string ModuleId { get; set; }
    public bool IsActive;
    public FixedString32Bytes CallSign;
    // IShipModule implementation...
}

public class WeaponModule : IShipModule
{
    public ShipModuleCategory Category => ShipModuleCategory.Offense;
    public string ModuleId { get; set; }
    public int SlotIndex;
    public int HardpointIndex;
    public float Damage;
    public float FireRate;
    public float Range;
    public float CooldownRemaining;
    public byte WeightClass;
    public ProjectileTypeEnum ProjectileType;
    // IShipModule implementation + weapon-specific Update (cooldown)
}
```

### Rotation Modes

```csharp
public enum RotationModeEnum : byte
{
    Instant = 0,
    Smooth = 1,
    Physics = 2,
    ThrusterBased = 3
}

public enum RotationStateEnum : byte
{
    Idle = 0,
    Accelerate = 1,
    Coast = 2,
    Brake = 3,
    Settling = 4
}
```

### Ability System

Unique abilities per ship class. Each ability is a self-contained behavior.

```csharp
public interface IShipAbility
{
    int AbilityId { get; }
    string DisplayName { get; }
    bool CanActivate(ShipInstance ship);
    void Activate(ShipInstance ship);
    void Update(ShipInstance ship, float deltaTime);
    void Deactivate(ShipInstance ship);
    float CooldownRemaining { get; }
    float CooldownDuration { get; }
    bool IsActive { get; }

    AbilitySnapshotEntry CreateSnapshot();
    void RestoreFromSnapshot(AbilitySnapshotEntry snapshot);
}

// Examples of unique per-class abilities:
// CloakAbility        - Ship becomes invisible to sensors
// TractorBeamAbility  - Pull/push objects
// MineDeployerAbility - Drop explosive mines
// HackingAbility      - Disable enemy modules remotely
// ShieldBoostAbility  - Temporary shield overcharge
// WarpJammerAbility   - Prevent nearby ships from warping
```

### AI / Behavior Controller

```csharp
public interface IBehaviorController
{
    ControlInput Evaluate(ShipInstance ship, SimulationContext context);
    void OnTierChanged(int newTier);
}

public struct ControlInput
{
    public float2 MovementDirection;
    public float Throttle;
    public float2 AimDirection;
    public bool FirePressed;
    public bool WarpPressed;
    public byte DriverType;     // 0=None, 1=Player, 2=AI
}
```

### Damage Model

```csharp
public class DamageModel
{
    public List<HitboxZone> HitboxZones;
    public List<ZoneModuleMapping> ZoneMappings;
    public RepairConfiguration RepairConfig;
    public ModuleDamageState WorstModuleState;
    public int DisabledModuleCount;

    public void ProcessDamage(PendingDamage damage, ShipInstance ship);
    public void UpdateRepair(float deltaTime, ShipInstance ship);
}

public struct HitboxZone
{
    public HitboxZoneType ZoneType;
    public float2 LocalCenter;
    public float2 LocalExtents;
    public float DamageAbsorption;
}

public struct ZoneModuleMapping
{
    public HitboxZoneType Zone;
    public ShipModuleCategory TargetCategory;
    public int TargetSlotIndex;
    public float DamageWeight;
}

public struct PendingDamage
{
    public float Amount;
    public DamageTypeEnum DamageType;
    public double2 ImpactPosition;
    public float2 LocalImpactPosition;
    public HitboxZoneType HitZone;
    public int SourceEntityId;
}

public struct RepairConfiguration
{
    public float RepairDelayAfterDamage;
    public float BaseRepairRatePerSecond;
    public bool CanAutoRepairDisabled;
    public bool RequiresOutOfCombat;
}

public enum HitboxZoneType : byte
{
    Center = 0,
    Fore = 1,
    Aft = 2,
    Port = 3,
    Starboard = 4,
    Dorsal = 5,
    Ventral = 6,
    Upper = 10,
    Lower = 11,
    Ring = 12
}

public enum DamageTypeEnum : byte
{
    Kinetic = 0,
    Energy = 1,
    Explosive = 2,
    Collision = 3
}
```

### Gravity & Heat State

```csharp
public struct GravityState
{
    public int PrimarySourceId;
    public int SecondarySourceId;
    public double SemiMajorAxis;
    public double Eccentricity;
    public double ArgumentOfPeriapsis;
    public double MeanAnomalyAtEpoch;
    public double EpochTime;
    public bool IsOrbiting;
    public bool IsEscaping;
}

public struct HeatState
{
    public float CurrentTemperature;
    public float LastHeatDamageTime;
    public float AccumulatedHeatDamage;
    public bool IsAtEquilibrium;
}

public struct HullThermalProperties
{
    public float MaxTemperature;
    public float Mass;
    public float ThermalResistance;
}
```

### Warp State

```csharp
public struct WarpState
{
    public WarpPhase Phase;
    public double WarpStartTime;
    public double2 WarpDirection;
    public float CurrentSpeed;
    public float TargetSpeed;
    public float ChargeProgress;
    public double DistanceTraveled;
    public WarpDropReason LastDropReason;
}

public enum WarpPhase : byte
{
    None = 0,
    Charging = 1,
    Active = 2,
    Decelerating = 3,
    Cooldown = 4
}
```

### Station Data

```csharp
public class StationInstance
{
    public EntityId Id;
    public FactionId Faction;
    public AbsolutePosition Position;
    public ChunkCoord Chunk;

    public ShieldModule Shield;
    public HullModule Hull;
    public SensorModule Sensor;
    public TransponderModule Transponder;
    public List<WeaponModule> Weapons;
    public DamageModel DamageModel;

    public StationType Type;
    public FixedString64Bytes StationName;
    public int DockingCapacity;
    public int CurrentDocked;
    public List<StationService> Services;
}

public struct StationService
{
    public ServiceType Type;
    public bool IsAvailable;
    public float PriceModifier;
}
```

---

## Sensor Simulation Layer (Tier 2)

Lightweight struct representing what sensors can detect about a distant entity. No polymorphism, no module detail - just observable state.

```csharp
public struct SensorContact
{
    // Identity
    public int EntityId;
    public int ShipConfigId;
    public int FactionIndex;
    public EntityPersistence Persistence;
    public FixedString32Bytes UniqueId;

    // Observable state (what sensors detect)
    public double2 Position;
    public double2 Velocity;
    public float Heading;
    public float Speed;

    // Aggregate stats (no module detail)
    public float HullPercent;
    public float ShieldPercent;
    public bool ShieldsActive;
    public bool WeaponsArmed;

    // Warp state (detectable as signature)
    public bool IsInWarp;
    public float WarpSpeed;
    public double2 WarpDirection;

    // State machine AI
    public SensorAIState CurrentState;
    public SensorAIState PreviousState;
    public float StateTimer;
    public int TargetEntityId;
    public double2 Waypoint;

    // Ship capabilities (from config, for AI decisions)
    public float MaxSpeed;
    public float WeaponRange;
    public float SensorRange;
    public float CombatStrength;

    // Snapshot for restoring to Rich layer
    public ShipSnapshot Snapshot;
}

public enum SensorAIState : byte
{
    Idle = 0,
    Patrol = 1,
    Pursue = 2,
    Combat = 3,
    Flee = 4,
    Orbit = 5,
    Dock = 6,
    Warp = 7
}
```

### Sensor Detection Levels

What the player's sensors reveal depends on distance and sensor capability:

| Detection Level | Range Factor | Info Shown |
|----------------|-------------|------------|
| Blip | Max range | Dot on map, faction unknown |
| Contact | 75% range | Faction, heading, speed |
| Identified | 50% range | Ship class, hull/shield status |
| Full Read | 25% range | Weapons, warp state, target |

```csharp
public enum DetectionLevel : byte
{
    None = 0,
    Blip = 1,
    Contact = 2,
    Identified = 3,
    FullRead = 4
}
```

### Faction Sensor Pool

Shared detection data per faction (sensor contacts detected by any ship in the faction):

```csharp
public class FactionSensorPool
{
    public int FactionIndex;
    public double LastUpdateTime;
    public List<FactionDetection> Detections;
}

public struct FactionDetection
{
    public int DetectedEntityId;
    public double2 Position;
    public double2 LastKnownVelocity;
    public float Distance;
    public DetectionLevel Level;
    public double DetectedTime;
    public double ConfidenceDecayStart;
    public int DetectedByEntityId;
}
```

---

## Strategic Layer (Tier 3-4)

### Fleet Data

When transient entities move beyond 100k units, they group into fleets:

```csharp
public struct FleetData
{
    public int FleetId;
    public int FactionIndex;
    public double2 Position;
    public double2 Velocity;
    public float Rotation;
    public int MemberCount;
    public FleetFormation Formation;
    public float TotalStrength;
    public float TotalHP;
    public FleetBehavior CurrentBehavior;
    public int TargetFleetId;
}

public enum FleetFormation : byte
{
    Line = 0,
    Wedge = 1,
    Circle = 2,
    Cluster = 3
}

public enum FleetBehavior : byte
{
    Patrol = 0,
    Intercept = 1,
    Retreat = 2,
    Hold = 3,
    Escort = 4
}
```

### Fleet Members

```csharp
public struct FleetMember
{
    public int EntityId;
    public int FormationSlot;
    public float2 FormationOffset;
    public float FormationRotation;
    public float Strength;
    public float HP;
    public EntityPersistence Persistence;
    public ShipSnapshot Snapshot;
}
```

### Fleet Lifecycle

1. **Grouping (Tier 2 -> 3):** Same-faction SensorContacts within grouping radius form a fleet. Individual snapshots stored in FleetMember entries.
2. **Simulation:** Fleet AI runs at ~1 second intervals. Battles resolved abstractly (strength vs strength).
3. **Unpacking (Tier 3 -> 2):** Player approaches. Fleet dissolves. SensorContacts created from FleetMember snapshots. Positions calculated from formation offsets transformed by fleet rotation.

```csharp
public static double2 CalculateUnpackPosition(in FleetData fleet, in FleetMember member)
{
    float rotRad = math.radians(fleet.Rotation);
    float cos = math.cos(rotRad);
    float sin = math.sin(rotRad);

    double2 rotatedOffset = new double2(
        member.FormationOffset.x * cos - member.FormationOffset.y * sin,
        member.FormationOffset.x * sin + member.FormationOffset.y * cos
    );

    return fleet.Position + rotatedOffset;
}
```

### Dormant Tracking (Tier 4)

```csharp
public struct DormantRecord
{
    public ChunkCoord Chunk;
    public EntityTypeEnum EntityType;
    public int FactionIndex;
    public int Count;
    public uint Seed;
    public EntityPersistence Persistence;
    public FixedString32Bytes UniqueId;
    public ShipSnapshot Snapshot;   // Only for Persistent entities
}
```

---

## Mass Entity Layer

Burst-compatible structs for uniform entities processed in batch.

### Asteroid Data

```csharp
[BurstCompatible]
public struct AsteroidData
{
    public double2 Position;
    public double2 Velocity;
    public float Rotation;
    public float AngularVelocity;
    public float Mass;
    public float Radius;
    public int Variant;
    public float Seed;
    public AsteroidSource Source;
    public float ResourceValue;
    public float HullIntegrity;
    public float MaxHull;
}

public enum AsteroidSource : byte
{
    Belt = 0,
    Ring = 1,
    Scatter = 2
}
```

### Projectile Data

```csharp
[BurstCompatible]
public struct ProjectileData
{
    public double2 Position;
    public double2 Velocity;
    public float Rotation;
    public float Damage;
    public DamageTypeEnum DamageType;
    public float Lifetime;
    public float ElapsedTime;
    public int OwnerEntityId;
    public int FactionIndex;
    public ProjectileTypeEnum Type;
}

public enum ProjectileTypeEnum : byte
{
    Kinetic = 0,
    Energy = 1,
    Missile = 2,
    Beam = 3
}
```

### Debris Data

```csharp
[BurstCompatible]
public struct DebrisData
{
    public double2 Position;
    public double2 Velocity;
    public float Rotation;
    public float AngularVelocity;
    public float Mass;
    public float Radius;
    public float Lifetime;
    public int Variant;
}
```

### Gravity Source Data

Used by both the Mass Entity Layer (for asteroid gravity) and the Rich Layer (for ship gravity).

```csharp
[BurstCompatible]
public struct GravitySourceData
{
    public int SourceId;
    public GravitySourceType SourceType;
    public double2 AbsolutePosition;
    public double Mass;
    public double GravitationalParameter;   // GM (pre-computed)
    public double SOIRadius;
    public double SurfaceRadius;
    public int ParentSourceId;
    public int StarSystemId;
    public bool IsImmovable;
    public float Luminosity;
}

public enum GravitySourceType : byte
{
    BlackHole = 0,
    Star = 1,
    Planet = 2,
    Moon = 3
}
```

### Celestial Body Data

```csharp
public struct CelestialBodyData
{
    public CelestialBodyKind Kind;
    public float Luminosity;
    public float Seed;
    public int ParentSourceId;
    public int StarSystemId;
}

public enum CelestialBodyKind : byte
{
    BlackHole = 0,
    Star = 1,
    Planet = 2,
    Moon = 3
}

public struct PrecomputedOrbit
{
    public double SemiMajorAxis;
    public double Eccentricity;
    public double ArgumentOfPeriapsis;
    public double InitialMeanAnomaly;
    public double Period;
    public double2 OrbitCenter;

    public double2 SamplePosition(double time);
    public double2 SampleVelocity(double time);
}

public struct StarSystemData
{
    public int SystemId;
    public StarSystemType Type;
    public FixedList128Bytes<int> StarSourceIds;
    public double2 Barycenter;
    public double TotalMass;
    public OrbitPatternType PatternType;
}

public enum StarSystemType : byte { Single = 0, Binary = 1, Triple = 2 }
public enum OrbitPatternType : byte { None = 0, SimpleBinary = 1, HierarchicalTriple = 2, Figure8Triple = 3 }
```

---

## Collision Configuration

```csharp
public struct CollisionConfig
{
    public CollisionResponseEnum Response;
    public byte CollisionLayer;
    public byte CollisionMask;
    public bool IsCollidable;
}

public enum CollisionResponseEnum : byte
{
    Bounce = 0,
    Damage = 1,
    Destroy = 2,
    None = 3
}
```

Used by both Rich Layer (per ShipInstance/StationInstance) and Mass Entity Layer (per AsteroidData).

---

## Game Event System

The event bus that both C# managers and Lua scripts subscribe to. Events are queued during the frame and dispatched to Lua in a single batch during ScriptExecutionManager.Update().

```csharp
public class GameEventBus
{
    public event Action<ShipInstance> OnEntitySpawned;
    public event Action<ShipInstance, PendingDamage> OnEntityDamaged;
    public event Action<ShipInstance, int> OnEntityDestroyed;
    public event Action<ProjectileData, ShipInstance> OnProjectileHit;
    public event Action<ShipInstance, StationInstance> OnPlayerDocked;
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
}
```

C# managers fire events during their update step. The ScriptExecutionManager collects these and dispatches them to registered Lua callbacks after all simulation is complete. This ensures Lua sees a consistent world state.

**Multiplayer:** A `NetworkEventBridge` subscribes to gameplay events on the server and forwards relevant events to clients via ObserversRpc/TargetRpc for VFX and SFX. See [[13-networking-architecture]] for the full event routing table.

---

## Lua Entity Proxy

Managed proxy that wraps a ShipInstance for safe Lua access. Created on-demand when dispatching events to Lua scripts.

```csharp
public class LuaEntityProxy
{
    private ShipInstance _ship;

    // Read properties
    // id, type, faction, position (x,y), velocity (x,y), heading
    // health, max_health, shield, max_shield
    // speed, is_in_warp

    // Write methods
    // set_health(float), set_position(x, y), set_velocity(x, y)

    // Tag/data methods
    // has_tag(string), get_data(string), set_data(string, float)

    // Module access
    // get_module(category) -> module proxy with health, efficiency, is_enabled
}
```

Since ShipInstance is a managed C# object, the proxy reads/writes fields directly. No deferred buffer. Changes are visible immediately within the same frame.

See [[12-modding-architecture]] for the full Lua API surface.

---

## Quality Settings

```csharp
public class SimulationQualitySettings : ScriptableObject
{
    [Header("Tier Boundaries (World Units)")]
    public float Tier0MaxDistance = 5000;
    public float Tier1MaxDistance = 20000;
    public float Tier2MaxDistance = 100000;
    public float Tier3MaxDistance = 200000;

    [Header("Hysteresis (15% of tier boundary)")]
    public float Tier0Hysteresis = 750;
    public float Tier1Hysteresis = 3000;
    public float Tier2Hysteresis = 15000;
    public float Tier3Hysteresis = 30000;

    [Header("Cooldowns")]
    public float TierChangeCooldown = 2.0f;

    [Header("Update Rates")]
    public int SensorUpdateBatchSize = 30;
    public float StrategicUpdateInterval = 1.0f;

    [Header("Capacity Limits")]
    public int Tier0MaxEntities = 20;
    public int RichLayerMaxEntities = 500;
    public int SensorLayerMaxEntities = 2000;
    public int MaxFleets = 100;
    public int MaxCriticalEntities = 50;

    [Header("Fleet Settings")]
    public int MinFleetSize = 3;
    public float FleetGroupingRadius = 5000;
}
```

---

## Related Documents

- [[02-system-architecture]] - How each layer processes its data
- [[03-tiered-simulation]] - Distance-based tier system and transitions
- [[04-archetype-strategy]] - Entity composition patterns per layer
- [[05-configuration-layer]] - JSON configuration for ships and modules
- [[09-progressive-destruction]] - Damage system detail
- [[10-gravity-system]] - Gravity components and orbital mechanics
- [[11-heat-system]] - Heat components and thermal radiation
- [[12-modding-architecture]] - Mod loading, Lua scripting, event bridge
- [[13-networking-architecture]] - NetworkShipState, ShipStateDelta, SensorContactSummary, delta compression

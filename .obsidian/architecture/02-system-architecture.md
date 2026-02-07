# System Architecture

This document defines the processing pipeline for each layer, execution order, and data flow.

---

## Processing Overview

Each layer has its own manager class that runs during Unity's update cycle. The layers execute in sequence, not as DOTS system groups.

```mermaid
flowchart TB
    subgraph FRAME["MonoBehaviour Update (Per Frame)"]
        direction TB
        GM["GameManager.Update()"]
        INPUT["InputManager"]
        RICH["RichEntityManager"]
        SENSOR["SensorSimulationManager"]
        MASS["MassEntityManager"]
        ENV["EnvironmentManager"]
        TIER["TierManager"]
        SCRIPT["ScriptExecutionManager"]
        PRESENT["PresentationManager"]
    end

    GM --> INPUT
    INPUT --> RICH
    RICH --> SENSOR
    SENSOR --> MASS
    MASS --> ENV
    ENV --> TIER
    TIER --> SCRIPT
    SCRIPT --> PRESENT
```

---

## Layer Managers

### 1. InputManager

Collects input from the player and feeds it into the player's ShipInstance.

```csharp
public class InputManager : MonoBehaviour
{
    public ControlInput CurrentInput { get; private set; }

    void Update()
    {
        // Read Unity Input System
        // Detect keyboard/mouse vs gamepad
        // Populate CurrentInput struct
    }
}
```

| Responsibility | Budget |
|---------------|--------|
| Read Unity Input System | 0.2ms |
| Detect input device | 0.1ms |
| Populate ControlInput | 0.1ms |
| **Total** | **0.5ms** |

---

### 2. RichEntityManager

Manages all Tier 0-1 ships and stations. This is the core gameplay simulation.

```mermaid
flowchart TB
    subgraph RichUpdate["RichEntityManager.Update()"]
        direction TB
        AI["1. AI Evaluation\n(BehaviorController.Evaluate)"]
        MOD["2. Module Updates\n(shields, weapons, sensors)"]
        ABILITY["3. Ability Updates\n(per-ship unique abilities)"]
        PHYS["4. Physics Integration\n(velocity, rotation, position)"]
        COLL["5. Collision Detection\n(spatial hash)"]
        COMBAT["6. Combat Processing\n(damage, destruction)"]
    end

    AI --> MOD --> ABILITY --> PHYS --> COLL --> COMBAT
```

```csharp
public class RichEntityManager
{
    private Dictionary<int, ShipInstance> _ships;
    private Dictionary<int, StationInstance> _stations;
    private SpatialHashGrid _collisionGrid;
    private SimulationContext _context;

    public void Update(float deltaTime)
    {
        // 1. AI evaluation (behavior trees for AI ships)
        foreach (var ship in _ships.Values)
        {
            if (ship.BehaviorController != null)
            {
                var input = ship.BehaviorController.Evaluate(ship, _context);
                ship.ApplyControlInput(input);
            }
        }

        // 2. Module updates (shields regen, weapon cooldowns, sensor scans)
        foreach (var ship in _ships.Values)
            ship.UpdateModules(deltaTime);

        // 3. Ability updates (unique per-class abilities)
        foreach (var ship in _ships.Values)
            ship.UpdateAbilities(deltaTime);

        // 4. Physics integration
        foreach (var ship in _ships.Values)
            ship.IntegratePhysics(deltaTime);

        // 5. Collision detection
        _collisionGrid.Clear();
        _collisionGrid.InsertAll(_ships.Values);
        _collisionGrid.DetectAndResolve();

        // 6. Combat processing (damage routing, destruction)
        ProcessCombat(deltaTime);
    }
}
```

| Step | Responsibility | Budget |
|------|---------------|--------|
| AI Evaluation | Behavior trees for AI ships | 1.0ms |
| Module Updates | Shield regen, weapon cooldowns, sensors | 0.5ms |
| Ability Updates | Per-class unique abilities | 0.3ms |
| Physics Integration | Velocity, rotation, position updates | 0.3ms |
| Collision Detection | Spatial hash broad + narrow phase | 0.5ms |
| Combat Processing | Damage routing, destruction | 0.4ms |
| **Total** | | **3.0ms** |

**Collision System (Rich Layer):**

| Tier | Collision Approach |
|------|-------------------|
| Tier 0 | Unity Physics (Rigidbody2D colliders) |
| Tier 1 | Managed spatial hash with circle-circle tests |

```csharp
public class SpatialHashGrid
{
    private const float CellSize = 50f;
    private const int MaxEntitiesPerCell = 64;

    public void InsertAll(IEnumerable<ShipInstance> ships);
    public void DetectAndResolve();
}
```

---

### 3. SensorSimulationManager

Manages Tier 2 entities visible on sensors. Amortized updates for realistic but efficient simulation.

```mermaid
flowchart TB
    subgraph SensorUpdate["SensorSimulationManager.Update()"]
        direction TB
        BATCH["1. Select Batch\n(round-robin cursor)"]
        SM["2. State Machine AI\n(per contact in batch)"]
        POS["3. Position Updates\n(velocity integration)"]
        NOTIFY["4. Notify Map\n(state change events)"]
    end

    BATCH --> SM --> POS --> NOTIFY
```

```csharp
public class SensorSimulationManager
{
    private List<SensorContact> _contacts;
    private int _updateCursor;
    private int _batchSize = 30;

    public void Update(float deltaTime)
    {
        float scaledDt = deltaTime * (_contacts.Count / (float)_batchSize);
        int end = Math.Min(_updateCursor + _batchSize, _contacts.Count);

        for (int i = _updateCursor; i < end; i++)
        {
            ref var contact = ref _contacts[i];
            UpdateStateMachine(ref contact, scaledDt);
            IntegratePosition(ref contact, scaledDt);
            CheckStateChanges(ref contact);
        }

        _updateCursor = (end >= _contacts.Count) ? 0 : end;
    }
}
```

**State Machine AI:**

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Patrol: Has waypoints
    Idle --> Pursue: Enemy detected
    Idle --> Dock: Near station + needs service

    Patrol --> Pursue: Enemy in range
    Patrol --> Idle: No waypoints
    Patrol --> Dock: Low hull near station

    Pursue --> Combat: In weapon range
    Pursue --> Flee: Low health
    Pursue --> Patrol: Target lost

    Combat --> Flee: HullPercent < 0.3
    Combat --> Pursue: Target fled

    Flee --> Warp: Has warp + safe to charge
    Flee --> Idle: Safe distance

    Warp --> Patrol: Arrived at destination
    Warp --> Idle: Warp dropped

    Dock --> Idle: Docking complete

    Orbit --> Pursue: Enemy detected
    Orbit --> Idle: Orbit complete
```

| Responsibility | Budget |
|---------------|--------|
| State machine evaluation (~30 contacts/frame) | 0.2ms |
| Position integration | 0.1ms |
| State change notifications | 0.1ms |
| **Total** | **0.5ms** |

At ~200 contacts / 30 per batch = full cycle every ~7 frames. At 60fps that is ~8.5 updates per second per entity.

---

### 4. MassEntityManager

Manages asteroids, debris, and projectiles using NativeArrays and Burst Jobs.

```mermaid
flowchart TB
    subgraph MassUpdate["MassEntityManager.Update()"]
        direction TB
        PROJ["1. Projectile Update\n(IJobParallelFor, Burst)"]
        AST["2. Asteroid Physics\n(gravity, position)"]
        DEB["3. Debris Lifetime\n(decay, destroy)"]
        COLL["4. Mass-Rich Collision\n(projectile vs ships)"]
    end

    PROJ --> AST --> DEB --> COLL
```

```csharp
public class MassEntityManager
{
    private NativeArray<ProjectileData> _projectiles;
    private NativeArray<AsteroidData> _asteroids;
    private NativeArray<DebrisData> _debris;
    private NativeArray<GravitySourceData> _gravitySources;

    public void Update(float deltaTime)
    {
        // 1. Projectile position update (Burst)
        new ProjectileUpdateJob
        {
            Projectiles = _projectiles,
            DeltaTime = deltaTime
        }.Schedule(_projectiles.Length, 64).Complete();

        // 2. Asteroid gravity + position (Burst)
        new AsteroidGravityJob
        {
            Asteroids = _asteroids,
            GravitySources = _gravitySources,
            DeltaTime = deltaTime
        }.Schedule(_asteroids.Length, 64).Complete();

        // 3. Debris lifetime
        new DebrisLifetimeJob
        {
            Debris = _debris,
            DeltaTime = deltaTime
        }.Schedule(_debris.Length, 64).Complete();

        // 4. Projectile vs Rich entity collision (queries RichEntityManager)
        CheckProjectileHits();
    }
}
```

**Burst Jobs:**

```csharp
[BurstCompile]
public struct AsteroidGravityJob : IJobParallelFor
{
    public NativeArray<AsteroidData> Asteroids;
    [ReadOnly] public NativeArray<GravitySourceData> GravitySources;
    public float DeltaTime;

    public void Execute(int index)
    {
        var asteroid = Asteroids[index];

        // Velocity Verlet integration with gravity
        double2 acceleration = CalculateGravity(asteroid.Position);
        asteroid.Position += asteroid.Velocity * DeltaTime
                          + 0.5 * acceleration * DeltaTime * DeltaTime;
        double2 newAcceleration = CalculateGravity(asteroid.Position);
        asteroid.Velocity += 0.5 * (acceleration + newAcceleration) * DeltaTime;

        asteroid.Rotation += asteroid.AngularVelocity * DeltaTime;

        Asteroids[index] = asteroid;
    }

    private double2 CalculateGravity(double2 position)
    {
        double2 totalAccel = double2.zero;
        for (int i = 0; i < GravitySources.Length; i++)
        {
            var source = GravitySources[i];
            double2 diff = source.AbsolutePosition - position;
            double distSq = math.lengthsq(diff);
            double dist = math.sqrt(distSq);
            if (dist < source.SurfaceRadius) continue;
            totalAccel += (source.GravitationalParameter / distSq)
                        * (diff / dist);
        }
        return totalAccel;
    }
}

[BurstCompile]
public struct ProjectileUpdateJob : IJobParallelFor
{
    public NativeArray<ProjectileData> Projectiles;
    public float DeltaTime;

    public void Execute(int index)
    {
        var proj = Projectiles[index];
        proj.Position += proj.Velocity * DeltaTime;
        proj.ElapsedTime += DeltaTime;
        Projectiles[index] = proj;
    }
}
```

| Step | Responsibility | Budget |
|------|---------------|--------|
| Projectile update | Position integration (Burst) | 0.3ms |
| Asteroid gravity | Velocity Verlet (Burst) | 1.0ms |
| Debris lifetime | Decay and cleanup | 0.2ms |
| Mass-Rich collision | Projectile hit detection | 0.5ms |
| **Total** | | **2.0ms** |

---

### 5. EnvironmentManager

Gravity source updates and heat simulation. Applies to both Rich and Mass layers.

```csharp
public class EnvironmentManager
{
    private NativeArray<GravitySourceData> _gravitySources;
    private List<StarSystemData> _starSystems;

    public void Update(float deltaTime)
    {
        // 1. Update star positions from pre-computed orbits
        UpdateStarOrbits(deltaTime);

        // 2. Recalculate barycenters for multi-star systems
        UpdateBarycenters();

        // 3. Apply gravity to Rich layer ships
        ApplyGravityToShips(deltaTime);

        // 4. Apply heat to Rich layer ships
        ApplyHeatToShips(deltaTime);
    }
}
```

See [[10-gravity-system]] and [[11-heat-system]] for detail.

| Responsibility | Budget |
|---------------|--------|
| Star orbit updates | 0.1ms |
| Barycenter calculation | 0.1ms |
| Ship gravity (Rich layer) | 0.4ms |
| Ship heat (Rich layer) | 0.2ms |
| **Total** | **1.0ms** (asteroid gravity counted in Mass layer) |

---

### 6. TierManager

Distance calculation and layer transitions.

```mermaid
flowchart TB
    subgraph TierUpdate["TierManager.Update()"]
        direction TB
        DIST["1. Distance Calculation\n(player to all entities)"]
        TRANS["2. Tier Transitions\n(promote/demote between layers)"]
        CHUNK["3. Chunk Migration\n(entities crossing chunk boundaries)"]
        STRAT["4. Strategic Updates\n(fleet AI, ~1s interval)"]
    end

    DIST --> TRANS --> CHUNK --> STRAT
```

```csharp
public class TierManager
{
    private RichEntityManager _richLayer;
    private SensorSimulationManager _sensorLayer;
    private StrategicManager _strategicLayer;
    private SimulationQualitySettings _settings;

    public void Update(float deltaTime)
    {
        var playerPos = GetPlayerPosition();

        // Check Rich layer entities for demotion to Sensor
        CheckRichDemotions(playerPos);

        // Check Sensor layer contacts for promotion to Rich
        CheckSensorPromotions(playerPos);

        // Check Sensor contacts for demotion to Strategic
        CheckSensorDemotions(playerPos);

        // Check Strategic for promotion to Sensor
        CheckStrategicPromotions(playerPos);

        // Update strategic layer (amortized, ~1s interval)
        _strategicLayer.Update(deltaTime);
    }
}
```

**Transition with hysteresis:**
- Demote at boundary distance
- Promote at boundary - hysteresis (15%)
- 2-second cooldown between tier changes per entity

| Responsibility | Budget |
|---------------|--------|
| Distance calculation | 0.2ms |
| Tier transitions | 0.2ms |
| Strategic updates (amortized) | 0.1ms |
| **Total** | **0.5ms** |

---

### 7. ScriptExecutionManager

Dispatches game events to Lua mod scripts and processes Lua API calls. Runs after all game simulation is complete for the frame, so Lua callbacks see consistent world state.

```csharp
public class ScriptExecutionManager
{
    private LuaRuntime _lua;
    private GameEventBus _eventBus;
    private Queue<GameEvent> _pendingEvents;

    public void Update(float deltaTime)
    {
        // 1. Dispatch buffered events to Lua callbacks
        while (_pendingEvents.TryDequeue(out var evt))
            _lua.DispatchEvent(evt);

        // 2. Process Lua timer callbacks
        _lua.UpdateTimers(deltaTime);

        // 3. Execute any queued Lua API calls
        // (e.g., starfire.spawn() requests from Lua)
        _lua.ProcessPendingCommands();
    }
}
```

**Why events are buffered:** Most events originate from RichEntityManager (combat, spawning) and MassEntityManager (projectile hits) which run earlier in the frame. Events are queued to the `GameEventBus` during the frame and dispatched to Lua in a single batch during `ScriptExecutionManager.Update()`. This guarantees Lua callbacks see a consistent world state where all simulation for the frame is complete.

**Burst constraint:** Mass layer events (projectile hits detected in Burst jobs) cannot call into managed code during the job. Instead, `MassEntityManager` collects hit results into a managed `NativeQueue` after the Burst job completes, then forwards them to `GameEventBus` for dispatch here.

**Direct Rich layer access:** Since `ShipInstance` is a managed C# class (not an ECS entity), Lua scripts interact with entities through `LuaEntityProxy` wrappers that read/write fields directly. Changes take effect immediately within the same frame — no deferred buffer or 1-frame delay.

| Responsibility | Budget |
|---------------|--------|
| Event dispatch to Lua | 0.2ms |
| Timer callbacks | 0.1ms |
| API call processing | 0.2ms |
| **Total** | **0.5ms** |

See [[12-modding-architecture]] for the full Lua API surface, sandbox configuration, and mod loading pipeline.

---

### 8. PresentationManager

Syncs visual GameObjects for Tier 0 entities.

```csharp
public class PresentationManager
{
    private Dictionary<int, ShipView> _activeViews;
    private GameObjectPool _pool;

    public void Update()
    {
        // Sync ShipInstance data → ShipView MonoBehaviour
        foreach (var (entityId, view) in _activeViews)
        {
            var ship = _richLayer.GetShip(entityId);
            view.SyncFromShip(ship);
        }
    }

    public void PromoteToView(ShipInstance ship)
    {
        var go = _pool.Get(ship.ShipConfigId);
        var view = go.GetComponent<ShipView>();
        view.Initialize(ship);
        _activeViews[ship.Id.Value] = view;
    }

    public void DemoteFromView(int entityId)
    {
        if (_activeViews.TryGetValue(entityId, out var view))
        {
            _pool.Return(view.gameObject);
            _activeViews.Remove(entityId);
        }
    }
}
```

**ShipView MonoBehaviour (Tier 0 only):**

```csharp
public class ShipView : MonoBehaviour
{
    private ShipInstance _ship;
    private SpriteRenderer _renderer;
    private ParticleSystem _thrusterFX;
    private ParticleSystem _shieldFX;

    public void SyncFromShip(ShipInstance ship)
    {
        // Position (using LocalPosition relative to floating origin)
        transform.position = WorldToLocal(ship.Position);
        transform.rotation = Quaternion.Euler(0, 0, ship.Rotation);

        // Visual effects based on ship state
        UpdateThrusterFX(ship);
        UpdateShieldFX(ship);
        UpdateDamageEffects(ship);
    }
}
```

| Responsibility | Budget |
|---------------|--------|
| Transform sync | 0.5ms |
| Visual effects update | 0.5ms |
| Audio management | 0.3ms |
| Map/minimap rendering | 0.7ms |
| **Total** | **2.0ms** |

---

## Combat Pipeline Detail

Combat runs within the RichEntityManager for Tier 0-1 entities.

```mermaid
sequenceDiagram
    participant WPN as WeaponModule
    participant MASS as MassEntityManager
    participant PROJ as Projectile
    participant DMG as DamageModel
    participant HULL as HullModule
    participant DEST as DestructionHandler

    WPN->>MASS: Fire (spawn projectile)
    MASS->>PROJ: Position update (Burst)
    PROJ->>DMG: Hit detected → PendingDamage
    DMG->>DMG: Shield absorption
    DMG->>DMG: Hitbox zone detection
    DMG->>DMG: Module damage routing
    DMG->>HULL: Module overflow → hull
    HULL->>HULL: Armor reduction
    HULL->>DEST: If hull <= 0
    DEST->>DEST: Loot, effects, cleanup
```

**Damage Flow:**
1. Shields absorb damage first (reduce amount)
2. Remaining damage → hitbox zone detection (from impact position)
3. Zone → module mapping routes damage to specific modules
4. Module overflow → hull damage (armor reduction applied)
5. Hull <= 0 → destruction

See [[09-progressive-destruction]] for detail.

**Event dispatch:** Combat events (`OnEntityDamaged`, `OnEntityDestroyed`, `OnProjectileHit`) are queued to the `GameEventBus` during combat processing. They are not dispatched immediately — Lua callbacks receive them later in the frame during `ScriptExecutionManager.Update()`, after all simulation is complete.

---

## Warp Mode System Integration

When warp is engaged, processing changes across layers:

| System | Normal Mode | Warp Mode |
|--------|-------------|-----------|
| Rich Layer physics | Standard integration | WarpMovementSystem (high-speed) |
| Rich Layer collision | Full detection | Swept raycast only |
| Sensor Layer | Normal updates | Updates continue normally |
| Mass Entity spawning | Active | Suspended in warp corridor |
| Tier transitions | Distance-based | Corridor entities freeze |

See [[07-warp-system]] for detail.

---

## Audio Integration

| Tier | Audio Behavior |
|------|----------------|
| Tier 0 | Full 3D spatial audio, all sounds |
| Tier 1 | Distant sounds only (explosions, large weapons) |
| Tier 2+ | No audio (too far) |

---

## Performance Budget Summary

| Group | Target | Notes |
|-------|--------|-------|
| Input | 0.5ms | Player input collection |
| Rich Layer | 3.0ms | AI, modules, abilities, physics, collision, combat |
| Sensor Layer | 0.5ms | Amortized state machine (~30/frame) |
| Mass Entities | 2.0ms | Burst gravity, projectiles, debris |
| Environment | 1.0ms | Gravity sources, heat |
| Tier Management | 0.5ms | Distance, transitions, strategic |
| Script Execution | 0.5ms | Lua event dispatch, timer callbacks, mod API calls |
| Presentation | 2.0ms | View sync, effects, audio, map |
| **Total** | **10.0ms** | **6.5ms headroom for 60 FPS** |

---

## Related Documents

- [[01-component-model]] - Data types processed by each system
- [[03-tiered-simulation]] - Tier system driving layer membership
- [[07-warp-system]] - Warp mode system changes
- [[09-progressive-destruction]] - Combat damage pipeline
- [[10-gravity-system]] - Gravity simulation detail
- [[11-heat-system]] - Heat simulation detail
- [[12-modding-architecture]] - Mod loading, Lua scripting, event bridge

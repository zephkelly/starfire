# Background Simulation System Handoff

## Purpose

The background simulation system keeps entities "alive" when their chunks unload. When a player pushes an asteroid and flies away, the asteroid continues moving in the background simulation. When the player returns, the asteroid appears at its correctly simulated position.

## Architecture Overview

```
BackgroundSimulationManager (MonoBehaviour singleton, orchestrator)
├── Tier1ActiveSimulator    — lightweight per-frame Euler physics, nearby unloaded chunks
├── Tier2BallisticTracker   — snapshot + analytical prediction, medium distance
└── Tier3EventSystem        — discrete events, galaxy scale (NOT IMPLEMENTED)
```

All tiers use **absolute coordinates (Vector2D / double)** so entities can exist far from the floating origin.

## File Structure

```
Assets/core/v2/world/simulation/
├── BackgroundSimulationConfig.cs    — ScriptableObject: tier radii, max counts, thresholds
├── BackgroundSimulationManager.cs   — MonoBehaviour singleton orchestrator + preview system
├── SimulatedEntity.cs               — Data class: ID, absolute pos/vel, rotation, mass, etc.
├── SimulationTier.cs                — Enum: None, Tier1Active, Tier2Ballistic, Tier3Event
├── Tier1ActiveSimulator.cs          — Per-frame Euler integration, chunk migration detection
├── Tier2BallisticTracker.cs         — Snapshot storage + analytical position prediction
├── BallisticSnapshot.cs             — Data class for Tier 2 snapshots
└── Editor/
    └── BackgroundSimulationManagerEditor.cs  — Custom inspector with preview display
```

## Key Files and Their Roles

### BackgroundSimulationConfig.cs
ScriptableObject that configures the simulation:
- `tier1ChunkRadius` — How many chunks beyond loaded area use Tier 1 simulation
- `tier1MaxEntities` — Max entities in active simulation
- `tier2StartDistance` — Chebyshev distance where Tier 2 begins
- `minVelocitySqrToSimulate` — Velocity threshold (squared) for registering entities (default 0.01)

### BackgroundSimulationManager.cs
Central orchestrator singleton:
- Creates and manages Tier1ActiveSimulator and Tier2BallisticTracker
- Handles tier transitions based on player chunk distance
- Provides `RegisterEntity()` / `UnregisterEntity()` API
- Contains debug preview system (texture-based visualization)
- Instance accessed via `BackgroundSimulationManager.Instance`

### SimulatedEntity.cs
Data class holding entity state:
```csharp
public class SimulatedEntity
{
    public int EntityId;
    public EntityType EntityType;
    public ChunkCoord CurrentChunk;
    public ChunkCoord OriginChunk;
    public Vector2D AbsolutePosition;  // Double precision
    public Vector2D Velocity;          // Double precision
    public float Rotation;
    public float AngularVelocity;
    public float Mass;
    public float Radius;
    public float Drag;
    public SimulationTier CurrentTier;
    public double LastSimulationTime;
    public bool HasBeenModified;
    // Visual recreation data
    public int Variant;
    public float Seed;
    public int SourceType;
}
```

### Tier1ActiveSimulator.cs
Lightweight physics for nearby entities:
- Euler integration: `position += velocity * dt`
- Drag application: `velocity *= (1 - drag/mass * dt)`
- Chunk migration detection via `ChunkCoord.FromAbsolutePosition()`
- Fires `OnEntityMigratedChunk` event for cross-chunk tracking

### Tier2BallisticTracker.cs
Analytical prediction for distant entities:
- Stores `BallisticSnapshot` with position, velocity, drag, timestamp
- On query, computes predicted position analytically (no per-frame updates)
- More efficient for large numbers of distant entities

## Integration Points

### BackgroundSimulationManager (Manual Setup)
The BackgroundSimulationManager is now manually added to a GameObject in the scene. It self-initializes when:
1. The `config` field is assigned in the inspector
2. WorldGenerationService is initialized and ready

The manager will retry initialization each frame until WorldGenerationService is available.

**CRITICAL**: You must manually add BackgroundSimulationManager to a GameObject and assign the BackgroundSimulationConfig asset.

### AsteroidConsumer.cs
Registers asteroids with simulation when chunks unload:
```csharp
public void OnChunkUnloading(Chunk.Chunk chunk)
{
    // For each asteroid with Rigidbody2D...
    if (rb.linearVelocity.sqrMagnitude > velocityThreshold)
    {
        var simEntity = new SimulatedEntity { ... };
        simManager.RegisterEntity(simEntity);
    }
    // Return to pool or destroy
}
```

Also handles promotion (spawning) when chunks reload:
```csharp
public void OnChunkLoaded(Chunk.Chunk chunk)
{
    var simulatedEntities = simManager.PromoteEntitiesForChunk(chunk.Coord);
    foreach (var simEntity in simulatedEntities)
    {
        // Create visual, apply velocity from simulation
        rb.linearVelocity = simEntity.Velocity.ToVector2();
    }
}
```

## Data Flow

### Registration Flow (chunk unloading)
```
1. ChunkManager unloads chunk
2. AsteroidConsumer.OnChunkUnloading() called
3. For each asteroid with velocity > threshold:
   - Create SimulatedEntity with absolute position, velocity
   - Call BackgroundSimulationManager.RegisterEntity()
   - Entity added to Tier1ActiveSimulator
4. GameObject returned to pool or destroyed
```

### Simulation Flow (per frame)
```
1. BackgroundSimulationManager.Update()
2. Tier1ActiveSimulator.Tick(deltaTime)
   - For each entity: apply drag, integrate position
   - Check chunk migration, fire events if crossed boundary
3. Check player chunk change
   - Process tier transitions (Tier1 <-> Tier2)
4. Update preview if enabled
```

### Promotion Flow (chunk loading)
```
1. ChunkManager loads chunk
2. AsteroidConsumer.OnChunkLoaded() called
3. BackgroundSimulationManager.PromoteEntitiesForChunk(coord)
   - Returns entities in that chunk from Tier1/Tier2
   - Removes them from simulation
4. Create visual GameObjects at simulated positions
5. Apply velocity to Rigidbody2D
```

## Debug Preview System

The BackgroundSimulationManager includes a texture-based preview (like WorldFabricService):

### Preview Modes
- **Zones**: Colored regions for loaded/Tier1/Tier2/beyond
- **Entities**: Dots on dark background
- **Combined**: Both zones and entities
- **ChunkGrid**: Zones with chunk boundary lines

### Color Scheme
| Element | Color |
|---------|-------|
| Loaded chunks | Blue (0.2, 0.3, 0.5) |
| Tier 1 zone | Green (0.15, 0.4, 0.2) |
| Tier 2 zone | Orange (0.4, 0.35, 0.15) |
| Beyond all tiers | Dark purple (0.08, 0.05, 0.12) |
| Tier 1 entities | Cyan |
| Tier 2 entities | Orange |
| Player position | White crosshair |

### Inspector Features
- Enable/disable toggle
- Mode dropdown
- Resolution, world size sliders
- Follow player toggle
- Entity count statistics
- Color legend

## Setup Instructions

1. **Create BackgroundSimulationConfig asset:**
   - Right-click in Project → Create → Starfire → Simulation → Background Config
   - Configure tier radii and thresholds

2. **Add BackgroundSimulationManager to your scene:**
   - Create a new GameObject (or use an existing one)
   - Add the `BackgroundSimulationManager` component
   - Drag the BackgroundSimulationConfig asset to the `Config` field
   - The manager will self-initialize when WorldGenerationService is ready

3. **Verify asteroid prefabs:**
   - Must have Rigidbody2D component
   - Velocity must exceed `sqrt(minVelocitySqrToSimulate)` (default ~0.1 units/sec)

## Debug Logging

Extensive logging has been added to trace entity registration:

| Prefix | File | Purpose |
|--------|------|---------|
| `[WorldGenService]` | WorldGenerationService.cs | Simulation initialization |
| `[BackgroundSim]` | BackgroundSimulationManager.cs | Registration, tier transitions |
| `[AsteroidConsumer]` | AsteroidConsumer.cs | Chunk unload, velocity checks |
| `[Tier1Simulator]` | Tier1ActiveSimulator.cs | Entity add/remove |

### Troubleshooting Flow
If entities aren't registering:
1. Check `[WorldGenService]` logs — is config assigned?
2. Check `[BackgroundSim] Initialize` — did it complete?
3. Check `[AsteroidConsumer]` logs — are asteroids being checked? What's their velocity?
4. Check `[Tier1Simulator]` logs — is AddEntity being called?

## Collision & Event System

### Overview
The background simulation now includes collision detection, destruction, and event tracking. When entities collide in the background simulation, the collision is resolved with proper physics and recorded as an event that can be queried by other game systems (NPCs, quests, etc.).

### New File Structure
```
Assets/core/v2/world/simulation/
├── collision/
│   ├── SpatialHashGrid.cs           — O(n) broad-phase collision detection
│   ├── CollisionMath.cs             — Circle-circle collision + elastic response
│   └── SimulationCollisionDetector.cs — Orchestrates collision detection
├── events/
│   ├── SimulationEventType.cs       — Enum: Collision, Destruction, etc.
│   ├── SimulationEvent.cs           — Base event class
│   ├── CollisionEvent.cs            — Collision details + instigator
│   ├── DestructionEvent.cs          — Destruction details
│   ├── CollisionResult.cs           — Enum: Bounced, Destroyed, etc.
│   └── SimulationEventLog.cs        — Event storage + queries
└── InstigatorTracker.cs              — Component to track who pushed objects
```

### Collision Detection
- **Broad Phase**: Spatial hash grid with configurable cell size
- **Narrow Phase**: Circle-circle intersection (squared distance check)
- **Response**: Elastic collision with configurable restitution coefficient
- **Separation**: Overlapping entities are pushed apart proportional to inverse mass

### Destruction System
- Impact energy calculated from relative velocity and reduced mass
- Entities have structural integrity = mass × multiplier
- When impact energy exceeds structural integrity, entity is destroyed
- Destruction events are recorded for narrative systems

### Instigator Tracking
Tracks "blame" for collision chains:
1. Add `InstigatorTracker` component to objects that can be pushed
2. When an entity (player/ship) collides with the object, it's recorded as the instigator
3. When the object is registered with the simulation, the instigator ID is preserved
4. When collisions occur in simulation, instigator transfers through the chain
5. Example: Player → Asteroid A → Asteroid B → Station = Player is blamed

### Event Queries
```csharp
var eventLog = BackgroundSimulationManager.Instance.EventLog;

// Get events in a region
var nearby = eventLog.GetEventsInRegion(npcChunk, radius: 5, sinceTime: lastCheckTime);

// Get events caused by player
var playerCaused = eventLog.GetEventsByInstigator(playerId);

// Get destructions of stations (for revenge quests)
var stationDestructions = eventLog.GetDestructionsByVictimType(EntityType.Station);
foreach (var evt in stationDestructions)
{
    if (evt.InstigatorEntityId == playerId)
    {
        TriggerRevengeQuest(evt.NarrativeSummary);
    }
}
```

### Config Options
```csharp
// BackgroundSimulationConfig now includes:
[Header("Collision Detection")]
float spatialHashCellSize = 100f;        // Cell size for broad phase
float collisionRestitution = 0.8f;       // Bounciness (1 = perfectly elastic)
float minCollisionIntensity = 1f;        // Min relative velocity to record

[Header("Destruction")]
float structuralIntegrityMultiplier = 10f;  // integrity = mass × this
float minDestructionEnergy = 100f;          // Below this, no destruction

[Header("Event Storage")]
int maxEvents = 1000;                    // Max events to store
float eventRetentionSeconds = 600f;      // Event lifetime
```

## Known Issues / Current State

### Working
- Tier 1 active simulation (Euler integration)
- Tier 2 ballistic tracking (analytical prediction)
- Preview system with zones and entity visualization
- Chunk migration detection
- Entity registration from AsteroidConsumer
- **Collision detection and resolution in Tier 1**
- **Destruction system with energy thresholds**
- **Event logging with spatial/temporal/instigator queries**
- **Instigator tracking for blame chains**

### Not Implemented
- Tier 3 event system (galaxy-scale discrete events)
- Save/load integration for simulated entities and events
- TrimFarthestEntities() in Tier1 (just a stub)
- Debris spawning on destruction (config exists but not implemented)
- Collision detection in Tier 2 (only Tier 1 entities collide)

## Key Classes Reference

### Vector2D (double precision)
```csharp
public struct Vector2D
{
    public double X, Y;
    public static Vector2D FromVector2(Vector2 v);
    public Vector2 ToVector2();
    public static Vector2D operator +(Vector2D a, Vector2D b);
    public static Vector2D operator *(Vector2D v, double scalar);
}
```

### ChunkCoord
```csharp
public struct ChunkCoord
{
    public long X, Y;
    public static ChunkCoord FromAbsolutePosition(Vector2D pos, double chunkSize);
    public long ChebyshevDistance(ChunkCoord other);
}
```

## Testing Checklist

### Basic Simulation
1. [ ] Push asteroid, fly away until chunk unloads → fly back → asteroid at simulated position
2. [ ] Push asteroid across chunk boundary → verify migration events fire
3. [ ] Push asteroid far away (beyond Tier 1) → fly back later → correct position
4. [ ] Enable preview → verify zones render correctly
5. [ ] Push asteroids → verify dots appear in preview
6. [ ] Trigger floating origin shift → verify no position drift

### Collision System
7. [ ] Push asteroid toward cluster → fly away → return → asteroids bounced apart
8. [ ] Push small asteroid fast into large one → verify destruction occurs
9. [ ] Check inspector → Event stats show collision/destruction counts
10. [ ] Query `EventLog.GetEventsByInstigator(playerId)` → shows player-caused events

### Instigator Tracking
11. [ ] Add `InstigatorTracker` component to asteroid prefab
12. [ ] Push asteroid (player collision sets instigator)
13. [ ] Fly away, wait for chain collision
14. [ ] Return → query destructions → instigator ID is player's entity ID

## Files Modified for This System

- `Assets/core/v2/world/consumers/AsteroidConsumer.cs` — Registration on unload, promotion on load
- `Assets/core/v2/world/simulation/*` — All new files (BackgroundSimulationManager self-initializes)
- `Assets/core/v2/world/Data/Vector2D.cs` — Double-precision vector (may already exist)

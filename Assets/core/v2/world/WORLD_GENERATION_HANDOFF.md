# World Generation Service - Handoff Document

## Overview

A chunk-based world generation service was implemented for the Starfire space shooter game. The system enables infinite procedural world generation with floating origin support, managing content (currently nebulas) via a hashmap/dictionary structure.

## Architecture

```
WorldGenerationService (Singleton Orchestrator)
    │
    ├── ChunkManager
    │   └── Dictionary<ChunkCoord, Chunk>  ← O(1) lookup by grid coordinates
    │
    ├── IChunkDataGenerator (extensible)
    │   └── NebulaChunkGenerator → produces NebulaChunkData
    │
    └── IChunkDataConsumer (extensible)
        └── NebulaRegionConsumer → bridges to existing NebulaRegionManager
```

## File Structure

All files are in `Assets/core/v2/world/`:

```
world/
├── WorldGenerationService.cs      # Main orchestrator singleton
├── WorldGenerationConfig.cs       # ScriptableObject configuration
├── Vector2D.cs                    # Double-precision vector for large coordinates
├── WORLD_GENERATION_HANDOFF.md    # This file
│
├── chunk/
│   ├── ChunkCoord.cs              # Immutable struct, dictionary key
│   ├── ChunkState.cs              # Enum: Unloaded, Loading, Loaded, Unloading
│   ├── Chunk.cs                   # Data container with Dictionary<Type, ChunkData>
│   └── ChunkManager.cs            # Lifecycle: load/unload queues, LRU eviction
│
├── data/
│   ├── ChunkData.cs               # Abstract base class for chunk content
│   └── NebulaChunkData.cs         # Contains List<NebulaRegionDefinition>
│
├── generation/
│   ├── IChunkDataGenerator.cs     # Interface for content generators
│   ├── ChunkGenerationContext.cs  # Context passed to generators
│   ├── ChunkSeed.cs               # Deterministic seeding utilities
│   └── generators/
│       ├── NebulaGenerationConfig.cs  # ScriptableObject for nebula params
│       └── NebulaChunkGenerator.cs    # Generates nebula definitions
│
├── consumers/
│   ├── IChunkDataConsumer.cs      # Interface for runtime object creation
│   └── NebulaRegionConsumer.cs    # Creates NebulaRegion via NebulaRegionManager
│
└── sampling/
    └── PoissonDiskSampler.cs      # Bridson's algorithm for natural distribution
```

## Key Concepts

### 1. Chunk Coordinates (ChunkCoord)
- Immutable `readonly struct` with `X`, `Y` integers
- Efficient `GetHashCode()` for dictionary keys
- Converts between world/absolute positions and grid coordinates
- `FromAbsolutePosition()` and `ToAbsoluteCenter()` methods

### 2. Floating Origin Support
The system tracks positions in two coordinate spaces:

- **Absolute Space**: Infinite world coordinates using `Vector2D` (double-precision)
- **Unity Space**: Current Unity world coordinates (shifted back to origin periodically)

```csharp
// WorldGenerationService provides conversion:
Vector2D absolutePos = service.WorldToAbsolute(unityPosition);
Vector2 unityPos = service.AbsoluteToWorld(absolutePosition);
```

**Origin Shift Flow:**
1. Camera exceeds `floatingOriginLimit` (default 2560 units)
2. `WorldGenerationService.PerformOriginShift()` calculates offset
3. `OnOriginShift` event fires with shift amount
4. All consumers update their runtime objects' positions
5. External systems (entities, camera) must subscribe and shift themselves

### 3. Chunk Lifecycle
```
Unloaded → Loading → Loaded → Unloading → Unloaded
              ↓
        Generators run
              ↓
        Consumers notified
```

- **Load Radius**: Chunks within this distance are loaded (default 3)
- **Unload Radius**: Chunks beyond this distance are unloaded (default 5)
- **LRU Eviction**: When `MaxLoadedChunks` exceeded, oldest accessed chunks removed

### 4. Generator/Consumer Pattern
**Generators** create data definitions (serializable, position-relative):
```csharp
public interface IChunkDataGenerator
{
    int Priority { get; }           // Lower = runs first
    Type DataType { get; }          // e.g., typeof(NebulaChunkData)
    void Generate(Chunk chunk, ChunkGenerationContext context);
    void OnChunkUnloading(Chunk chunk);
}
```

**Consumers** create runtime objects from definitions:
```csharp
public interface IChunkDataConsumer
{
    Type DataType { get; }
    void OnChunkLoaded(Chunk chunk);
    void OnChunkUnloading(Chunk chunk);
    void OnOriginShift(Vector2 offset);
    void Update(float deltaTime);
}
```

### 5. Nebula Integration
The `NebulaRegionConsumer` bridges to the existing `NebulaRegionManager`:

```csharp
// On chunk load:
foreach (var definition in data.Regions)
{
    Vector2 worldPos = chunkCenter + definition.LocalPosition;
    var region = _regionManager.CreateRegion(worldPos, definition.Radius, definition.Config);
    data.RuntimeRegions.Add(region);
}

// On chunk unload:
foreach (var region in data.RuntimeRegions)
    _regionManager.DestroyRegion(region);

// On origin shift:
foreach (var region in _regionManager.GetAllRegions())
    region.WorldPosition += offset;
```

## Configuration

### WorldGenerationConfig (ScriptableObject)
```csharp
float chunkSize = 500f;              // World units per chunk
float worldSeed = 0f;                // 0 = random on start
float floatingOriginLimit = 2560f;   // Distance to trigger origin shift
int loadRadius = 3;                  // Chunks to keep loaded
int unloadRadius = 5;                // Distance to unload
int chunksPerFrame = 2;              // Load/unload budget
int maxLoadedChunks = 100;           // Memory limit
NebulaGenerationConfig nebulaConfig; // Reference to nebula settings
```

### NebulaGenerationConfig (ScriptableObject)
```csharp
float densityNoiseScale = 0.001f;    // Noise scale for density field
float minDensityThreshold = 0.3f;    // Min density to spawn nebulas
float minNebulaSpacing = 100f;       // Poisson disk min distance
float minRadius = 30f;               // Nebula size range
float maxRadius = 150f;
List<WeightedNebulaConfig> nebulaConfigs; // Weighted config selection
```

## Setup Instructions

1. **Create ScriptableObjects:**
   - Right-click → Create → Starfire → World → **World Generation Config**
   - Right-click → Create → Starfire → World → **Nebula Generation Config**

2. **Configure Nebula Generation:**
   - Add existing `NebulaRegionConfig` assets to the weighted configs list
   - Set weights and density thresholds for variety

3. **Add to Scene:**
   - Create GameObject with `WorldGenerationService` component
   - Assign WorldGenerationConfig
   - Ensure `NebulaRegionManager` exists in scene

4. **Integrate Floating Origin:**
   ```csharp
   // Option A: Let WorldGenerationService handle it automatically
   // (shifts consumers, fires OnOriginShift event)

   // Option B: Handle externally and notify the service
   WorldGenerationService.Instance.NotifyOriginShift(shiftAmount);
   ```

## Extending the System

### Adding a New Content Type (e.g., Asteroids)

1. **Create Data Class:**
   ```csharp
   public class AsteroidChunkData : ChunkData
   {
       public List<AsteroidDefinition> Asteroids { get; } = new();
       public List<AsteroidInstance> RuntimeInstances { get; } = new();
   }
   ```

2. **Create Generator:**
   ```csharp
   public class AsteroidChunkGenerator : IChunkDataGenerator
   {
       public int Priority => 15;  // After nebulas (10)
       public Type DataType => typeof(AsteroidChunkData);

       public void Generate(Chunk chunk, ChunkGenerationContext context)
       {
           var data = new AsteroidChunkData();
           // Use PoissonDiskSampler, context.Random, etc.
           chunk.SetData(data);
       }
   }
   ```

3. **Create Consumer:**
   ```csharp
   public class AsteroidChunkConsumer : IChunkDataConsumer
   {
       public Type DataType => typeof(AsteroidChunkData);

       public void OnChunkLoaded(Chunk chunk)
       {
           var data = chunk.GetData<AsteroidChunkData>();
           // Instantiate asteroid GameObjects
       }

       public void OnOriginShift(Vector2 offset)
       {
           // Shift all asteroid transforms
       }
   }
   ```

4. **Register with Service:**
   ```csharp
   WorldGenerationService.Instance.RegisterGenerator(new AsteroidChunkGenerator(...));
   WorldGenerationService.Instance.RegisterConsumer(new AsteroidChunkConsumer());
   ```

## Integration with Existing Systems

### NebulaRegionManager API Used
- `CreateRegion(Vector2 position, float radius, NebulaRegionConfig config)` - Line 236
- `DestroyRegion(NebulaRegion region)` - Line 259
- `GetAllRegions()` - Line 285

### Events to Subscribe To
```csharp
// Chunk lifecycle
WorldGenerationService.Instance.OnChunkGenerated += (chunk) => { };
WorldGenerationService.Instance.OnChunkDestroyed += (chunk) => { };

// Floating origin (for entities, camera, etc.)
WorldGenerationService.Instance.OnOriginShift += (offset) => {
    // Shift all your GameObjects by offset
    transform.position += (Vector3)offset;
};
```

## Future Enhancements (Planned)

1. **Voronoi/Poisson Disk Biomes**: Use the sampling system to create distinct biome regions with different nebula styles
2. **Asteroid Generation**: Add asteroid fields using the extensible generator/consumer pattern
3. **POI Generation**: Stations, derelicts, anomalies
4. **Entity Spawners**: AI ships, hazards tied to chunk lifecycle

## Testing

1. **Enable Debug Gizmos**: Set `enableDebugGizmos = true` in WorldGenerationConfig
2. **Enable Logging**: Set `logChunkEvents = true` to see chunk lifecycle in console
3. **Verify Chunks**: Move camera around, watch chunks load/unload in scene view
4. **Test Origin Shift**: Move far from origin (>2560 units), verify nebulas stay in place visually

## Known Considerations

- **NebulaRegionManager.maxVisibleRegions** (8) still applies - only 8 nebulas render at once regardless of how many are loaded
- Chunk size should be larger than typical nebula radius to avoid nebulas spanning many chunks
- The system uses `Mathf.PerlinNoise` for density fields - consider upgrading to better noise for more varied distributions

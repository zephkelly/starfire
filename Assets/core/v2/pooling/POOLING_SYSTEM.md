# Projectile Pooling System

This document describes the global object pooling system for projectiles in Starfire.

## Overview

The pooling system eliminates GC allocations during gameplay by reusing projectile GameObjects instead of instantiating and destroying them. All entities in the game share the same pools, which dynamically size based on weapon usage.

## Architecture

```
ProjectilePoolManager (Singleton)
    ├── Physics Pools (Dictionary<prefabId, PrefabPool>)
    ├── Raycast Pools (Dictionary<prefabId, PrefabPool>)
    └── Hitscan Beam Pool (single shared pool)
```

## Files

| File | Purpose |
|------|---------|
| `IPoolable.cs` | Interface for poolable objects |
| `PrefabPool.cs` | Generic Stack-based pool for GameObjects |
| `ProjectilePoolConfig.cs` | ScriptableObject for pool settings |
| `ProjectilePoolManager.cs` | Singleton manager for all pools |

## Setup

1. **Add ProjectilePoolManager to scene**: Create an empty GameObject and add the `ProjectilePoolManager` component.

2. **Create config asset** (optional): Create a `ProjectilePoolConfig` asset via `Create > StarfireV2 > Pooling > ProjectilePoolConfig` and assign it to the manager.

3. **Ensure prefabs have V2Projectile**: Projectile prefabs should have the `V2Projectile` component attached.

## How It Works

### Pool Lifecycle

1. **First use**: When a weapon fires for the first time, the spawner checks if a pool exists for that prefab. If not, one is created with `defaultInitialSize` instances.

2. **Get from pool**: `ProjectilePoolManager.GetPhysicsProjectile(prefab)` returns an inactive GameObject from the pool, or creates a new one if empty.

3. **Return to pool**: When a projectile expires (lifetime) or hits something (destroyOnHit), it calls `ReturnToPool()` which returns it to the appropriate pool.

4. **Reset state**: On return, `IPoolable.OnPoolReturn()` is called to reset all state (velocity, damage, owner, etc.).

### Projectile Types

| Type | Pool Strategy |
|------|--------------|
| Physics | One pool per prefab, keyed by `prefab.GetInstanceID()` |
| RaycastBacked | One pool per prefab, keyed by `prefab.GetInstanceID()` |
| Hitscan | Shared beam visual pool with LineRenderer |

## Configuration

```csharp
[CreateAssetMenu(fileName = "ProjectilePoolConfig", menuName = "StarfireV2/Pooling/ProjectilePoolConfig")]
public class ProjectilePoolConfig : ScriptableObject
{
    public int defaultInitialSize = 20;      // Initial pool size per prefab
    public int expansionStep = 10;           // Instances added when pool is empty
    public int maxPoolSize = 500;            // Max per prefab (0 = unlimited)
    public int projectilesPerWeapon = 10;    // Base pool size per weapon
    public float fireRateMultiplier = 2f;    // Pool = fireRate * lifetime * this
    public int hitscanBeamPoolSize = 30;     // Hitscan beam pool size
    public bool logPoolEvents = false;       // Debug logging
}
```

## Dynamic Sizing

Pools can be pre-warmed when entities spawn:

```csharp
// Register a weapon config to size pools appropriately
ProjectilePoolManager.Instance.RegisterWeaponConfig(
    projectilePrefab,
    fireRate,
    projectileLifetime,
    V2ProjectileMode.Physics
);
```

The formula for pool size calculation:
```
poolSize = (fireRate * lifetime * fireRateMultiplier) * entityCount
```

## Integration Points

### V2ProjectileSpawner

The spawner automatically uses pools when `ProjectilePoolManager.Instance` is available:

```csharp
// Physics projectiles
var projectileGO = ProjectilePoolManager.Instance?.GetPhysicsProjectile(prefab)
    ?? Object.Instantiate(prefab);

// Set source prefab for return
projectile.SetSourcePrefab(prefab);
```

### V2Projectile

Implements `IPoolable` with:
- `OnPoolGet()`: Resets `_isActive` flag, clears consumed state
- `OnPoolReturn()`: Resets all state (owner, damage, velocity, trail, etc.)
- `ReturnToPool()`: Called instead of `Destroy()` when projectile expires

### Backwards Compatibility

The system is fully backwards compatible:
- If no `ProjectilePoolManager` exists, direct `Instantiate`/`Destroy` is used
- Prefabs without `V2Projectile` component get it added dynamically (with pooling disabled)
- `DisablePooling()` can be called to force destruction instead of pool return

## Debug Features

Enable `showDebugInfo` on the manager to display runtime pool statistics:
- Active/pooled counts per prefab
- Total instances created
- Pool expansion events

Enable `logPoolEvents` in config for console logging of all pool operations.

## Best Practices

1. **Pre-warm pools**: Call `PrewarmPool()` at scene start for commonly used projectiles
2. **Use prefabs**: Ensure weapons reference prefab assets, not scene instances
3. **Trail renderers**: Include TrailRenderer on prefab; it's cleared automatically on pool return
4. **Avoid runtime modifications**: Don't add/remove components on pooled projectiles at runtime

## Scene Transitions

On scene unload:
- All active projectiles are returned to pools
- Pools are optionally shrunk based on `shrinkThreshold` config

## Troubleshooting

### Projectiles not returning to pool
- Check that `SetSourcePrefab()` was called after getting from pool
- Verify `ProjectilePoolManager.Instance` is not null
- Check console for pool log messages (enable `logPoolEvents`)

### Pool growing infinitely
- Ensure projectiles are returning to pool (not being destroyed)
- Check that `_sourcePrefab` is set correctly
- Verify prefab reference is the same instance (not a copy)

### Visual artifacts after pool return
- Ensure `OnPoolReturn()` resets all visual state
- Clear trails with `TrailRenderer.Clear()`
- Reset transform position/rotation/scale

---

*Last updated: Implementation session*

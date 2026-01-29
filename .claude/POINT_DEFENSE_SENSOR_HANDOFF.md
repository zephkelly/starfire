# Point Defense & Sensor System Migration Handoff

## Status: COMPLETED

---

## Summary

All four phases have been implemented:
1. **Lead Calculation Fixed** - Proper quadratic intercept solver accounting for shooter velocity
2. **Accuracy Configuration Added** - 0-100% scale with angular spread (default 72%)
3. **V2 Transponder Module Created** - Full migration with faction system
4. **V2 Sensor Module Created** - Dual detection (passive + transponder)
5. **Point Defense Integrated** - Optional sensor integration with physics fallback

---

## Files Created

### Transponder System (Assets/core/v2/entity/ship/module/type/comms/)
- [ITransponderModule.cs](../Assets/core/v2/entity/ship/module/type/comms/ITransponderModule.cs)
- [V2TransponderModule.cs](../Assets/core/v2/entity/ship/module/type/comms/V2TransponderModule.cs)
- [V2TransponderModuleConfig.cs](../Assets/core/v2/entity/ship/module/type/comms/V2TransponderModuleConfig.cs)
- [data/V2CommChannel.cs](../Assets/core/v2/entity/ship/module/type/comms/data/V2CommChannel.cs)
- [data/V2FactionData.cs](../Assets/core/v2/entity/ship/module/type/comms/data/V2FactionData.cs)
- [data/V2FactionRelationType.cs](../Assets/core/v2/entity/ship/module/type/comms/data/V2FactionRelationType.cs)
- [data/V2FactionRelationship.cs](../Assets/core/v2/entity/ship/module/type/comms/data/V2FactionRelationship.cs)
- [data/V2TransponderData.cs](../Assets/core/v2/entity/ship/module/type/comms/data/V2TransponderData.cs)

### Sensor System (Assets/core/v2/entity/ship/module/type/sensor/)
- [ISensorModule.cs](../Assets/core/v2/entity/ship/module/type/sensor/ISensorModule.cs)
- [V2SensorModule.cs](../Assets/core/v2/entity/ship/module/type/sensor/V2SensorModule.cs)
- [V2SensorModuleConfig.cs](../Assets/core/v2/entity/ship/module/type/sensor/V2SensorModuleConfig.cs)
- [data/V2DetectedEntity.cs](../Assets/core/v2/entity/ship/module/type/sensor/data/V2DetectedEntity.cs)
- [data/V2DetectedEntityType.cs](../Assets/core/v2/entity/ship/module/type/sensor/data/V2DetectedEntityType.cs)
- [data/V2DetectionLevel.cs](../Assets/core/v2/entity/ship/module/type/sensor/data/V2DetectionLevel.cs)
- [data/V2DetectionRangeConfig.cs](../Assets/core/v2/entity/ship/module/type/sensor/data/V2DetectionRangeConfig.cs)
- [data/V2SensorFilterConfig.cs](../Assets/core/v2/entity/ship/module/type/sensor/data/V2SensorFilterConfig.cs)

### Supporting Files
- [Assets/core/v2/entity/module/ModuleTier.cs](../Assets/core/v2/entity/module/ModuleTier.cs)

---

## Files Modified

### Point Defense
- [PointDefenseModule.cs](../Assets/core/v2/entity/ship/module/type/weapon/defensive/PointDefenseModule.cs)
  - Added `CalculateInterceptDirection()` - proper quadratic intercept solver
  - Added `ApplyAccuracySpread()` - accuracy-based angular deviation
  - Added sensor integration with `TryConnectToSensor()`, `UpdateSensorBasedTargeting()`
  - Added fallback to physics-based targeting when no sensor available

- [PointDefenseModuleConfig.cs](../Assets/core/v2/entity/ship/module/type/weapon/defensive/PointDefenseModuleConfig.cs)
  - Added `accuracyPercent` (default: 72%)
  - Added `maxSpreadAngle` (default: 15°)
  - Added `accuracyDecayOverRange` with `rangeAccuracyFalloff` curve
  - Added `useSensorIntegration` toggle

### Entity Registry
- [EntityRegistry.cs](../Assets/core/v2/entity/EntityRegistry.cs)
  - Added `GetAllEntities()` method
  - Added `GetInRangeWithTransponder()` method
  - Added `GetByFaction(V2FactionData)` method
  - Added `NotifyTransponderDataChanged()` method
  - Added `OnTransponderDataChanged` event

---

## Key Implementation Details

### Lead Calculation Fix

The new `CalculateInterceptDirection()` method properly solves the quadratic intercept equation:

```csharp
// Relative velocity depends on whether projectile inherits shooter velocity
Vector2 relativeVel = inheritVelocity
    ? (targetVelocity - shooterVelocity)  // Work in shooter's frame
    : targetVelocity;                      // Work in world frame

// Solve: |relativePos + relativeVel * t| = projectileSpeed * t
float a = Vector2.Dot(relativeVel, relativeVel) - projectileSpeed * projectileSpeed;
float b = 2f * Vector2.Dot(relativePos, relativeVel);
float c = Vector2.Dot(relativePos, relativePos);
// Quadratic formula for smallest positive t
```

### Accuracy System

Accuracy is applied as angular spread after calculating the perfect intercept:

```csharp
float effectiveAccuracy = _config.AccuracyPercent / 100f;
// Optional range decay
if (_config.AccuracyDecayOverRange)
    effectiveAccuracy *= _config.RangeAccuracyFalloff.Evaluate(rangeRatio);

float maxSpreadRadians = _config.MaxSpreadAngle * Mathf.Deg2Rad * (1f - effectiveAccuracy);
float randomAngle = Random.Range(-maxSpreadRadians, maxSpreadRadians);
// Rotate direction by random angle
```

### Dual Detection System

Sensors implement passive + transponder detection:

**With Active Transponder:**
- Far range (50-100%): Silhouette level (faction, class visible)
- Close range (0-50%): Full level (complete data)

**Without Transponder (Passive):**
- Far range (50-100%): Presence only (just a blip)
- Medium range (20-50%): Silhouette (shape detection)
- Close range (0-20%): Full (visual identification)

### Sensor Integration

Point defense can now use sensor data:

```csharp
// In OnAttach
if (_config.UseSensorIntegration)
    TryConnectToSensor();

// In OnUpdate
if (_usingSensorData && _sensorModule != null)
    UpdateSensorBasedTargeting();  // Use sensor threats
else
    ScanForThreats();              // Fall back to physics
```

---

## Progress Tracking

- [x] **Phase 1: Lead Calculation**
  - [x] Implement quadratic intercept solver
  - [x] Add shooter velocity to calculation
  - [x] Handle `inheritVelocity` flag
  - [x] Add accuracy configuration

- [x] **Phase 2: Transponder Migration**
  - [x] Create v2 interfaces (ITransponderModule)
  - [x] Port data types (V2TransponderData, V2CommChannel, V2FactionData)
  - [x] Implement V2TransponderModule
  - [x] Create ScriptableObject config

- [x] **Phase 3: Sensor Migration**
  - [x] Create v2 interfaces (ISensorModule)
  - [x] Port data types (V2DetectedEntity, V2DetectionLevel, configs)
  - [x] Implement V2SensorModule with dual detection
  - [x] Create ScriptableObject config
  - [x] Add threat detection events

- [x] **Phase 4: Integration**
  - [x] Add sensor reference to PointDefenseModule
  - [x] Subscribe to sensor threat events
  - [x] Implement sensor-based targeting
  - [x] Add fallback to physics queries

---

## Testing Checklist

### Lead Calculation
- [ ] Stationary shooter, moving target perpendicular - should hit
- [ ] Moving shooter toward target - should hit
- [ ] Moving shooter away from target - should hit
- [ ] High-speed chase scenario - should hit
- [ ] Test with `inheritVelocity = true` and `false`

### Accuracy
- [ ] 100% accuracy - perfect hits
- [ ] 72% accuracy (default) - occasional misses
- [ ] 0% accuracy - misses within spread angle
- [ ] Range decay enabled - accuracy drops at distance

### Sensor Detection
- [ ] Entities at max range appear at Presence level
- [ ] Transponder on: Silhouette at 50%, Full at 20%
- [ ] Transponder off: Presence far, Silhouette close
- [ ] Missiles/projectiles detected as threats

### Integration
- [ ] PD engages threats from sensor data
- [ ] PD falls back to physics if no sensor
- [ ] Sensor events properly unsubscribed on detach

---

## Usage Examples

### Creating a Sensor Module Config

1. Right-click in Project window
2. Create > StarfireV2 > Modules > Sensor
3. Configure range settings, polling interval, threat layers

### Creating a Transponder Module Config

1. Right-click in Project window
2. Create > StarfireV2 > Modules > Transponder
3. Assign default faction, channels, crew complement

### Creating a Faction

1. Right-click in Project window
2. Create > StarfireV2 > Factions > Faction Data
3. Set faction ID, name, color, icon
4. Add relationships to other factions

---

## Architecture Notes

### Module Tier System
All modules use the `ModuleTier` enum which provides multipliers:
- Basic: 0.75x effectiveness
- Standard: 1.0x effectiveness
- Advanced: 1.25x effectiveness

### Detection Levels
```csharp
V2DetectionLevel.None      // Not detected
V2DetectionLevel.Presence  // Basic blip (passive detection)
V2DetectionLevel.Silhouette // Faction/class visible
V2DetectionLevel.Full      // Complete transponder data
```

### Entity Types
```csharp
V2DetectedEntityType.Ship       // Standard ships
V2DetectedEntityType.Projectile // Bullets, lasers
V2DetectedEntityType.Missile    // Guided missiles
V2DetectedEntityType.Torpedo    // Heavy torpedoes
// IsThreat returns true for Projectile and above (value >= 10)
```

---

## Next Steps

1. **Create ScriptableObject assets** for testing (sensor, transponder, faction configs)
2. **Add sensor/transponder slots** to ShipController's module configuration
3. **Test in-game** with moving targets and point defense
4. **Tune accuracy values** based on gameplay feel
5. **Add UI** for sensor contacts display
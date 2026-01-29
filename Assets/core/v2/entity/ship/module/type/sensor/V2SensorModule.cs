using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Implementation of a sensor module that detects entities and threats.
    /// Uses dual detection: passive (always active) and transponder-enhanced.
    /// </summary>
    public class V2SensorModule : ISensorModule
    {
        private readonly V2SensorModuleConfig _config;
        private IEntityController _controller;
        private ITransponderModule _ownTransponder;

        private readonly List<V2DetectedEntity> _detectedEntities = new();
        private readonly HashSet<int> _previousEntityIds = new();
        private float _lastPollTime;
        private float _tierMultiplier;

        // IEntityModule
        public string ModuleId => _config.ModuleId;
        public bool IsEnabled { get; set; } = true;

        // IShipModule
        public ShipModuleCategory Category => ShipModuleCategory.Sensor;
        public ShipModuleType Type => ShipModuleType.SensorArray;

        // ISensorModule
        public float DetectionRange => _config.RangeConfig.MaxRange * _tierMultiplier;
        public float SilhouetteRange => _config.RangeConfig.SilhouetteRange * _tierMultiplier;
        public float FullRange => _config.RangeConfig.FullRange * _tierMultiplier;
        public float TargetingAccuracy => _config.TargetingAccuracy;
        public float PollingRate => _config.PollingInterval;

        public IReadOnlyList<V2DetectedEntity> DetectedEntities => _detectedEntities;
        public int DetectedCount => _detectedEntities.Count;

        public event Action<V2DetectedEntity> OnEntityDetected;
        public event Action<V2DetectedEntity> OnEntityLost;
        public event Action<V2DetectedEntity> OnThreatDetected;

        public V2SensorModule(V2SensorModuleConfig config)
        {
            _config = config;
            _tierMultiplier = config.Tier switch
            {
                ModuleTier.Basic => 0.75f,
                ModuleTier.Standard => 1.0f,
                ModuleTier.Advanced => 1.25f,
                _ => 1.0f
            };
        }

        public void OnAttach(IEntityController controller)
        {
            _controller = controller;

            // Subscribe to entity registry events
            if (EntityRegistry.Instance != null)
            {
                EntityRegistry.Instance.OnEntityUnregistered += OnRegistryEntityRemoved;
            }
        }

        public void OnDetach()
        {
            if (EntityRegistry.Instance != null)
            {
                EntityRegistry.Instance.OnEntityUnregistered -= OnRegistryEntityRemoved;
            }

            _controller = null;
            _ownTransponder = null;
            _detectedEntities.Clear();
            _previousEntityIds.Clear();
        }

        public void OnUpdate(float deltaTime)
        {
            if (!IsEnabled || _controller == null) return;

            if (Time.time - _lastPollTime >= PollingRate)
            {
                RefreshNow();
                _lastPollTime = Time.time;
            }
        }

        public void RefreshNow()
        {
            if (_controller == null) return;

            Vector2 position = _controller.Transform.position;
            float effectiveRange = _config.RangeConfig.MaxRange * _tierMultiplier;

            // Cache own transponder
            _ownTransponder ??= GetOwnTransponder();
            V2FactionData ownFaction = _ownTransponder?.Faction;

            // Store previous entity IDs for change detection
            _previousEntityIds.Clear();
            foreach (var entity in _detectedEntities)
            {
                if (entity.Transform != null)
                {
                    _previousEntityIds.Add(entity.Transform.GetInstanceID());
                }
            }

            _detectedEntities.Clear();

            // Scan for entities using the entity registry
            ScanEntities(position, effectiveRange, ownFaction);

            // Scan for threats using physics
            ScanThreats(position, effectiveRange);
        }

        private void ScanEntities(Vector2 position, float effectiveRange, V2FactionData ownFaction)
        {
            if (EntityRegistry.Instance == null) return;

            foreach (var registeredEntity in EntityRegistry.Instance.GetAllEntities())
            {
                if (registeredEntity == _controller) continue;
                if (registeredEntity.Transform == null) continue;

                float distance = Vector2.Distance(position, registeredEntity.Transform.position);
                if (distance > effectiveRange) continue;

                // Get transponder if available
                var transponder = GetTransponderFromController(registeredEntity);
                bool hasActiveTransponder = transponder?.IsTransmitting ?? false;

                // Determine detection level based on transponder status and distance
                V2DetectionLevel level;
                if (hasActiveTransponder)
                {
                    level = _config.RangeConfig.GetLevelForDistance(distance, _tierMultiplier);
                }
                else
                {
                    level = _config.RangeConfig.GetPassiveLevelForDistance(distance, _tierMultiplier);
                }

                if (level == V2DetectionLevel.None) continue;

                // Apply filters if we have faction info
                if (transponder != null && _config.FilterConfig != null)
                {
                    if (!_config.FilterConfig.PassesFilter(transponder.Faction, transponder.ShipClass, ownFaction))
                        continue;
                }

                // Create detected entity
                var detected = new V2DetectedEntity
                {
                    Transform = registeredEntity.Transform,
                    Rigidbody = registeredEntity.Rigid2D,
                    EntityType = DetermineEntityType(registeredEntity),
                    Level = level,
                    Distance = distance,
                    LastUpdateTime = Time.time,
                    HasActiveTransponder = hasActiveTransponder
                };

                // Set faction data if available and level is sufficient
                if (transponder != null)
                {
                    detected.SetFactionData(transponder.Faction, transponder.ShipClass?.ClassName ?? "Unknown");

                    if (level >= V2DetectionLevel.Full)
                    {
                        detected.SetFullData(transponder.GetTransponderData());
                    }
                }

                _detectedEntities.Add(detected);

                // Fire detection event if new
                int instanceId = registeredEntity.Transform.GetInstanceID();
                if (!_previousEntityIds.Contains(instanceId))
                {
                    OnEntityDetected?.Invoke(detected);
                }
                _previousEntityIds.Remove(instanceId);
            }

            // Fire lost events for entities no longer detected
            foreach (int lostId in _previousEntityIds)
            {
                OnEntityLost?.Invoke(new V2DetectedEntity { Level = V2DetectionLevel.None });
            }
        }

        private void ScanThreats(Vector2 position, float effectiveRange)
        {
            if (_config.ThreatLayers == 0) return;

            var results = Physics2D.OverlapCircleAll(position, effectiveRange, _config.ThreatLayers);

            foreach (var collider in results)
            {
                if (collider == null) continue;

                // Skip if owned by us
                if (_controller != null && collider.transform.IsChildOf(_controller.Transform)) continue;

                float distance = Vector2.Distance(position, collider.transform.position);

                var threat = V2DetectedEntity.CreateThreat(
                    collider.transform,
                    collider.attachedRigidbody,
                    DetermineThreatType(collider),
                    distance
                );

                _detectedEntities.Add(threat);
                OnThreatDetected?.Invoke(threat);
            }
        }

        private ITransponderModule GetOwnTransponder()
        {
            // Try to get transponder from ship entity
            if (_controller is ShipController shipController)
            {
                return shipController.Ship?.Modules
                    ?.GetAllModulesOfType<ITransponderModule>()
                    ?.FirstOrDefault();
            }
            return null;
        }

        private ITransponderModule GetTransponderFromController(IEntityController controller)
        {
            if (controller is ShipController shipController)
            {
                return shipController.Ship?.Modules
                    ?.GetAllModulesOfType<ITransponderModule>()
                    ?.FirstOrDefault();
            }
            return null;
        }

        private V2DetectedEntityType DetermineEntityType(IEntityController controller)
        {
            // TODO: Expand this based on entity type when more entity types are added
            if (controller is ShipController)
                return V2DetectedEntityType.Ship;

            return V2DetectedEntityType.Unknown;
        }

        private V2DetectedEntityType DetermineThreatType(Collider2D collider)
        {
            // Check by tag first (most flexible approach)
            if (collider.CompareTag("Missile"))
                return V2DetectedEntityType.Missile;
            if (collider.CompareTag("Torpedo"))
                return V2DetectedEntityType.Torpedo;
            if (collider.CompareTag("Mine"))
                return V2DetectedEntityType.Mine;

            // Check for standard projectile component
            var projectile = collider.GetComponent<V2Projectile>();
            if (projectile != null)
                return V2DetectedEntityType.Projectile;

            // Default to projectile for unknown threats on threat layers
            return V2DetectedEntityType.Projectile;
        }

        private void OnRegistryEntityRemoved(IEntityController entity)
        {
            for (int i = _detectedEntities.Count - 1; i >= 0; i--)
            {
                if (_detectedEntities[i].Transform == entity?.Transform)
                {
                    var removed = _detectedEntities[i];
                    _detectedEntities.RemoveAt(i);
                    OnEntityLost?.Invoke(removed);
                    break;
                }
            }
        }

        // Query methods
        public IEnumerable<V2DetectedEntity> GetHostileEntities()
        {
            if (_ownTransponder?.Faction == null)
                return Enumerable.Empty<V2DetectedEntity>();

            return _detectedEntities.Where(e =>
            {
                var faction = e.Faction;
                if (faction == null) return false;
                return _ownTransponder.Faction.IsHostileTo(faction);
            });
        }

        public IEnumerable<V2DetectedEntity> GetAlliedEntities()
        {
            if (_ownTransponder?.Faction == null)
                return Enumerable.Empty<V2DetectedEntity>();

            return _detectedEntities.Where(e =>
            {
                var faction = e.Faction;
                if (faction == null) return false;
                return _ownTransponder.Faction.IsAlliedWith(faction);
            });
        }

        public IEnumerable<V2DetectedEntity> GetEntitiesByFaction(V2FactionData faction)
        {
            if (faction == null)
                return Enumerable.Empty<V2DetectedEntity>();

            return _detectedEntities.Where(e => e.Faction == faction);
        }

        public IEnumerable<V2DetectedEntity> GetEntitiesAtLevel(V2DetectionLevel minLevel)
        {
            return _detectedEntities.Where(e => e.Level >= minLevel);
        }

        public IEnumerable<V2DetectedEntity> GetDetectedThreats()
        {
            return _detectedEntities.Where(e => e.IsThreat);
        }

        public IEnumerable<V2DetectedEntity> GetThreatsInRange(float range)
        {
            return _detectedEntities.Where(e => e.IsThreat && e.Distance <= range);
        }
    }
}

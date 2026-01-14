using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Starfire.Entity.Modules.Transponder;

namespace Starfire.Entity.Modules.Sensor
{
    public class BasicSensorModule : ISensorModule
    {
        private readonly BasicSensorConfig _config;
        private EntityControllerBase _controller;
        private ITransponderModule _ownTransponder;

        private readonly List<DetectedEntity> _detectedEntities = new();
        private readonly HashSet<int> _previousEntityIds = new();
        private float _lastPollTime;
        private float _tierMultiplier;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public float DetectionRange => _config.DetectionRange;
        public float TargetingAccuracy => _config.TargetingAccuracy;
        public float PollingRate => _config.EffectivePollingRate;

        public IReadOnlyList<DetectedEntity> DetectedEntities => _detectedEntities;
        public int DetectedCount => _detectedEntities.Count;

        public event Action<DetectedEntity> OnEntityDetected;
        public event Action<DetectedEntity> OnEntityLost;

        public BasicSensorModule(BasicSensorConfig config)
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

        public void OnAttach(EntityControllerBase controller)
        {
            _controller = controller;

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
            if (_controller == null || EntityRegistry.Instance == null) return;

            Vector2 position = _controller.transform.position;
            float effectiveRange = _config.RangeConfig.maxRange * _tierMultiplier;

            _ownTransponder ??= GetOwnTransponder();
            FactionData ownFaction = _ownTransponder?.Faction;

            _previousEntityIds.Clear();
            foreach (var entity in _detectedEntities)
            {
                if (entity.Controller != null)
                {
                    _previousEntityIds.Add(entity.Controller.GetInstanceID());
                }
            }

            _detectedEntities.Clear();

            var candidates = EntityRegistry.Instance.GetInRangeWithTransponder(position, effectiveRange);

            foreach (var entity in candidates)
            {
                if (entity == _controller) continue;

                float distance = Vector2.Distance(position, entity.transform.position);
                DetectionLevel level = _config.RangeConfig.GetLevelForDistance(distance, _tierMultiplier);

                if (level == DetectionLevel.None) continue;

                var transponder = entity.Systems?.GetAllModulesOfType<ITransponderModule>().FirstOrDefault();
                if (transponder == null) continue;

                if (!_config.FilterConfig.PassesFilter(transponder.Faction, transponder.ShipClass, ownFaction))
                    continue;

                var detected = new DetectedEntity
                {
                    Controller = entity,
                    Level = level,
                    Distance = distance,
                    LastUpdateTime = Time.time
                };

                _detectedEntities.Add(detected);

                int instanceId = entity.GetInstanceID();
                if (!_previousEntityIds.Contains(instanceId))
                {
                    OnEntityDetected?.Invoke(detected);
                }

                _previousEntityIds.Remove(instanceId);
            }

            foreach (int lostId in _previousEntityIds)
            {
                OnEntityLost?.Invoke(new DetectedEntity { Controller = null, Level = DetectionLevel.None });
            }
        }

        public IEnumerable<DetectedEntity> GetHostileEntities()
        {
            if (_ownTransponder?.Faction == null)
                return Enumerable.Empty<DetectedEntity>();

            return _detectedEntities.Where(e =>
            {
                var faction = e.Faction;
                if (faction == null) return false;
                return _ownTransponder.Faction.IsHostileTo(faction);
            });
        }

        public IEnumerable<DetectedEntity> GetAlliedEntities()
        {
            if (_ownTransponder?.Faction == null)
                return Enumerable.Empty<DetectedEntity>();

            return _detectedEntities.Where(e =>
            {
                var faction = e.Faction;
                if (faction == null) return false;
                return _ownTransponder.Faction.IsAlliedWith(faction);
            });
        }

        public IEnumerable<DetectedEntity> GetEntitiesByFaction(FactionData faction)
        {
            if (faction == null)
                return Enumerable.Empty<DetectedEntity>();

            return _detectedEntities.Where(e => e.Faction == faction);
        }

        public IEnumerable<DetectedEntity> GetEntitiesAtLevel(DetectionLevel minLevel)
        {
            return _detectedEntities.Where(e => e.Level >= minLevel);
        }

        private void OnRegistryEntityRemoved(EntityControllerBase entity)
        {
            for (int i = _detectedEntities.Count - 1; i >= 0; i--)
            {
                if (_detectedEntities[i].Controller == entity)
                {
                    var removed = _detectedEntities[i];
                    _detectedEntities.RemoveAt(i);
                    OnEntityLost?.Invoke(removed);
                    break;
                }
            }
        }

        private ITransponderModule GetOwnTransponder()
        {
            return _controller?.Systems?.GetAllModulesOfType<ITransponderModule>().FirstOrDefault();
        }
    }
}

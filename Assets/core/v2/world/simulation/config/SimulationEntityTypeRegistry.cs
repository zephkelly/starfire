using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World.Simulation.Behaviors;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation.Config
{
    /// <summary>
    /// Runtime registry that loads and provides access to all SimulationEntityTypeConfig assets.
    /// Provides O(1) lookup by EntityType and caches behavior instances.
    /// </summary>
    public class SimulationEntityTypeRegistry
    {
        private readonly Dictionary<EntityType, SimulationEntityTypeConfig> _configsByType = new();
        private readonly Dictionary<SimulatedBehaviorType, ISimulationBehavior> _behaviorCache = new();
        private readonly List<SimulationEntityTypeConfig> _allConfigs = new();

        private bool _initialized = false;

        /// <summary>
        /// All registered entity type configurations.
        /// </summary>
        public IReadOnlyList<SimulationEntityTypeConfig> AllConfigs => _allConfigs;

        /// <summary>
        /// Number of registered entity types.
        /// </summary>
        public int Count => _allConfigs.Count;

        /// <summary>
        /// Whether the registry has been initialized.
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Initialize the registry by loading all SimulationEntityTypeConfig assets from Resources.
        /// </summary>
        /// <param name="resourcePath">Path within Resources to search for configs. Default: "Simulation/EntityTypes"</param>
        public void Initialize(string resourcePath = "Simulation/EntityTypes")
        {
            if (_initialized)
            {
                Debug.LogWarning("[SimulationEntityTypeRegistry] Already initialized. Call Clear() first to reinitialize.");
                return;
            }

            _configsByType.Clear();
            _behaviorCache.Clear();
            _allConfigs.Clear();

            // Load all configs from Resources
            var configs = Resources.LoadAll<SimulationEntityTypeConfig>(resourcePath);

            if (configs.Length == 0)
            {
                Debug.LogWarning($"[SimulationEntityTypeRegistry] No SimulationEntityTypeConfig assets found in Resources/{resourcePath}. " +
                                 "Create entity type configs and place them in that folder.");
            }

            foreach (var config in configs)
            {
                RegisterConfig(config);
            }

            // Also try loading from a fallback path
            if (configs.Length == 0)
            {
                configs = Resources.LoadAll<SimulationEntityTypeConfig>("");
                foreach (var config in configs)
                {
                    if (!_configsByType.ContainsKey(config.EntityType))
                    {
                        RegisterConfig(config);
                    }
                }
            }

            _initialized = true;

            Debug.Log($"[SimulationEntityTypeRegistry] Initialized with {_allConfigs.Count} entity type configs.");
        }

        /// <summary>
        /// Initialize from a provided list of configs (useful for testing or manual setup).
        /// </summary>
        /// <param name="configs">List of configs to register.</param>
        public void InitializeFromList(IEnumerable<SimulationEntityTypeConfig> configs)
        {
            if (_initialized)
            {
                Debug.LogWarning("[SimulationEntityTypeRegistry] Already initialized. Call Clear() first to reinitialize.");
                return;
            }

            _configsByType.Clear();
            _behaviorCache.Clear();
            _allConfigs.Clear();

            foreach (var config in configs)
            {
                RegisterConfig(config);
            }

            _initialized = true;

            Debug.Log($"[SimulationEntityTypeRegistry] Initialized from list with {_allConfigs.Count} entity type configs.");
        }

        /// <summary>
        /// Register a single entity type configuration.
        /// </summary>
        /// <param name="config">The configuration to register.</param>
        public void RegisterConfig(SimulationEntityTypeConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("[SimulationEntityTypeRegistry] Attempted to register null config.");
                return;
            }

            if (_configsByType.ContainsKey(config.EntityType))
            {
                Debug.LogWarning($"[SimulationEntityTypeRegistry] Duplicate config for EntityType {config.EntityType}. " +
                                 $"Keeping existing config '{_configsByType[config.EntityType].DisplayName}', " +
                                 $"ignoring '{config.DisplayName}'.");
                return;
            }

            _configsByType[config.EntityType] = config;
            _allConfigs.Add(config);

            // Pre-cache default behavior if available
            if (config.DefaultBehavior != null)
            {
                GetOrCreateBehavior(config.DefaultBehavior);
            }

            Debug.Log($"[SimulationEntityTypeRegistry] Registered config for {config.EntityType}: {config.DisplayName}");
        }

        /// <summary>
        /// Get the configuration for an entity type.
        /// </summary>
        /// <param name="type">The entity type to look up.</param>
        /// <returns>The configuration, or null if not registered.</returns>
        public SimulationEntityTypeConfig GetConfig(EntityType type)
        {
            _configsByType.TryGetValue(type, out var config);
            return config;
        }

        /// <summary>
        /// Try to get the configuration for an entity type.
        /// </summary>
        /// <param name="type">The entity type to look up.</param>
        /// <param name="config">The found configuration.</param>
        /// <returns>True if found.</returns>
        public bool TryGetConfig(EntityType type, out SimulationEntityTypeConfig config)
        {
            return _configsByType.TryGetValue(type, out config);
        }

        /// <summary>
        /// Check if a configuration exists for an entity type.
        /// </summary>
        /// <param name="type">The entity type to check.</param>
        /// <returns>True if a config exists.</returns>
        public bool HasConfig(EntityType type)
        {
            return _configsByType.ContainsKey(type);
        }

        /// <summary>
        /// Get a cached behavior instance by type, creating it if necessary.
        /// </summary>
        /// <param name="behaviorType">The behavior type.</param>
        /// <returns>The behavior instance, or null if no config provides this behavior type.</returns>
        public ISimulationBehavior GetBehavior(SimulatedBehaviorType behaviorType)
        {
            if (_behaviorCache.TryGetValue(behaviorType, out var cached))
            {
                return cached;
            }

            // Search configs for a behavior config of this type
            foreach (var config in _allConfigs)
            {
                var behaviorConfig = config.GetBehaviorByType(behaviorType);
                if (behaviorConfig != null)
                {
                    return GetOrCreateBehavior(behaviorConfig);
                }
            }

            return null;
        }

        /// <summary>
        /// Get or create a behavior instance from a config.
        /// </summary>
        /// <param name="behaviorConfig">The behavior configuration.</param>
        /// <returns>The behavior instance.</returns>
        public ISimulationBehavior GetOrCreateBehavior(SimulationBehaviorConfig behaviorConfig)
        {
            if (behaviorConfig == null) return null;

            if (_behaviorCache.TryGetValue(behaviorConfig.BehaviorType, out var cached))
            {
                return cached;
            }

            var behavior = behaviorConfig.CreateBehavior();
            if (behavior != null)
            {
                _behaviorCache[behaviorConfig.BehaviorType] = behavior;
                Debug.Log($"[SimulationEntityTypeRegistry] Created behavior: {behaviorConfig.BehaviorType}");
            }

            return behavior;
        }

        /// <summary>
        /// Get the default behavior for an entity type.
        /// </summary>
        /// <param name="type">The entity type.</param>
        /// <returns>The default behavior, or null if not configured.</returns>
        public ISimulationBehavior GetDefaultBehavior(EntityType type)
        {
            var config = GetConfig(type);
            if (config?.DefaultBehavior == null) return null;

            return GetOrCreateBehavior(config.DefaultBehavior);
        }

        /// <summary>
        /// Clear all registered configs and cached behaviors.
        /// </summary>
        public void Clear()
        {
            _configsByType.Clear();
            _behaviorCache.Clear();
            _allConfigs.Clear();
            _initialized = false;

            Debug.Log("[SimulationEntityTypeRegistry] Cleared all registrations.");
        }

        /// <summary>
        /// Get all entity types that have registered configs.
        /// </summary>
        public IEnumerable<EntityType> GetRegisteredTypes()
        {
            return _configsByType.Keys;
        }

        /// <summary>
        /// Log the current state of the registry for debugging.
        /// </summary>
        public void LogState()
        {
            Debug.Log($"[SimulationEntityTypeRegistry] State: Initialized={_initialized}, " +
                      $"EntityTypes={_configsByType.Count}, CachedBehaviors={_behaviorCache.Count}");

            foreach (var kvp in _configsByType)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value.DisplayName} (Priority: {kvp.Value.Tier1Priority})");
            }
        }
    }
}

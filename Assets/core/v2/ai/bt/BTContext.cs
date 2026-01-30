using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    public class BTContext
    {
        public Transform Transform { get; }
        public AIEntityControllerDriver Driver { get; }
        public IEntityController Controller { get; }

        /// <summary>
        /// Convenience accessor for the ship entity's module collection.
        /// Returns null if the controller's entity is not a ShipEntity.
        /// </summary>
        public ShipModuleSlotCollection Modules => (Controller?.Entity as ShipEntity)?.Modules;

        /// <summary>
        /// Enable debug logging for this entity's behavior tree execution.
        /// Only active in UNITY_EDITOR builds.
        /// </summary>
        public bool DebugLogging { get; set; } = false;

        /// <summary>
        /// Entity name for log prefixing.
        /// </summary>
        public string EntityName => Controller?.Transform?.name ?? "Unknown";

        private readonly Dictionary<string, object> _blackboard = new();

        public BTContext(IEntityController controller, AIEntityControllerDriver driver)
        {
            Transform = controller.Transform;
            Driver = driver;
            Controller = controller;
        }

        public void Set<T>(string key, T value)
        {
            _blackboard[key] = value;
        }

        public T Get<T>(string key)
        {
            return (T)_blackboard[key];
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_blackboard.TryGetValue(key, out var obj) && obj is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        public bool Has(string key)
        {
            return _blackboard.ContainsKey(key);
        }

        public void Remove(string key)
        {
            _blackboard.Remove(key);
        }

        public void Clear()
        {
            _blackboard.Clear();
        }
    }
}

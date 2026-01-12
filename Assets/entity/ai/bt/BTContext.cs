using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    public class BTContext
    {
        public Transform Transform { get; }
        public AIDriver Driver { get; }
        public ShipController Controller { get; }

        // Lazy property - ShipSystems may not be available during OnAttach
        // because the AICore module is attached during ShipSystems construction
        public ShipSystems Systems => Controller?.ShipSystems;

        private readonly Dictionary<string, object> _blackboard = new();

        public BTContext(EntityControllerBase controller, AIDriver driver)
        {
            Transform = controller.transform;
            Driver = driver;
            Controller = controller as ShipController;
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

using System;
using UnityEngine;

namespace Starfire.Entity.Modules
{
    public class ModuleSlot<T> : IModuleSlot where T : class, IEntityModule
    {
        private T _module;
        private readonly EntityControllerBase _controller;

        public T Module => _module;
        public bool HasModule => _module != null;
        public IEntityModule ModuleBase => _module;

        public event Action<T> OnModuleChanged;

        public ModuleSlot(EntityControllerBase controller)
        {
            _controller = controller;
        }

        public void Equip(T newModule)
        {
            if (_module != null)
            {
                _module.OnDetach();
            }

            _module = newModule;

            if (_module != null)
            {
                _module.OnAttach(_controller);
            }

            OnModuleChanged?.Invoke(_module);
        }

        public void EquipFromConfig(ScriptableObject config)
        {
            if (config is IModuleConfig moduleConfig)
            {
                var module = moduleConfig.CreateModule();
                if (module is T typedModule)
                {
                    Equip(typedModule);
                }
                else
                {
                    Debug.LogWarning($"Module type mismatch: expected {typeof(T).Name}, got {module?.GetType().Name}");
                }
            }
            else
            {
                Debug.LogWarning($"Config {config?.GetType().Name} does not implement IModuleConfig");
            }
        }

        public void Unequip()
        {
            Equip(null);
        }

        public void Update(float deltaTime)
        {
            if (_module != null && _module.IsEnabled)
            {
                _module.OnUpdate(deltaTime);
            }
        }
    }
}

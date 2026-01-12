using System;
using UnityEngine;
using Starfire.Entity.Modules.Weapon;

namespace Starfire.Entity.Modules
{
    public class ModuleSlot<T> : IModuleSlot where T : class, IEntityModule
    {
        private T _module;
        private readonly EntityControllerBase _controller;
        private string _slotId;

        public T Module => _module;
        public bool HasModule => _module != null;
        public IEntityModule ModuleBase => _module;
        public string SlotId => _slotId;

        public event Action<T> OnModuleChanged;

        public ModuleSlot(EntityControllerBase controller)
        {
            _controller = controller;
        }

        public void SetSlotId(string slotId)
        {
            _slotId = slotId;
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

                // If this is a weapon module, try to assign hardpoint
                if (_module is IWeaponModule weaponModule && !string.IsNullOrEmpty(_slotId))
                {
                    TryAssignHardpoint(weaponModule);
                }
            }

            OnModuleChanged?.Invoke(_module);
        }

        private void TryAssignHardpoint(IWeaponModule weaponModule)
        {
            var registry = _controller.GetComponent<HardpointRegistry>();
            if (registry == null)
            {
                // No hardpoint registry, weapon will work without visual
                return;
            }

            if (!registry.IsInitialized)
            {
                Debug.LogWarning($"HardpointRegistry not initialized when equipping weapon to slot '{_slotId}'");
                return;
            }

            var hardpoint = registry.GetHardpoint(_slotId);
            if (hardpoint != null)
            {
                weaponModule.OnHardpointAssigned(hardpoint);
            }
            else
            {
                Debug.LogWarning($"No hardpoint found for slot '{_slotId}' - weapon will work without visual");
            }
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

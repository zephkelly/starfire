using System;
using UnityEngine;

namespace StarfireV2
{
    public class ShipModuleSlot<T> : IEntityModuleSlot where T : class, IEntityModule
    {
        private T _module;
        private readonly IEntityController _controller;
        private string _slotId;

        public T Module => _module;
        public bool HasModule => _module != null;
        public IEntityModule ModuleBase => _module;
        public string SlotId => _slotId;

        public event Action<T> OnModuleChanged;

        public ShipModuleSlot(IEntityController controller)
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
                // Unassign hardpoint if weapon module
                if (_module is IWeaponModule oldWeapon)
                {
                    oldWeapon.OnHardpointUnassigned();
                }

                _module.OnDetach();
            }

            _module = newModule;

            if (_module != null)
            {
                _module.OnAttach(_controller);

                // Assign hardpoint for weapon modules
                if (_module is IWeaponModule weapon && !string.IsNullOrEmpty(_slotId))
                {
                    var registry = _controller.Transform.GetComponent<V2HardpointRegistry>();
                    if (registry != null)
                    {
                        var hardpoint = registry.GetHardpoint(_slotId);
                        if (hardpoint != null)
                        {
                            weapon.OnHardpointAssigned(hardpoint);
                        }
                        else
                        {
                            Debug.LogWarning($"[ShipModuleSlot] No hardpoint found for slot '{_slotId}'. " +
                                             $"Available hardpoints: {string.Join(", ", registry.GetAllSlotIds())}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ShipModuleSlot] No V2HardpointRegistry found on '{_controller.Transform.name}'");
                    }
                }
            }

            OnModuleChanged?.Invoke(_module);
        }

        public void EquipFromConfig(ScriptableObject config)
        {
            if (config is IShipModuleConfig moduleConfig)
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

        public void EquipFromData(IModuleRuntimeData data)
        {
            if (data == null) return;

            // Find the config SO type that can create a module from this data
            // We need the config to call CreateModuleFromData
            // Since we don't have a direct reference, we use a registry approach
            var module = ModuleDataFactory.CreateModuleFromData(data);
            if (module is T typedModule)
            {
                Equip(typedModule);
            }
            else
            {
                Debug.LogWarning($"Module type mismatch from data: expected {typeof(T).Name}, got {module?.GetType().Name}");
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
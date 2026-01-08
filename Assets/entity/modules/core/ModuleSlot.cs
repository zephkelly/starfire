using System;

namespace Starfire.Entity.Modules
{
    public class ModuleSlot<T> where T : class, IShipModule
    {
        private T _module;
        private readonly EntityController _controller;

        public T Module => _module;
        public bool HasModule => _module != null;

        public event Action<T> OnModuleChanged;

        public ModuleSlot(EntityController controller)
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

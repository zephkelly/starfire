using Starfire.Entity.Modules;

namespace Starfire.Entity
{
    public interface IEntitySystems
    {
        bool IsSlotAvailable(ModuleSlotType type);
        bool HasModule(ModuleSlotType type);
        IModuleSlot GetSlot(ModuleSlotType type);
        T GetModule<T>(ModuleSlotType type) where T : class, IEntityModule;
        void UpdateAll(float deltaTime);
    }
}

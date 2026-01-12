using System.Collections.Generic;
using Starfire.Entity.Modules;

namespace Starfire.Entity
{
    public interface IEntitySystems
    {
        // === Legacy single-slot methods (backwards compatible) ===

        bool IsSlotAvailable(ModuleSlotType type);
        bool HasModule(ModuleSlotType type);
        IModuleSlot GetSlot(ModuleSlotType type);
        T GetModule<T>(ModuleSlotType type) where T : class, IEntityModule;
        void UpdateAll(float deltaTime);

        // === Multi-slot methods ===

        /// <summary>
        /// Gets a slot by its unique string ID.
        /// </summary>
        IModuleSlot GetSlotById(string slotId);

        /// <summary>
        /// Gets all slots of a specific module type.
        /// </summary>
        IReadOnlyList<IModuleSlot> GetSlotsByType(ModuleTypeId typeId);

        /// <summary>
        /// Gets all slots in a category.
        /// </summary>
        IEnumerable<IModuleSlot> GetSlotsByCategory(ModuleCategory category);

        /// <summary>
        /// Gets all slots in a subcategory.
        /// </summary>
        IEnumerable<IModuleSlot> GetSlotsBySubCategory(ModuleSubCategory subCategory);

        /// <summary>
        /// Gets all equipped modules of a specific interface type.
        /// </summary>
        IEnumerable<T> GetAllModulesOfType<T>() where T : class, IEntityModule;

        /// <summary>
        /// Checks if any slot of the given type has a module equipped.
        /// </summary>
        bool HasModuleOfType(ModuleTypeId typeId);

        /// <summary>
        /// Counts equipped modules in a category.
        /// </summary>
        int CountModulesInCategory(ModuleCategory category);

        /// <summary>
        /// Counts equipped modules in a subcategory.
        /// </summary>
        int CountModulesInSubCategory(ModuleSubCategory subCategory);
    }
}

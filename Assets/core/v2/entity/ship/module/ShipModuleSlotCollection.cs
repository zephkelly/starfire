using System.Collections.Generic;

namespace StarfireV2
{
    public class ShipModuleSlotCollection : EntityModuleSlotCollection<ShipModuleCategory, ShipModuleTypeId>
    {
        protected override IEnumerable<ShipModuleTypeId> GetTypesInCategory(ShipModuleCategory category)
        {
            return EntityModuleHierarchyRegistry.GetTypesInCategory(category);
        }

        public IEnumerable<T> GetAllModulesOfType<T>() where T : class, IEntityModule
        {
            foreach (var slot in _slotsById.Values)
            {
                if (slot.ModuleBase is T module)
                {
                    yield return module;
                }
            }
        }
    }
}

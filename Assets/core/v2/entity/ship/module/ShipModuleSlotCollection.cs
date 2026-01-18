using System.Collections.Generic;

namespace StarfireV2
{
    public class ShipModuleSlotCollection : EntityModuleSlotCollection<ShipModuleCategory, ShipModuleTypeId>
    {
        protected override IEnumerable<ShipModuleTypeId> GetTypesInCategory(ShipModuleCategory category)
        {
            return EntityModuleHierarchyRegistry.GetTypesInCategory(category);
        }
    }
}

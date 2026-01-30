
namespace StarfireV2
{
    /// <summary>
    /// Condition that succeeds when entity has modules in a specific subcategory.
    /// Optionally requires a minimum count of equipped modules.
    /// </summary>
    public class HasModuleSubCategoryCondition : BTLeafCondition
    {
        private readonly ShipModuleCategory _category;
        private readonly int _minimumCount;

        public HasModuleSubCategoryCondition(ShipModuleCategory category, int minimumCount = 1)
        {
            _category = category;
            _minimumCount = minimumCount;
        }

        protected override bool CheckCondition()
        {
            if (Context.Modules == null)
            {
                return false;
            }

            int count = Context.Modules.CountModulesInCategory(_category);
            return count >= _minimumCount;
        }
    }
}

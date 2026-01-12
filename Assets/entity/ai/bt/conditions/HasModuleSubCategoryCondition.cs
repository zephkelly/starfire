using Starfire.Entity.Modules;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when entity has modules in a specific subcategory.
    /// Optionally requires a minimum count of equipped modules.
    /// </summary>
    public class HasModuleSubCategoryCondition : BTLeafCondition
    {
        private readonly ModuleSubCategory _subCategory;
        private readonly int _minimumCount;

        public HasModuleSubCategoryCondition(ModuleSubCategory subCategory, int minimumCount = 1)
        {
            _subCategory = subCategory;
            _minimumCount = minimumCount;
        }

        protected override bool CheckCondition()
        {
            if (Context.Systems == null)
            {
                return false;
            }

            int count = Context.Systems.CountModulesInSubCategory(_subCategory);
            return count >= _minimumCount;
        }
    }
}

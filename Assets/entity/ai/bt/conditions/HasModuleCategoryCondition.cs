using Starfire.Entity.Modules;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when entity has modules in a specific category.
    /// Optionally requires a minimum count of equipped modules.
    /// </summary>
    public class HasModuleCategoryCondition : BTLeafCondition
    {
        private readonly ModuleCategory _category;
        private readonly int _minimumCount;

        public HasModuleCategoryCondition(ModuleCategory category, int minimumCount = 1)
        {
            _category = category;
            _minimumCount = minimumCount;
        }

        protected override bool CheckCondition()
        {
            if (Context.Systems == null)
            {
                return false;
            }

            int count = Context.Systems.CountModulesInCategory(_category);
            return count >= _minimumCount;
        }
    }
}

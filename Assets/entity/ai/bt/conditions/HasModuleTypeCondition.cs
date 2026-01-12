using Starfire.Entity.Modules;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when entity has a specific module type equipped.
    /// Uses the new ModuleTypeId from the hierarchical module system.
    /// </summary>
    public class HasModuleTypeCondition : BTLeafCondition
    {
        private readonly ModuleTypeId _moduleType;

        public HasModuleTypeCondition(ModuleTypeId moduleType)
        {
            _moduleType = moduleType;
        }

        protected override bool CheckCondition()
        {
            if (Context.Systems == null)
            {
                return false;
            }

            return Context.Systems.HasModuleOfType(_moduleType);
        }
    }
}

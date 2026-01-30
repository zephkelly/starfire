
namespace StarfireV2
{
    /// <summary>
    /// Condition that succeeds when entity has a specific module type equipped.
    /// Uses the new ModuleTypeId from the hierarchical module system.
    /// </summary>
    public class HasModuleTypeCondition : BTLeafCondition
    {
        private readonly ShipModuleTypeId _moduleType;

        public HasModuleTypeCondition(ShipModuleTypeId moduleType)
        {
            _moduleType = moduleType;
        }

        protected override bool CheckCondition()
        {
            if (Context.Modules == null)
            {
                return false;
            }

            return Context.Modules.HasModuleOfType(_moduleType);
        }
    }
}

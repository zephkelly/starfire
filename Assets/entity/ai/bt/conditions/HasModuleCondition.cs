using Starfire.Entity.Modules;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when entity has a specific module type equipped.
    /// </summary>
    public class HasModuleCondition : BTLeafCondition
    {
        private readonly ModuleSlotType _moduleType;

        public HasModuleCondition(ModuleSlotType moduleType)
        {
            _moduleType = moduleType;
        }

        protected override bool CheckCondition()
        {
            if (Context.Systems == null)
            {
                return false;
            }

            return Context.Systems.HasModule(_moduleType);
        }
    }
}

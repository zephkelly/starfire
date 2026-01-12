using UnityEngine;

namespace Starfire.Entity.Modules
{
    public interface IModuleSlot
    {
        bool HasModule { get; }
        IEntityModule ModuleBase { get; }

        /// <summary>
        /// The slot ID for this slot (used for multi-slot systems and hardpoint lookup).
        /// </summary>
        string SlotId { get; }

        /// <summary>
        /// Sets the slot ID. Called by the systems base when registering multi-slots.
        /// </summary>
        void SetSlotId(string slotId);

        void EquipFromConfig(ScriptableObject config);
        void Unequip();
        void Update(float deltaTime);
    }
}

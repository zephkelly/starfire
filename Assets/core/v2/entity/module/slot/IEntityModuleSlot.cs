using UnityEngine;

namespace StarfireV2
{
    public interface IEntityModuleSlot
    {
        string SlotId { get; }
        bool HasModule { get; }
        IEntityModule ModuleBase { get; }

        void SetSlotId(string slotId);
        void EquipFromConfig(ScriptableObject config);
        void EquipFromData(IModuleRuntimeData data);
        void Unequip();
        void Update(float deltaTime);
        IModuleRuntimeData GetRuntimeData();
    }
}

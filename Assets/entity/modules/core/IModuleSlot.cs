using UnityEngine;

namespace Starfire.Entity.Modules
{
    public interface IModuleSlot
    {
        bool HasModule { get; }
        IEntityModule ModuleBase { get; }
        void EquipFromConfig(ScriptableObject config);
        void Unequip();
        void Update(float deltaTime);
    }
}

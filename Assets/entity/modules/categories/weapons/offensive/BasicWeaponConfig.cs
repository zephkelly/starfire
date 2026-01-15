using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    [CreateAssetMenu(fileName = "BasicWeapon", menuName = "Starfire/Modules/Weapon/Basic")]
    public class BasicWeaponConfig : WeaponModuleConfig
    {
        public override ModuleTypeId TypeId => ModuleTypeId.Laser;

        public override IWeaponModule CreateModule()
        {
            return new BasicWeaponModule(this);
        }
    }
}

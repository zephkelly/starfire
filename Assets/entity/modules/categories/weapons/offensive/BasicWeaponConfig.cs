using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    [CreateAssetMenu(fileName = "BasicWeapon", menuName = "Starfire/Modules/Weapon/Basic")]
    public class BasicWeaponConfig : WeaponModuleConfig
    {
        public override IWeaponModule CreateModule()
        {
            return new BasicWeaponModule(this);
        }
    }
}

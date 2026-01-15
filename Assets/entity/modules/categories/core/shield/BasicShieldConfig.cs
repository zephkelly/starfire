using UnityEngine;

namespace Starfire.Entity.Modules.Shield
{
    [CreateAssetMenu(fileName = "BasicShield", menuName = "Starfire/Modules/Shield/Basic")]
    public class BasicShieldConfig : ShieldModuleConfig
    {
        public override IShieldShipModule CreateModule()
        {
            return new BasicShieldModule(this);
        }
    }
}

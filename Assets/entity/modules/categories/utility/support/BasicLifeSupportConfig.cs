using UnityEngine;

namespace Starfire.Entity.Modules.LifeSupport
{
    [CreateAssetMenu(fileName = "BasicLifeSupport", menuName = "Starfire/Modules/LifeSupport/Basic")]
    public class BasicLifeSupportConfig : LifeSupportModuleConfig
    {
        public override ILifeSupportShipModule CreateModule()
        {
            return new BasicLifeSupportModule(this);
        }
    }
}

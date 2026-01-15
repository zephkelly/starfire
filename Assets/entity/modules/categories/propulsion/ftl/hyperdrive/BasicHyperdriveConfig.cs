using UnityEngine;

namespace Starfire.Entity.Modules.Hyperdrive
{
    [CreateAssetMenu(fileName = "BasicHyperdrive", menuName = "Starfire/Modules/Hyperdrive/Basic")]
    public class BasicHyperdriveConfig : HyperdriveModuleConfig
    {
        public override IHyperdriveShipModule CreateModule()
        {
            return new BasicHyperdriveModule(this);
        }
    }
}

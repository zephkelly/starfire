using UnityEngine;

namespace Starfire.Entity.Modules.Hyperdrive
{
    [CreateAssetMenu(fileName = "BasicHyperdrive", menuName = "Starfire/Modules/Hyperdrive/Basic")]
    public class BasicHyperdriveConfig : HyperdriveModuleConfig
    {
        public override IHyperdriveModule CreateModule()
        {
            return new BasicHyperdriveModule(this);
        }
    }
}

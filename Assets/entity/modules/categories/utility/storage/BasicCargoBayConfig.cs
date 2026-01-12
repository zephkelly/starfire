using UnityEngine;

namespace Starfire.Entity.Modules.CargoBay
{
    [CreateAssetMenu(fileName = "BasicCargoBay", menuName = "Starfire/Modules/CargoBay/Basic")]
    public class BasicCargoBayConfig : CargoBayModuleConfig
    {
        public override ICargoBayModule CreateModule()
        {
            return new BasicCargoBayModule(this);
        }
    }
}

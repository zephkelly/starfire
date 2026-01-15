using UnityEngine;

namespace Starfire.Entity.Modules.Hull
{
    [CreateAssetMenu(fileName = "BasicHull", menuName = "Starfire/Modules/Hull/Basic")]
    public class BasicHullConfig : HullModuleConfig
    {
        public override IHullShipModule CreateModule()
        {
            return new BasicHullModule(this);
        }
    }
}

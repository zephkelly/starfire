using UnityEngine;

namespace Starfire.Entity.Modules.Hull
{
    [CreateAssetMenu(fileName = "BasicHull", menuName = "Starfire/Modules/Hull/Basic")]
    public class BasicHullConfig : HullModuleConfig
    {
        public override IHullModule CreateModule()
        {
            return new BasicHullModule(this);
        }
    }
}

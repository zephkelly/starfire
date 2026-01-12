using UnityEngine;

namespace Starfire.Entity.Modules.Propulsion
{
    [CreateAssetMenu(fileName = "BasicPropulsion", menuName = "Starfire/Modules/Propulsion/Basic")]
    public class BasicPropulsionConfig : PropulsionModuleConfig
    {
        public override IPropulsionModule CreateModule()
        {
            return new BasicPropulsionModule(this);
        }
    }
}

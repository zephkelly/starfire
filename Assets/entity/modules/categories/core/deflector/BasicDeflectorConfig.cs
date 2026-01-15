using UnityEngine;

namespace Starfire.Entity.Modules.Deflector
{
    [CreateAssetMenu(fileName = "BasicDeflector", menuName = "Starfire/Modules/Deflector/Basic")]
    public class BasicDeflectorConfig : DeflectorModuleConfig
    {
        public override IDeflectorShipModule CreateModule()
        {
            return new BasicDeflectorModule(this);
        }
    }
}

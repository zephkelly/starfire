using UnityEngine;

namespace Starfire.Entity.Modules.Transponder
{
    [CreateAssetMenu(fileName = "BasicTransponder", menuName = "Starfire/Modules/Transponder/Basic")]
    public class BasicTransponderConfig : TransponderModuleConfig
    {
        public override ITransponderShipModule CreateModule()
        {
            return new BasicTransponderModule(this);
        }
    }
}

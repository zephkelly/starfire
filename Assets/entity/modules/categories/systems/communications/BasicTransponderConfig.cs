using UnityEngine;

namespace Starfire.Entity.Modules.Transponder
{
    [CreateAssetMenu(fileName = "BasicTransponder", menuName = "Starfire/Modules/Transponder/Basic")]
    public class BasicTransponderConfig : TransponderModuleConfig
    {
        public override ITransponderModule CreateModule()
        {
            return new BasicTransponderModule(this);
        }
    }
}

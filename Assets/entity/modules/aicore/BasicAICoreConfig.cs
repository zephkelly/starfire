using UnityEngine;

namespace Starfire.Entity.Modules.AICore
{
    [CreateAssetMenu(fileName = "BasicAICore", menuName = "Starfire/Modules/AICore/Basic")]
    public class BasicAICoreConfig : AICoreModuleConfig
    {
        public override IAICoreModule CreateModule()
        {
            return new BasicAICoreModule(this);
        }
    }
}

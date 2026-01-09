using UnityEngine;

namespace Starfire.Entity.Modules.Transponder
{
    public abstract class TransponderModuleConfig : ScriptableObject
    {
        [Header("Module Identity")]
        [SerializeField] protected string moduleId = "transponder_module";
        [SerializeField] protected string displayName = "Transponder Module";
        [SerializeField] protected ModuleTier tier = ModuleTier.Standard;

        [Header("Transponder Settings")]
        [SerializeField] protected string defaultFaction = "Neutral";

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public ModuleTier Tier => tier;
        public string DefaultFaction => defaultFaction;

        public abstract ITransponderModule CreateModule();
    }
}

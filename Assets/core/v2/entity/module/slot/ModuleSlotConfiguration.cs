using System;
using UnityEngine;

namespace StarfireV2
{
    [Serializable]
    public class ModuleSlotConfiguration
    {
        public string slotId;
        public ShipModuleTypeId typeId;
        public bool isRequired;
        public bool isAvailable = true;
        public ScriptableObject defaultModule;

        [SerializeReference] public IModuleRuntimeData overrideData;
        public bool hasOverrides;

        public ModuleSlotConfiguration()
        {
            isAvailable = true;
        }

        public ModuleSlotConfiguration(string slotId, ShipModuleTypeId typeId, bool isRequired = false)
        {
            this.slotId = slotId;
            this.typeId = typeId;
            this.isRequired = isRequired;
            this.isAvailable = true;
        }
    }
}

using System;
using UnityEngine;

namespace Starfire.Entity.Modules
{
    /// <summary>
    /// Runtime configuration for a module slot (derived from MultiSlotEntry).
    /// Used during ship initialization to create actual slot instances.
    /// </summary>
    [Serializable]
    public class MultiSlotConfiguration
    {
        public string slotId;
        public ModuleTypeId moduleType;
        public bool isAvailable;
        public bool isRequired;
        public ScriptableObject defaultModule;

        public MultiSlotConfiguration()
        {
            isAvailable = true;
        }

        public MultiSlotConfiguration(MultiSlotEntry entry)
        {
            slotId = entry.slotId;
            moduleType = entry.moduleType;
            isAvailable = true;
            isRequired = entry.isRequired;
            defaultModule = entry.defaultModule;
        }
    }
}

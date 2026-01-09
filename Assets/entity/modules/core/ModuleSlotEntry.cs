using System;
using UnityEngine;

namespace Starfire.Entity.Modules
{
    [Serializable]
    public class ModuleSlotEntry
    {
        [Tooltip("The type of module this slot accepts")]
        public ModuleSlotType slotType;

        [Tooltip("Whether a module must be installed in this slot")]
        public bool isRequired;

        [Tooltip("The default module configuration for this slot")]
        public ScriptableObject defaultModule;
    }
}

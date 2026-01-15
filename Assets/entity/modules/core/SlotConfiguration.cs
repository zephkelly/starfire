using System;
using UnityEngine;

namespace Starfire.Entity.Modules
{
    [Serializable]
    public class SlotConfiguration
    {
        [Tooltip("The type of module slot")]
        public ModuleTypeId moduleType;

        [Tooltip("Whether this slot is available on the ship")]
        public bool isAvailable = true;

        [Tooltip("Whether a module must be equipped in this slot")]
        public bool isRequired = false;

        [Tooltip("Default module config to equip (null = empty slot)")]
        public ScriptableObject defaultModule;
    }
}

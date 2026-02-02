using System;
using UnityEngine;

namespace StarfireV2
{
    [Serializable]
    public class ShieldModuleData : IModuleRuntimeData
    {
        public string moduleId = "shield_module";
        public int maxShield = 50;
        public float regenRate = 5f;
        public float rechargeDelay = 3f;
        public bool enableBoundary = true;
        public Vector2 boundarySize = new Vector2(2f, 1.5f);
        public Vector2 boundaryOffset = Vector2.zero;
        public int boundaryResolution = 24;

        public string ModuleId => moduleId;
        public ShipModuleTypeId TypeId => ShipModuleTypeId.Shield;
    }
}

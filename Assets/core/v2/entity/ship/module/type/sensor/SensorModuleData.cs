using System;
using UnityEngine;

namespace StarfireV2
{
    [Serializable]
    public class SensorModuleData : IModuleRuntimeData
    {
        public string moduleId;
        public string displayName;
        public ModuleTier tier = ModuleTier.Standard;
        public V2DetectionRangeConfig rangeConfig;
        public float pollingInterval = 0.25f;
        [Range(0f, 1f)]
        public float targetingAccuracy = 0.9f;
        public V2SensorFilterConfig filterConfig;
        public LayerMask threatLayers;

        public string ModuleId => moduleId;
        public ShipModuleTypeId TypeId => ShipModuleTypeId.SensorArray;
    }
}

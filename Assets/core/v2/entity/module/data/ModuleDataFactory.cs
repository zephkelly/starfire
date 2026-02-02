using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Creates module instances directly from runtime data classes,
    /// bypassing the need for a ScriptableObject config reference.
    /// </summary>
    public static class ModuleDataFactory
    {
        public static IShipModule CreateModuleFromData(IModuleRuntimeData data)
        {
            return data switch
            {
                PropulsionModuleData d => new PropulsionModule(d),
                OffensiveWeaponModuleData d => new OffensiveWeaponModule(d),
                PointDefenseModuleData d => new PointDefenseModule(d),
                SensorModuleData d => new V2SensorModule(d),
                TransponderModuleData d => new V2TransponderModule(d),
                RotationModuleData d => new RotationModule(d),
                AICoreModuleData d => new V2AICoreModule(d),
                ShieldModuleData d => new ShieldModule(d),
                _ => null
            };
        }
    }
}

using System;

namespace StarfireV2
{
    [Serializable]
    public class PropulsionModuleData : IModuleRuntimeData
    {
        public string moduleId = "propulsion_module";
        public string displayName = "Propulsion Module";
        public float maxSpeed = 10f;
        public float acceleration = 5f;
        public float drag = 1f;

        public string ModuleId => moduleId;
        public ShipModuleTypeId TypeId => ShipModuleTypeId.ManeuveringThrusters;
    }
}

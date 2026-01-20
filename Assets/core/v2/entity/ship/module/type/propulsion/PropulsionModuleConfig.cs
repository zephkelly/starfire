using UnityEngine;

namespace StarfireV2
{
    [CreateAssetMenu(fileName = "Propulsion", menuName = "StarfireV2/Modules/Propulsion")]
    public class PropulsionModuleConfig : ScriptableObject, IShipModuleConfig
    {
        public ShipModuleTypeId TypeId => ShipModuleTypeId.ManeuveringThrusters;
        IShipModule IShipModuleConfig.CreateModule() => CreateModule();

        [Header("Module Identity")]
        [SerializeField] private string moduleId = "propulsion_module";
        [SerializeField] private string displayName = "Propulsion Module";

        [Header("Propulsion Stats")]
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float acceleration = 5f;
        [SerializeField] private float drag = 1f;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float Drag => drag;

        public IShipPropulsionModule CreateModule()
        {
            return new PropulsionModule(this);
        }
    }
}

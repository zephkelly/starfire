using UnityEngine;

namespace StarfireV2
{
    public class ShipController : MonoBehaviour, IEntityController
    {
        [SerializeField] private ModuleSlotConfiguration[] _slotConfigurations;

        public IEntity Entity => Ship;
        public ShipEntity Ship { get; private set; }
        public IEntityControllerDriver Driver { get; private set; }

        public Rigidbody2D Rigidbody2D { get; private set; }
        public Transform Transform { get; private set; }

        private void Awake()
        {
            Rigidbody2D = GetComponent<Rigidbody2D>();
            Transform = transform;

            Ship = new ShipEntity(EntityType.Ship, GetInstanceID());
            Ship.InitializeModules(this, _slotConfigurations);
        }

        private void Update()
        {
            Ship.UpdateModules(Time.deltaTime);
        }

        public void SetDriver(IEntityControllerDriver driver)
        {
            Driver = driver;
        }
    }
}
using UnityEngine;
using Starfire.Entity.Modules.Rotation;

namespace Starfire.Entity
{
    [RequireComponent(typeof(EntityController))]
    [RequireComponent(typeof(OldInputProvider))]
    public class PlayerSetup : MonoBehaviour
    {
        [Header("Ship Stats")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float rotationSpeed = 180f;

        [Header("Entity Stats")]
        [SerializeField] private int health = 100;
        [SerializeField] private int energy = 50;
        [SerializeField] private int fuel = 100;
        [SerializeField] private int shieldHealth = 50;

        [Header("Modules")]
        [SerializeField] private RotationModuleConfig rotationModule;

        private PlayerDriver playerDriver;

        private void Start()
        {
            var entityController = GetComponent<EntityController>();
            var inputProvider = GetComponent<OldInputProvider>();

            var ship = new Ship(health, energy, fuel, shieldHealth, moveSpeed, rotationSpeed);
            entityController.Initialize(ship);

            playerDriver = new PlayerDriver(inputProvider, priority: 10);
            entityController.DriverStack.Push(playerDriver);

            if (rotationModule != null)
            {
                entityController.SetRotationModule(rotationModule);
            }
        }

        private void OnDestroy()
        {
            playerDriver?.Unsubscribe();
        }
    }
}

using UnityEngine;

namespace Starfire.Entity
{
    [RequireComponent(typeof(EntityController))]
    [RequireComponent(typeof(OldInputProvider))]
    public class PlayerSetup : MonoBehaviour
    {
        [Header("Ship Class")]
        [SerializeField] private ShipClassDefinition shipClass;

        private PlayerDriver playerDriver;

        private void Start()
        {
            var entityController = GetComponent<EntityController>();
            var inputProvider = GetComponent<OldInputProvider>();

            if (shipClass == null)
            {
                Debug.LogError("PlayerSetup: No ShipClassDefinition assigned!");
                return;
            }

            entityController.Initialize(shipClass);

            playerDriver = new PlayerDriver(inputProvider, priority: 10);
            entityController.DriverStack.Push(playerDriver);
        }

        private void OnDestroy()
        {
            playerDriver?.Unsubscribe();
        }
    }
}

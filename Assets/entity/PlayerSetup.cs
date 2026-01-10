using UnityEngine;

namespace Starfire.Entity
{
    [RequireComponent(typeof(ShipController))]
    [RequireComponent(typeof(OldInputProvider))]
    public class PlayerSetup : MonoBehaviour
    {
        [Header("Ship Class")]
        [SerializeField] private ShipClassDefinition shipClass;

        private PlayerDriver playerDriver;

        private void Start()
        {
            var shipController = GetComponent<ShipController>();
            var inputProvider = GetComponent<OldInputProvider>();

            if (shipClass == null)
            {
                Debug.LogError("PlayerSetup: No ShipClassDefinition assigned!");
                return;
            }

            shipController.Initialize(shipClass);

            playerDriver = new PlayerDriver(inputProvider, priority: 10);
            shipController.DriverStack.Push(playerDriver);
        }

        private void OnDestroy()
        {
            playerDriver?.Unsubscribe();
        }
    }
}

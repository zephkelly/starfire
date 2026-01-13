using UnityEngine;
using Starfire.Core;
using Starfire.Entity.Modules.Weapon;

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
            // Assign to Player layer for collision filtering
            gameObject.layer = LayerMask.NameToLayer(GameLayers.Player);

            var shipController = GetComponent<ShipController>();
            var inputProvider = GetComponent<OldInputProvider>();

            if (shipClass == null)
            {
                Debug.LogError("PlayerSetup: No ShipClassDefinition assigned!");
                return;
            }

            // Initialize hardpoint registry before ship systems (so weapons can find their hardpoints)
            var hardpointRegistry = GetComponent<HardpointRegistry>();
            hardpointRegistry?.Initialize();

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

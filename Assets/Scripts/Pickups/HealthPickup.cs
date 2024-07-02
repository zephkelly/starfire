using UnityEngine;

namespace Starfire
{
    public class HealthPickup : MonoBehaviour, IPickup
    {
        private PickupType type;
        private int value;

        public PickupType Type => type;
        public int Value => value;

        private void Awake()
        {
            type = PickupType.Health;
            value = 50;
        }
    }
}
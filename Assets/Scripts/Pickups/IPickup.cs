using UnityEngine;

namespace Starfire
{
    public interface IPickup
    {
        public PickupType Type { get; }
        public int Value { get; }
    }

    public enum PickupType
    {
        Health,
        Armour,
        Fuel,
    }
}
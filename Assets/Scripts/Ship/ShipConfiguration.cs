using UnityEngine;
using Starfire.Items;

namespace Starfire
{
    public enum DamageType
    {
        Hull,
        Shield
    }

    public class ShipConfiguration : ScriptableObject
    {
        public int Health { get; private set; }
        public int Fuel { get; private set; }
        public int Cargo { get; private set; }
        public int EngineTopSpeed { get; private set; }
        public int MaxHealth { get; private set; }
        public int MaxFuel { get; private set; }
        public int MaxCargo { get; private set; }

        public int Damage(int damage, DamageType damageType)
        {
            if (damageType == DamageType.Hull) return DamageHull(damage);
            else if (damageType == DamageType.Shield) return DamageShield(damage);

            return -1;
        }

        public void Repair(int repair, DamageType damageType)
        {
            if (damageType == DamageType.Hull) RepairHull(repair);
            else if (damageType == DamageType.Shield) RepairShield(repair);
        }

        public void Refuel(int fuel)
        {
            //To be implemented
        }

        public void LoadCargo(ItemType itemType, int amount)
        {
            //To be implemented
        }

        private int DamageHull(int damage)
        {
            //To be implemented
            return -1;
        }

        private int DamageShield(int damage)
        {
            //To be implemented
            return -1;
        }

        private void RepairHull(int repair)
        {
            //To be implemented
        }

        private void RepairShield(int repair)
        {
            //To be implemented
        }
    }
}
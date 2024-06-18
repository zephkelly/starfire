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
        private ShipController shipController;

        public int Health { get; private set; }
        public int Fuel { get; private set; }
        public int Cargo { get; private set; }
        public int EngineTopSpeed { get; private set; }
        public int ProjectileDamage { get; private set; }
        public int MaxHealth { get; private set; }
        public int MaxFuel { get; private set; }
        public int MaxCargo { get; private set; }

        public void SetConfiguration(ShipController shipConfig, int health, int fuel, int cargo, int engineTopSpeed)
        {
            shipController = shipConfig;
            Health = health;
            Fuel = fuel;
            Cargo = cargo;
            EngineTopSpeed = engineTopSpeed;
            MaxHealth = health;
            MaxFuel = fuel;
            MaxCargo = cargo;

            ProjectileDamage = 4;
        }

        public void Damage(int damage, DamageType damageType)
        {
            if (damageType == DamageType.Hull) DamageHull(damage);
            else if (damageType == DamageType.Shield) DamageShield(damage);
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

        private void DamageHull(int damage)
        {
            Health -= damage;

            if (Health <= 0)
            {
                Destroy(shipController.gameObject);
                Destroy(shipController);
                Destroy(this);
            }
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
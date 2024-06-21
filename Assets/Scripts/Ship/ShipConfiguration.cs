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
        public int ThrusterMaxSpeed { get; private set; }
        public int WarpMaxSpeed { get; private set; }
        public int TurnDegreesPerSecond { get; private set; }
        public int ProjectileDamage { get; private set; }
        public int MaxHealth { get; private set; }
        public int MaxFuel { get; private set; }
        public int MaxCargo { get; private set; }

        public void SetConfiguration(ShipController shipConfig, int health, int fuel, int cargo, int _thrusterMaxSpeed, int _warpMaxSpeed, int _turnSpeed, int _projectioleDamage = 4)
        {
            shipController = shipConfig;
            Health = health;
            MaxHealth = health;
            Fuel = fuel;
            Cargo = cargo;
            ThrusterMaxSpeed = _thrusterMaxSpeed;
            WarpMaxSpeed = _warpMaxSpeed;
            TurnDegreesPerSecond = _turnSpeed;
            MaxFuel = fuel;
            MaxCargo = cargo;

            ProjectileDamage = _projectioleDamage;
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
            //Activate healthbar
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
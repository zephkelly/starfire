using UnityEngine;

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
        public int WarpFuel { get; private set; }
        public int Cargo { get; private set; }
        public int ThrusterMaxSpeed { get; private set; }
        public int WarpIncrementSpeed { get; private set; }
        public int WarpMaxSpeed { get; private set; }
        public int TurnDegreesPerSecond { get; private set; }
        public int ProjectileDamage { get; private set; }
        public int MaxHealth { get; private set; }
        public int MaxWarpFuel { get; private set; }
        public int MaxFuel { get; private set; }
        public int MaxCargo { get; private set; }

        public void SetConfiguration(ShipController shipConfig, int health, int fuel, int warpFuel, int cargo, int _thrusterMaxSpeed, int _warpMaxSpeed, int _turnSpeed, int _projectileDamage = 4)
        {
            shipController = shipConfig;

            Health = health;
            MaxHealth = health;

            Fuel = fuel;
            MaxFuel = fuel;

            WarpFuel = warpFuel;
            MaxWarpFuel = warpFuel;

            Cargo = cargo;
            MaxCargo = cargo;

            ThrusterMaxSpeed = _thrusterMaxSpeed;
            WarpIncrementSpeed = _thrusterMaxSpeed / 2;
            WarpMaxSpeed = _warpMaxSpeed;
            TurnDegreesPerSecond = _turnSpeed;

            ProjectileDamage = _projectileDamage;
        }

        public void UseWarpFuel()
        {
            const int maxWarpFuelUsage = 10;
            const int minWarpFuelUsage = 0;

            int warpFuelUsage = (int) Mathf.Lerp(maxWarpFuelUsage, minWarpFuelUsage, shipController.ShipRigidBody.velocity.magnitude / (WarpMaxSpeed * 0.8f));
            if (shipController.IsOrbiting) warpFuelUsage = 4;
            WarpFuel -= warpFuelUsage;
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
                Health = 0;
                shipController.DestroyShip();
            }
        }

        private int DamageShield(int damage)
        {
            //To be implemented
            return -1;
        }

        private void RepairHull(int repair)
        {
            Health += repair;
            
            if (Health > MaxHealth) Health = MaxHealth;
        }

        private void RepairShield(int repair)
        {
            //To be implemented
        }
    }
}
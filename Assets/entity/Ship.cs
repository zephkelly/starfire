namespace Starfire.Entity
{
    public class Ship : Entity
    {
        public Ship(float moveSpeed, float rotationSpeed)
            : base(health: 100, energy: 50, fuel: 100, shieldHealth: 50, moveSpeed, rotationSpeed)
        {
        }

        public Ship(int health, int energy, int fuel, int shieldHealth, float moveSpeed, float rotationSpeed)
            : base(health, energy, fuel, shieldHealth, moveSpeed, rotationSpeed)
        {
        }
    }
}

namespace Starfire.Entity
{
    public abstract class Entity : IEntity
    {
        public EntityCapabilities[] Capabilities { get; protected set; }
        public int Health { get; set; }
        public int Energy { get; set; }
        public int Fuel { get; set; }
        public Shield Shield { get; set; }

        public float MoveSpeed { get; set; }
        public float RotationSpeed { get; set; }

        protected Entity(int health, int energy, int fuel, int shieldHealth, float moveSpeed = 10f, float rotationSpeed = 180f)
        {
            Health = health;
            Energy = energy;
            Fuel = fuel;
            Shield = new Shield(shieldHealth);
            MoveSpeed = moveSpeed;
            RotationSpeed = rotationSpeed;
        }
    }
}
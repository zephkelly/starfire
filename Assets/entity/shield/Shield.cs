namespace Starfire.Entity
{
    public enum ShieldState
    {
        Destroyed,
        Inactive,
        Charging,
        Overloading,
        Active,
    }

    public class Shield
    {
        public ShieldState State { get; }
        public int Health { get; set; }

        public Shield(int health)
        {
            Health = health;
            State = ShieldState.Inactive;
        }

        public int Damage(int amount)
        {
            // Handle differently in different states
            Health -= amount;
            if (Health <= 0)
            {
                Health = 0;
            }
            return Health;
        }
    }
}
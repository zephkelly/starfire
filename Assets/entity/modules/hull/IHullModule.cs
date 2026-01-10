namespace Starfire.Entity.Modules.Hull
{
    public interface IHullModule : IEntityModule
    {
        int MaxHealth { get; }
        int CurrentHealth { get; set; }
        float DamageResistance { get; }
        void TakeDamage(int amount);
    }
}

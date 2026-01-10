namespace Starfire.Entity.Modules.Shield
{
    public enum ShieldState
    {
        Destroyed,
        Inactive,
        Charging,
        Recharging,
        Overloading,
        Active,
    }

    public interface IShieldModule : IEntityModule
    {
        int MaxShield { get; }
        int CurrentShield { get; set; }
        float RegenRate { get; }
        ShieldState State { get; }
        void TakeDamage(int amount);
    }
}

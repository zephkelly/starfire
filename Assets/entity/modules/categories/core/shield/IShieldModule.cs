namespace Starfire.Entity.Modules.Shield
{
    public enum ShieldState
    {
        Destroyed,
        Inactive,
        Charging,    // Has not taken damage in X window
        Recharging,  // Recently took damage
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

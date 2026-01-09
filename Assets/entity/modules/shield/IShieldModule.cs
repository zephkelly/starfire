namespace Starfire.Entity.Modules.Shield
{
    public interface IShieldModule : IShipModule
    {
        int MaxShield { get; }
        int CurrentShield { get; set; }
        float RegenRate { get; }
        ShieldState State { get; }
        void TakeDamage(int amount);
    }
}

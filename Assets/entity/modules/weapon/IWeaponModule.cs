namespace Starfire.Entity.Modules.Weapon
{
    public interface IWeaponModule : IShipModule
    {
        float Damage { get; }
        float FireRate { get; }
        float Range { get; }
        void Fire();
    }
}

namespace Starfire.Entity.Modules.Weapon
{
    public interface IWeaponModule : IEntityModule
    {
        float Damage { get; }
        float FireRate { get; }
        float Range { get; }
        void Fire();
    }
}

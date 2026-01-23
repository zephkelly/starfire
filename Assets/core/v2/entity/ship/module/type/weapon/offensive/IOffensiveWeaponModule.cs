namespace StarfireV2
{
    /// <summary>
    /// Interface for offensive weapon modules that deal damage to targets.
    /// Extends both IWeaponModule (v2 weapon system) and IShipOffensiveModule (for ShipEntity.OffenseModules compatibility).
    /// </summary>
    public interface IOffensiveWeaponModule : IWeaponModule, IShipOffensiveModule
    {
        // Note: Damage, FireRate, Range, CanFire, Fire(), SetAimDirection() are inherited from IShipOffensiveModule

        /// <summary>
        /// Configuration for damage types and multipliers.
        /// </summary>
        V2WeaponDamageConfig DamageConfig { get; }

        /// <summary>
        /// Configuration for projectile behavior.
        /// </summary>
        V2ProjectileConfig ProjectileConfig { get; }
    }
}

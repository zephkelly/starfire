using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    public interface IWeaponShipModule : IShipModule
    {
        float Damage { get; }
        float FireRate { get; }
        float Range { get; }

        /// <summary>
        /// Returns true if weapon can fire (not on cooldown, visual ready, etc.)
        /// </summary>
        bool CanFire { get; }

        /// <summary>
        /// Returns true if this weapon operates as a turret (can rotate).
        /// </summary>
        bool IsTurret { get; }

        /// <summary>
        /// Attempts to fire the weapon. Returns true if fired successfully.
        /// </summary>
        bool Fire();

        /// <summary>
        /// Sets the target direction for turret aiming (world space).
        /// For fixed weapons, this may be ignored.
        /// </summary>
        void SetAimDirection(Vector2 worldDirection);

        /// <summary>
        /// Called when the weapon is assigned to a hardpoint.
        /// Allows the weapon to set up visuals at the mount point.
        /// </summary>
        void OnHardpointAssigned(HardpointMarker hardpoint);
    }
}

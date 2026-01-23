using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Base interface for all weapon modules (offensive and defensive).
    /// Extends IShipModule with weapon-specific functionality.
    /// </summary>
    public interface IWeaponModule : IShipModule
    {
        /// <summary>
        /// The weight class of this weapon, determining which hardpoints it can mount on.
        /// </summary>
        WeaponWeightClass WeightClass { get; }

        /// <summary>
        /// Fire rate in shots per second.
        /// </summary>
        float FireRate { get; }

        /// <summary>
        /// Maximum effective range of the weapon.
        /// </summary>
        float Range { get; }

        /// <summary>
        /// Whether this weapon can rotate to track targets.
        /// </summary>
        bool IsTurret { get; }

        /// <summary>
        /// Whether the weapon is ready to fire (enabled, off cooldown, aimed if turret).
        /// </summary>
        bool CanFire { get; }

        /// <summary>
        /// Remaining cooldown time in seconds before the weapon can fire again.
        /// </summary>
        float CooldownRemaining { get; }

        /// <summary>
        /// Attempts to fire the weapon.
        /// </summary>
        /// <returns>True if the weapon fired successfully, false otherwise.</returns>
        bool Fire();

        /// <summary>
        /// Sets the target direction for turret aiming.
        /// </summary>
        /// <param name="worldDirection">The direction to aim in world space.</param>
        void SetAimDirection(Vector2 worldDirection);

        /// <summary>
        /// Called when the weapon is assigned to a hardpoint.
        /// Used to instantiate visuals and set up positioning.
        /// </summary>
        /// <param name="hardpoint">The hardpoint marker this weapon is mounted on.</param>
        void OnHardpointAssigned(V2HardpointMarker hardpoint);

        /// <summary>
        /// Called when the weapon is removed from its hardpoint.
        /// Used to clean up visuals.
        /// </summary>
        void OnHardpointUnassigned();
    }
}

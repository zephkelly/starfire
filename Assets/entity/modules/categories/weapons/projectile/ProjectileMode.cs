namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Defines how projectile collision detection is handled.
    /// </summary>
    public enum ProjectileMode
    {
        /// <summary>
        /// Full physics simulation with Rigidbody2D and trigger collider.
        /// Best for slow/medium projectiles or guided missiles that need physics.
        /// May tunnel through objects at very high speeds.
        /// </summary>
        Physics,

        /// <summary>
        /// Single raycast at fire time determines the hit point.
        /// Visual projectile travels to the predetermined hit location.
        /// Guarantees hit detection - no tunneling possible.
        /// Best for fast straight-line projectiles.
        /// </summary>
        RaycastBacked,

        /// <summary>
        /// Instant raycast with immediate damage application.
        /// Visual beam/trail appears instantly and fades over time.
        /// Best for laser beams and instant-hit weapons.
        /// </summary>
        Hitscan
    }
}

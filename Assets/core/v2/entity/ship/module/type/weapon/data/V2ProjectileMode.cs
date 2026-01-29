namespace StarfireV2
{
    /// <summary>
    /// Defines how projectiles handle collision detection and movement.
    /// </summary>
    public enum V2ProjectileMode
    {
        /// <summary>
        /// Full physics simulation with Rigidbody2D.
        /// Uses continuous collision detection.
        /// Best for slow/medium speed projectiles.
        /// </summary>
        Physics,

        /// <summary>
        /// Single raycast at fire time determines hit point.
        /// Visual projectile travels to predetermined location.
        /// Guarantees hit (no tunneling). Best for fast straight-line projectiles.
        /// </summary>
        RaycastBacked,

        /// <summary>
        /// Instant raycast with immediate damage application.
        /// Visual beam/trail appears and fades over time.
        /// Best for laser beams and instant-hit weapons.
        /// </summary>
        Hitscan,

        /// <summary>
        /// Homing missile with weaving path and target tracking.
        /// Uses physics simulation with steering forces applied by V2MissileBehavior.
        /// Missiles bloom outward at launch, then track and weave toward targets.
        /// </summary>
        Missile,

        /// <summary>
        /// Physics-driven missile using thruster forces for movement and steering.
        /// Main rear thruster provides forward acceleration, side steering thrusters
        /// apply lateral forces via AddForceAtPosition for realistic turning.
        /// </summary>
        ThrustMissile
    }
}

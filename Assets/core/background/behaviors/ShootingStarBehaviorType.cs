namespace Starfire.Core.Background.Behaviors
{
    /// <summary>
    /// Types of behavior for shooting stars.
    /// </summary>
    public enum ShootingStarBehaviorType
    {
        /// <summary>
        /// Current default behavior - travels distance, fades based on lifetime progress.
        /// Fade-in first 10%, fade-out last 10%.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Stays visible while in camera view. When leaving view,
        /// continues moving but starts slow fade.
        /// </summary>
        Persistent = 1,

        /// <summary>
        /// Fades out slowly based on time OR distance (configurable).
        /// </summary>
        SlowFade = 2,

        /// <summary>
        /// Reserved for future: Travels distance then explodes.
        /// </summary>
        Explosion = 3,

        /// <summary>
        /// Simple time-based fade - fades from full brightness to zero over duration.
        /// No modes or complex parameters, just fade duration and optional delay.
        /// </summary>
        SimpleTimeFade = 4
    }

    /// <summary>
    /// Mode for slow fade behavior.
    /// </summary>
    public enum SlowFadeMode
    {
        /// <summary>
        /// Fade based on elapsed time.
        /// </summary>
        TimeBased = 0,

        /// <summary>
        /// Fade based on distance traveled.
        /// </summary>
        DistanceBased = 1
    }
}

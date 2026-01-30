namespace StarfireV2
{
    /// <summary>
    /// Determines how BTParallel evaluates success.
    /// </summary>
    public enum ParallelMode
    {
        /// <summary>
        /// Success when ALL children succeed. Running if ANY is running.
        /// This is the default behavior.
        /// </summary>
        RequireAll = 0,

        /// <summary>
        /// Success when ANY child succeeds (first-to-finish wins).
        /// Useful for timeout patterns: Parallel(Action, Timer).
        /// </summary>
        RequireOne = 1
    }
}

using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for BTParallel composite node.
    /// </summary>
    [Serializable]
    public class ParallelParameters : IBTNodeParameters
    {
        /// <summary>
        /// How the Parallel evaluates success.
        /// RequireAll (default): Success when ALL children succeed.
        /// RequireOne: Success when ANY child succeeds (first-to-finish wins).
        /// </summary>
        public ParallelMode mode = ParallelMode.RequireAll;
    }
}

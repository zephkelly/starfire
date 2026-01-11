using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for AimInMovementDirectionAction.
    /// </summary>
    [Serializable]
    public class AimInMovementDirectionParameters : IBTNodeParameters
    {
        /// <summary>
        /// Distance ahead of current position to set aim target.
        /// </summary>
        public float lookAheadDistance = 10f;
    }
}

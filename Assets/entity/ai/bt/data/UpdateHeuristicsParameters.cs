using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for UpdateHeuristicsAction.
    /// Calculates ship state heuristics and writes them to blackboard.
    /// </summary>
    [Serializable]
    public class UpdateHeuristicsParameters : IBTNodeParameters
    {
        /// <summary>
        /// If true, writes individual heuristic values to blackboard for BT conditions.
        /// If false, only writes the full HeuristicData struct.
        /// </summary>
        public bool writeIndividualKeys = true;
    }
}

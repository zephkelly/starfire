using System;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for a Subtree node, containing the reference to the external tree.
    /// </summary>
    [Serializable]
    public class SubtreeParameters : IBTNodeParameters
    {
        /// <summary>
        /// Reference to the subtree asset to execute.
        /// </summary>
        [SerializeField]
        public BehaviorTreeAsset subtreeAsset;
    }
}

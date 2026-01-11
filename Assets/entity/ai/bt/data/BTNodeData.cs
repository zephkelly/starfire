using System;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Serializable data for a single behavior tree node.
    /// </summary>
    [Serializable]
    public class BTNodeData
    {
        /// <summary>
        /// Unique identifier for this node.
        /// </summary>
        public string id = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of node (Selector, Sequence, Repeater, Action).
        /// </summary>
        public BTNodeType nodeType;

        /// <summary>
        /// For Action nodes, specifies which action type (e.g., "Patrol", "MoveTo").
        /// </summary>
        public string actionType;

        /// <summary>
        /// Position in the graph editor.
        /// </summary>
        public Vector2 editorPosition;

        /// <summary>
        /// Node-specific parameters (polymorphic via SerializeReference).
        /// </summary>
        [SerializeReference]
        public IBTNodeParameters parameters;

        public BTNodeData() { }

        public BTNodeData(BTNodeType type, Vector2 position)
        {
            nodeType = type;
            editorPosition = position;
        }
    }
}

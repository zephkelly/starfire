using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Represents a connection between two nodes in the behavior tree.
    /// </summary>
    [Serializable]
    public class BTConnection
    {
        /// <summary>
        /// ID of the parent node.
        /// </summary>
        public string parentId;

        /// <summary>
        /// ID of the child node.
        /// </summary>
        public string childId;

        /// <summary>
        /// Index position of this child under the parent.
        /// Order matters for Selector and Sequence nodes.
        /// </summary>
        public int childIndex;

        public BTConnection() { }

        public BTConnection(string parent, string child, int index = 0)
        {
            parentId = parent;
            childId = child;
            childIndex = index;
        }
    }
}

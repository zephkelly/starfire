using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// A ScriptableObject that contains an entire behavior tree definition.
    /// Can be edited with the visual node graph editor.
    /// </summary>
    [CreateAssetMenu(fileName = "BehaviorTree", menuName = "Starfire/AI/Behavior Tree")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        [SerializeField]
        private List<BTNodeData> nodes = new();

        [SerializeField]
        private List<BTConnection> connections = new();

        [SerializeField]
        private string rootNodeId;

        /// <summary>
        /// All nodes in the tree.
        /// </summary>
        public IReadOnlyList<BTNodeData> Nodes => nodes;

        /// <summary>
        /// All connections between nodes.
        /// </summary>
        public IReadOnlyList<BTConnection> Connections => connections;

        /// <summary>
        /// ID of the root node.
        /// </summary>
        public string RootNodeId
        {
            get => rootNodeId;
            set => rootNodeId = value;
        }

        /// <summary>
        /// Creates a runtime behavior tree from this asset.
        /// </summary>
        public IBTNode CreateRuntimeTree()
        {
            if (string.IsNullOrEmpty(rootNodeId) || nodes.Count == 0)
            {
                Debug.LogWarning($"BehaviorTreeAsset '{name}' has no root node or is empty.");
                return null;
            }

            // Create all runtime nodes
            var nodeMap = new Dictionary<string, IBTNode>();
            foreach (var data in nodes)
            {
                var node = CreateNode(data);
                if (node != null)
                {
                    nodeMap[data.id] = node;
                }
            }

            // Wire up connections (sorted by child index)
            var connectionsByParent = connections
                .GroupBy(c => c.parentId)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.childIndex).ToList());

            foreach (var kvp in connectionsByParent)
            {
                if (!nodeMap.TryGetValue(kvp.Key, out var parent))
                    continue;

                var children = kvp.Value
                    .Where(c => nodeMap.ContainsKey(c.childId))
                    .Select(c => nodeMap[c.childId])
                    .ToList();

                SetChildren(parent, children);
            }

            return nodeMap.TryGetValue(rootNodeId, out var root) ? root : null;
        }

        private IBTNode CreateNode(BTNodeData data)
        {
            return data.nodeType switch
            {
                BTNodeType.Selector => new BTSelector(new List<IBTNode>()),
                BTNodeType.Sequence => new BTSequence(new List<IBTNode>()),
                BTNodeType.Parallel => new BTParallel(new List<IBTNode>()),
                BTNodeType.Repeater => CreateRepeater(data.parameters as RepeaterParameters),
                BTNodeType.Action => CreateAction(data),
                _ => null
            };
        }

        private IBTNode CreateRepeater(RepeaterParameters parameters)
        {
            // Repeater needs a child, but we'll set it later via SetChildren
            // For now, create with a placeholder that will be replaced
            int repeatCount = parameters?.repeatCount ?? -1;
            return new BTRepeaterBuilder(repeatCount);
        }

        private IBTNode CreateAction(BTNodeData data)
        {
            if (string.IsNullOrEmpty(data.actionType))
            {
                Debug.LogWarning($"Action node '{data.id}' has no action type specified.");
                return null;
            }

            try
            {
                return BTActionRegistry.CreateAction(data.actionType, data.parameters);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create action '{data.actionType}': {e.Message}");
                return null;
            }
        }

        private void SetChildren(IBTNode parent, List<IBTNode> children)
        {
            switch (parent)
            {
                case BTSelector selector:
                    selector.SetChildren(children);
                    break;
                case BTSequence sequence:
                    sequence.SetChildren(children);
                    break;
                case BTParallel parallel:
                    parallel.SetChildren(children);
                    break;
                case BTRepeaterBuilder repeaterBuilder:
                    if (children.Count > 0)
                        repeaterBuilder.SetChild(children[0]);
                    break;
            }
        }

        #region Editor Methods (for graph editor)

        public BTNodeData AddNode(BTNodeType type, Vector2 position)
        {
            var node = new BTNodeData(type, position);

            // Add default parameters for nodes that need them
            if (type == BTNodeType.Repeater)
            {
                node.parameters = new RepeaterParameters();
            }

            nodes.Add(node);
            return node;
        }

        public BTNodeData AddActionNode(string actionType, Vector2 position)
        {
            var node = new BTNodeData(BTNodeType.Action, position)
            {
                actionType = actionType,
                parameters = BTActionRegistry.CreateDefaultParameters(actionType)
            };
            nodes.Add(node);
            return node;
        }

        public void RemoveNode(string nodeId)
        {
            nodes.RemoveAll(n => n.id == nodeId);
            connections.RemoveAll(c => c.parentId == nodeId || c.childId == nodeId);

            if (rootNodeId == nodeId)
                rootNodeId = null;
        }

        public BTNodeData GetNode(string nodeId)
        {
            return nodes.FirstOrDefault(n => n.id == nodeId);
        }

        public void AddConnection(string parentId, string childId)
        {
            // Check for existing connection
            if (connections.Any(c => c.parentId == parentId && c.childId == childId))
                return;

            // Get next child index for this parent
            int nextIndex = connections.Count(c => c.parentId == parentId);

            connections.Add(new BTConnection(parentId, childId, nextIndex));
        }

        public void RemoveConnection(string parentId, string childId)
        {
            connections.RemoveAll(c => c.parentId == parentId && c.childId == childId);

            // Reindex remaining children
            var parentConnections = connections.Where(c => c.parentId == parentId).OrderBy(c => c.childIndex).ToList();
            for (int i = 0; i < parentConnections.Count; i++)
            {
                parentConnections[i].childIndex = i;
            }
        }

        public List<BTConnection> GetChildConnections(string parentId)
        {
            return connections.Where(c => c.parentId == parentId).OrderBy(c => c.childIndex).ToList();
        }

        public BTConnection GetParentConnection(string childId)
        {
            return connections.FirstOrDefault(c => c.childId == childId);
        }

        /// <summary>
        /// Swap the order of two children of a parent node.
        /// </summary>
        public void SwapChildOrder(string parentId, int indexA, int indexB)
        {
            var parentConnections = connections.Where(c => c.parentId == parentId).ToList();

            var connA = parentConnections.FirstOrDefault(c => c.childIndex == indexA);
            var connB = parentConnections.FirstOrDefault(c => c.childIndex == indexB);

            if (connA != null && connB != null)
            {
                connA.childIndex = indexB;
                connB.childIndex = indexA;
            }
        }

        #endregion
    }

    /// <summary>
    /// Builder class for BTRepeater that allows setting child after construction.
    /// </summary>
    internal class BTRepeaterBuilder : IBTNode
    {
        private readonly int _repeatCount;
        private IBTNode _child;
        private BTRepeater _actualRepeater;

        public BTRepeaterBuilder(int repeatCount)
        {
            _repeatCount = repeatCount;
        }

        public void SetChild(IBTNode child)
        {
            _child = child;
            _actualRepeater = new BTRepeater(child, _repeatCount);
        }

        public void Initialize(BTContext context)
        {
            _actualRepeater?.Initialize(context);
        }

        public BTNodeStatus Execute(float deltaTime)
        {
            return _actualRepeater?.Execute(deltaTime) ?? BTNodeStatus.Failure;
        }

        public void Reset()
        {
            _actualRepeater?.Reset();
        }
    }
}

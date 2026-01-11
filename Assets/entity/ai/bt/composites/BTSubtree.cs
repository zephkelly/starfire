using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Executes an external behavior tree as a subtree.
    /// The subtree shares the same BTContext (blackboard) as the parent tree.
    /// Returns the status of the subtree's root node execution.
    /// </summary>
    public class BTSubtree : IBTNode
    {
        private readonly BehaviorTreeAsset _subtreeAsset;
        private readonly string _subtreeName;
        private IBTNode _runtimeSubtree;
        private bool _initialized;

        /// <summary>
        /// Creates a new subtree node.
        /// </summary>
        /// <param name="subtreeAsset">The behavior tree asset to execute as a subtree.</param>
        public BTSubtree(BehaviorTreeAsset subtreeAsset)
        {
            _subtreeAsset = subtreeAsset;
            _subtreeName = subtreeAsset != null ? subtreeAsset.name : "null";
        }

        public void Initialize(BTContext context)
        {
            if (_subtreeAsset == null)
            {
                Debug.LogWarning("BTSubtree: No subtree asset assigned.");
                return;
            }

            // Create runtime tree from asset
            _runtimeSubtree = _subtreeAsset.CreateRuntimeTree();

            if (_runtimeSubtree == null)
            {
                Debug.LogWarning($"BTSubtree: Failed to create runtime tree from '{_subtreeName}'.");
                return;
            }

            // Initialize subtree with SAME context (shared blackboard)
            _runtimeSubtree.Initialize(context);
            _initialized = true;
        }

        public BTNodeStatus Execute(float deltaTime)
        {
            if (!_initialized || _runtimeSubtree == null)
            {
                return BTNodeStatus.Failure;
            }

            return _runtimeSubtree.Execute(deltaTime);
        }

        public void Reset()
        {
            _runtimeSubtree?.Reset();
        }
    }
}

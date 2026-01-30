using System.Collections.Generic;

namespace StarfireV2
{
    /// <summary>
    /// Runs all children every tick in parallel.
    /// Returns Success when all children succeed, Failure if any child fails.
    /// Returns Running if any child is still running (and none have failed).
    /// </summary>
    public class BTParallel : IBTNode
    {
        private readonly List<IBTNode> _children;
        private readonly ParallelMode _mode;
        private BTContext _context;

        public BTParallel(List<IBTNode> children, ParallelMode mode = ParallelMode.RequireAll)
        {
            _children = children ?? new List<IBTNode>();
            _mode = mode;
        }

        public void Initialize(BTContext context)
        {
            _context = context;
            foreach (var child in _children)
            {
                child.Initialize(context);
            }
        }

        public BTNodeStatus Execute(float deltaTime)
        {
            int successCount = 0;
            bool anyRunning = false;

            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                var status = child.Execute(deltaTime);

                switch (status)
                {
                    case BTNodeStatus.Failure:
                        Reset();
                        return BTNodeStatus.Failure;

                    case BTNodeStatus.Success:
                        successCount++;
                        // Race mode: first success wins
                        if (_mode == ParallelMode.RequireOne)
                        {
                            Reset();
                            return BTNodeStatus.Success;
                        }
                        break;

                    case BTNodeStatus.Running:
                        anyRunning = true;
                        break;
                }
            }

            // All succeeded
            if (successCount == _children.Count)
            {
                Reset();
                return BTNodeStatus.Success;
            }

            return anyRunning ? BTNodeStatus.Running : BTNodeStatus.Success;
        }

        public void Reset()
        {
            foreach (var child in _children)
            {
                child.Reset();
            }
        }

        /// <summary>
        /// Sets the children of this parallel. Used by BehaviorTreeAsset during construction.
        /// </summary>
        public void SetChildren(List<IBTNode> children)
        {
            _children.Clear();
            _children.AddRange(children);

            if (_context != null)
            {
                foreach (var child in _children)
                {
                    child.Initialize(_context);
                }
            }
        }
    }
}

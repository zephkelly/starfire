using System.Collections.Generic;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Runs each child until one fails (AND logic).
    /// Returns Failure on first child failure, Success if all children succeed.
    /// </summary>
    public class BTSequence : IBTNode
    {
        private readonly List<IBTNode> _children;
        private BTContext _context;
        private int _currentIndex;

        public BTSequence(List<IBTNode> children)
        {
            _children = children;
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
            while (_currentIndex < _children.Count)
            {
                var child = _children[_currentIndex];
                var status = child.Execute(deltaTime);

                if (status == BTNodeStatus.Running)
                    return BTNodeStatus.Running;

                if (status == BTNodeStatus.Failure)
                {
                    _currentIndex = 0;
                    return BTNodeStatus.Failure;
                }

                // Child succeeded - move to next
                _currentIndex++;
            }

            _currentIndex = 0;
            return BTNodeStatus.Success;
        }

        public void Reset()
        {
            _currentIndex = 0;
            foreach (var child in _children)
            {
                child.Reset();
            }
        }

        /// <summary>
        /// Sets the children of this sequence. Used by BehaviorTreeAsset during construction.
        /// </summary>
        public void SetChildren(List<IBTNode> children)
        {
            _children.Clear();
            _children.AddRange(children);

            // Initialize children if we have a context
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

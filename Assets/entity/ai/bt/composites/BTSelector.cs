using System.Collections.Generic;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Tries each child until one succeeds (OR logic).
    /// Returns Success on first child success, Failure if all children fail.
    /// </summary>
    public class BTSelector : IBTNode
    {
        private readonly List<IBTNode> _children;
        private BTContext _context;
        private int _currentIndex;

        public BTSelector(List<IBTNode> children)
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
                var status = _children[_currentIndex].Execute(deltaTime);

                if (status == BTNodeStatus.Running)
                    return BTNodeStatus.Running;

                if (status == BTNodeStatus.Success)
                {
                    _currentIndex = 0;
                    return BTNodeStatus.Success;
                }

                _currentIndex++;
            }

            _currentIndex = 0;
            return BTNodeStatus.Failure;
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
        /// Sets the children of this selector. Used by BehaviorTreeAsset during construction.
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

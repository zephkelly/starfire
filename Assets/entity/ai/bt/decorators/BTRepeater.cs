namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Repeats child N times or forever (-1).
    /// Returns Running while repeating, Success when count reached.
    /// </summary>
    public class BTRepeater : IBTNode
    {
        private readonly IBTNode _child;
        private readonly int _repeatCount;
        private BTContext _context;
        private int _currentCount;

        /// <param name="child">The child node to repeat</param>
        /// <param name="repeatCount">Number of times to repeat. -1 for infinite.</param>
        public BTRepeater(IBTNode child, int repeatCount = -1)
        {
            _child = child;
            _repeatCount = repeatCount;
        }

        public void Initialize(BTContext context)
        {
            _context = context;
            _child.Initialize(context);
        }

        public BTNodeStatus Execute(float deltaTime)
        {
            var status = _child.Execute(deltaTime);

            if (status == BTNodeStatus.Running)
                return BTNodeStatus.Running;

            _currentCount++;

            if (_repeatCount > 0 && _currentCount >= _repeatCount)
            {
                return BTNodeStatus.Success;
            }

            _child.Reset();
            return BTNodeStatus.Running;
        }

        public void Reset()
        {
            _currentCount = 0;
            _child.Reset();
        }
    }
}

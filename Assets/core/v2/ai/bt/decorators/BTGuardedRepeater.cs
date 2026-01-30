namespace StarfireV2
{
    /// <summary>
    /// A repeater with two children: a guard and a body.
    /// The guard is checked each tick BEFORE executing the body.
    /// If the guard returns Success, the loop is interrupted and control returns to parent.
    /// This allows clean interruption of repeating behaviors (e.g., patrol interrupted by combat).
    /// </summary>
    public class BTGuardedRepeater : IBTNode
    {
        private readonly IBTNode _guard;
        private readonly IBTNode _body;
        private readonly int _repeatCount;
        private BTContext _context;
        private int _currentCount;

        /// <param name="guard">Condition checked each tick. Success = interrupt loop.</param>
        /// <param name="body">Main behavior to repeat while guard fails.</param>
        /// <param name="repeatCount">Max iterations. -1 for infinite (until guard triggers).</param>
        public BTGuardedRepeater(IBTNode guard, IBTNode body, int repeatCount = -1)
        {
            _guard = guard;
            _body = body;
            _repeatCount = repeatCount;
        }

        public void Initialize(BTContext context)
        {
            _context = context;
            _guard?.Initialize(context);
            _body?.Initialize(context);
        }

        public BTNodeStatus Execute(float deltaTime)
        {
            // Check guard FIRST - if it succeeds, interrupt the loop
            if (_guard != null)
            {
                var guardStatus = _guard.Execute(deltaTime);
                if (guardStatus == BTNodeStatus.Success)
                {
                    // Guard triggered - interrupt! Let parent handle the situation
                    return BTNodeStatus.Success;
                }
                if (guardStatus == BTNodeStatus.Running)
                {
                    // Guard still evaluating
                    return BTNodeStatus.Running;
                }
                // Guard failed - no interrupt, continue with body
            }

            // Execute body
            if (_body == null)
                return BTNodeStatus.Failure;

            var bodyStatus = _body.Execute(deltaTime);

            if (bodyStatus == BTNodeStatus.Running)
                return BTNodeStatus.Running;

            // Body completed - check count and loop
            _currentCount++;

            if (_repeatCount > 0 && _currentCount >= _repeatCount)
            {
                return BTNodeStatus.Success;
            }

            // Reset for next iteration
            _body.Reset();
            _guard?.Reset();
            return BTNodeStatus.Running;
        }

        public void Reset()
        {
            _currentCount = 0;
            _guard?.Reset();
            _body?.Reset();
        }
    }
}

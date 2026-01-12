namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Base class for leaf condition nodes that return Success or Failure.
    /// Unlike BTCondition (decorator), this has no child node.
    /// Supports tick intervals for optimization - expensive conditions can run every N ticks.
    /// </summary>
    public abstract class BTLeafCondition : IBTNode
    {
        protected BTContext Context { get; private set; }

        /// <summary>
        /// How often this condition runs. 1 = every tick, 2 = every 2nd tick, etc.
        /// When skipped, returns the cached result from last evaluation.
        /// </summary>
        public int TickInterval { get; set; } = 1;

        private int _tickCounter = 0;
        private BTNodeStatus _cachedStatus = BTNodeStatus.Failure;

        public void Initialize(BTContext context)
        {
            Context = context;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        protected abstract bool CheckCondition();

        public BTNodeStatus Execute(float deltaTime)
        {
            _tickCounter++;

            if (_tickCounter >= TickInterval)
            {
                _tickCounter = 0;
                _cachedStatus = CheckCondition() ? BTNodeStatus.Success : BTNodeStatus.Failure;
            }

            return _cachedStatus;
        }

        public void Reset()
        {
            _tickCounter = 0;
            _cachedStatus = BTNodeStatus.Failure;
        }
    }
}

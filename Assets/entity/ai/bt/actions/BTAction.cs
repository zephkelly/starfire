namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Base class for behavior tree leaf nodes that perform actions.
    /// Supports tick intervals for optimization - expensive actions can run every N ticks.
    /// </summary>
    public abstract class BTAction : IShipBTNode
    {
        protected BTContext Context { get; private set; }

        /// <summary>
        /// How often this action runs. 1 = every tick, 2 = every 2nd tick, etc.
        /// When skipped, returns the cached result from last execution.
        /// </summary>
        public int TickInterval { get; set; } = 1;

        private int _tickCounter = 0;
        private BTNodeStatus _cachedStatus = BTNodeStatus.Running;

        public void Initialize(BTContext context)
        {
            Context = context;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        public BTNodeStatus Execute(float deltaTime)
        {
            _tickCounter++;

            if (_tickCounter >= TickInterval)
            {
                _tickCounter = 0;
                _cachedStatus = OnExecute(deltaTime);
            }

            return _cachedStatus;
        }

        /// <summary>
        /// Override this to implement the action logic.
        /// </summary>
        protected abstract BTNodeStatus OnExecute(float deltaTime);

        public virtual void Reset()
        {
            _tickCounter = 0;
            _cachedStatus = BTNodeStatus.Running;
        }
    }
}

namespace StarfireV2
{
    /// <summary>
    /// Guards a subtree with a condition check.
    /// Returns Failure if condition fails, otherwise returns child result.
    /// </summary>
    public abstract class BTCondition : IBTNode
    {
        private readonly IBTNode _child;
        protected BTContext Context { get; private set; }

        protected BTCondition(IBTNode child)
        {
            _child = child;
        }

        public void Initialize(BTContext context)
        {
            Context = context;
            _child?.Initialize(context);
        }

        protected abstract bool CheckCondition();

        public BTNodeStatus Execute(float deltaTime)
        {
            if (!CheckCondition())
                return BTNodeStatus.Failure;

            return _child?.Execute(deltaTime) ?? BTNodeStatus.Success;
        }

        public void Reset()
        {
            _child?.Reset();
        }
    }
}

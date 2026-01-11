namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Base class for behavior tree leaf nodes that perform actions.
    /// </summary>
    public abstract class BTAction : IBTNode
    {
        protected BTContext Context { get; private set; }

        public void Initialize(BTContext context)
        {
            Context = context;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        public abstract BTNodeStatus Execute(float deltaTime);

        public virtual void Reset() { }
    }
}

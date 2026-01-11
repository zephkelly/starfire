namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Types of behavior tree nodes.
    /// </summary>
    public enum BTNodeType
    {
        // Composites
        Selector,
        Sequence,
        Parallel,

        // Decorators
        Repeater,

        // Actions (actual type determined by actionType field)
        Action
    }
}

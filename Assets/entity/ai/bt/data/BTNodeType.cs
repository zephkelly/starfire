namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Types of behavior tree nodes.
    /// IMPORTANT: Use explicit values to maintain backwards compatibility with serialized assets.
    /// </summary>
    public enum BTNodeType
    {
        // Composites
        Selector = 0,
        Sequence = 1,
        Parallel = 2,

        // Decorators
        Repeater = 3,

        // Actions (actual type determined by actionType field)
        Action = 4,

        // Subtree reference (executes another BehaviorTreeAsset)
        Subtree = 5,

        // New decorators added after existing values to preserve compatibility
        GuardedRepeater = 6
    }
}

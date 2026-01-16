namespace Starfire.Entity.AI.Goals
{
    /// <summary>
    /// Types of goals that can be pursued by AI entities.
    /// </summary>
    public enum GoalType
    {
        None = 0,
        Idle,
        Patrol,
        Investigate,
        Combat,
        Flee,
        Escort,
        Guard,
    }
}

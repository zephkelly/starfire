namespace Starfire.Entity.AI.Goals
{
    /// <summary>
    /// Blackboard key constants for goal system values.
    /// </summary>
    public static class GoalKeys
    {
        public const string GoalManager = "goal_manager";
        public const string CurrentGoalType = "current_goal_type";
        public const string CurrentGoal = "current_goal";
        public const string IsCommanderAssigned = "goal_is_commander_assigned";

        // Patrol goal keys
        public const string PatrolCenter = "patrol_center";
        public const string PatrolRadius = "patrol_radius";

        // Flee goal keys
        public const string FleeTarget = "flee_target";
        public const string SafeDistance = "flee_safe_distance";

        // Combat goal keys
        public const string CombatTarget = "combat_target";
        public const string EngageRange = "combat_engage_range";
    }
}

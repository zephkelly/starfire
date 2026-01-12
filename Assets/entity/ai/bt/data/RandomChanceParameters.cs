using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for RandomChanceCondition.
    /// Succeeds based on a probability roll.
    /// </summary>
    [Serializable]
    public class RandomChanceParameters : IBTNodeParameters
    {
        /// <summary>
        /// Probability of success (0.0 to 1.0).
        /// 0.3 = 30% chance to succeed.
        /// </summary>
        public float chance = 0.5f;

        /// <summary>
        /// If true, only evaluate once and cache the result until reset.
        /// Useful for "should I stop at this waypoint" decisions.
        /// </summary>
        public bool evaluateOnce = false;

        /// <summary>
        /// Blackboard key to store/retrieve cached result when evaluateOnce is true.
        /// </summary>
        public string resultKey = "random_chance_result";

        /// <summary>
        /// Blackboard key that triggers result reset when its value changes.
        /// Typically "waypoint_index" to re-roll at each new waypoint.
        /// </summary>
        public string resetTriggerKey = "waypoint_index";
    }
}
